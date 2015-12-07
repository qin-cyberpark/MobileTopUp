﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MobileTopUp.Models;
using MobileTopUp.Utilities;

namespace MobileTopUp.Controllers
{
    public class WxController : Controller
    {
        // GET: Wx
        TopUpStore _store = new TopUpStore();
        public ActionResult Test()
        {
            bool correct = WechatHelper.CheckSignature(Request["signature"], Request["timestamp"], Request["nonce"]);
            if (correct && Request.HttpMethod == "GET")
            {
                //for wexin verify using
                return Content(Request["echostr"]);
            }
            else if (correct && Request.HttpMethod == "POST")
            {
                return Content(Request["echostr"]);
            }
            else
            {
                return Content("NG");
            }
        }

        // GET: TopUp
        public ActionResult TopUpIndex(string brand, int? amount)
        {
            //get customer
            Customer customer = (Customer)Session["LoginCustomer"];
            if (customer == null)
            {
                customer = _store.GetCustomerByWechatCode(Request["code"]);
                if (customer == null)
                {
                    return Content("NG");
                }
                Session["LoginCustomer"] = customer;
            }

            ViewBag.Brand = _store.GetVerifiedBrandOrDefault(brand);
            ViewBag.Amount = _store.GetVerifiedAmountOrDefault(amount);
            ViewBag.Discount = _store.Discount;
            ViewBag.ExRateCNY = _store.GetExchangeRate(CurrencyTypes.CNY);
            ViewBag.ExRateNZD = _store.GetExchangeRate(CurrencyTypes.NZD);

            return View("topup-home");

        }

        [OutputCacheAttribute(VaryByParam = "*", Duration = 0, NoStore = true)]
        public ActionResult TopUpConfirm(string brand, int amount, string payType)
        {
            //verify customer
            Customer customer = (Customer)Session["LoginCustomer"];
            if (customer == null)
            {
                return Redirect("/wx/topup");
            }

            //verify brand, amount, pay type
            Transaction trans = new Transaction();
            if (!_store.IsValidBrand(brand))
            {
                //unvalid brand
                return Redirect("/wx/topup");
            }

            //verify pay type
            payType = payType.ToUpper();
            if (!_store.IsValidPayType(payType))
            {
                //unvalid brand
                return Redirect("/wx/topup");
            }


            //transaction info
            trans.Brand = brand;
            trans.TotalDenomination = _store.GetVerifiedAmountOrDefault(amount);
            trans.PaymentType = payType;

            if (TopUpStoreHelper.PaymentCodeToType(trans.PaymentType) != PaymentTypes.WechatPay)
            {
                trans.Currency = TopUpStoreHelper.CurrencyTypeToString(CurrencyTypes.NZD);
                trans.ExchangeRate = _store.GetExchangeRate(CurrencyTypes.NZD);
                trans.SellingPrice = trans.TotalDenomination * _store.Discount;
            }
            else
            {
                trans.Currency = TopUpStoreHelper.CurrencyTypeToString(CurrencyTypes.CNY);
                trans.ExchangeRate = _store.GetExchangeRate(CurrencyTypes.CNY);
                trans.SellingPrice = trans.TotalDenomination * _store.Discount * trans.ExchangeRate;
            }
            trans.SellingPrice = Math.Round(trans.SellingPrice, 2);
            Session["CurrentTransaction"] = trans; 

            ViewBag.Transaction = trans;

            return View("topup-confirm");
        }

        // GET: TopUp
        [OutputCacheAttribute(VaryByParam = "*", Duration = 0, NoStore = true)]
        public ActionResult TopUpPay()
        {
            //verify customer
            Customer customer = (Customer)Session["LoginCustomer"];
            if (customer == null)
            {
                return Redirect("/wx/topup");
            }

            //verify transaction
            Transaction trans = (Transaction)Session["CurrentTransaction"];
            Session.Remove("CurrentTransaction");
            if (trans == null)
            {
                return Redirect("/wx/topup");
            }

            //verify voucher
            //link vouchour
            int voucherNumber = TopUpStoreHelper.CalcNeededVoucherNumber(trans.TotalDenomination);
            IEnumerable<Voucher> vouchers = _store.Vouchers.Where(x => x.Brand.Equals(trans.Brand) && x.TransactionID == null).Take(voucherNumber);
            if (vouchers.Count() != voucherNumber)
            {
                ViewBag.ImageUrl = "/img/soldout.png";
                return View("topup-msg");
            }
            using (var dbTrans = _store.Database.BeginTransaction())
            {
                try
                {
                    //save transaction
                    trans.CustomerID = customer.ID;
                    trans.OrderDate = DateTime.Now;
                    _store.Transactions.Add(trans);
                    _store.SaveChanges();

                    //hold voucher
                    foreach (Voucher v in vouchers)
                    {
                        v.CustomerID = customer.ID;
                        v.TransactionID = trans.ID;
                    }
                    _store.SaveChanges();
                
                    dbTrans.Commit();
                }
                catch (Exception)
                {
                    dbTrans.Rollback();
                    ViewBag.Message = "Sorry, something went wrong.";
                    return View("topup-msg");
                }
            }
           
            //redirect to pay
            switch (TopUpStoreHelper.PaymentCodeToType(trans.PaymentType))
            {
                case PaymentTypes.WechatPay: break;
                case PaymentTypes.PaymentExpressA2A: break;
                case PaymentTypes.PaymentExpressCC: break;
                default: break;
            }

            //test
            return Redirect("/wx/topup/paid/" + trans.ID);
        }

        // GET: TopUp
        [OutputCacheAttribute(VaryByParam = "*", Duration = 0, NoStore = true)]
        public ActionResult TopUpPaid(int transID)
        {
            //verify customer
            Customer customer = (Customer)Session["LoginCustomer"];
            if (customer == null)
            {
                return Redirect("/wx/topup");
            }

            //verify transaction
            Transaction trans = _store.Transactions.Find(transID);
            if(trans == null || trans.CustomerID != customer.ID)
            {
                return Redirect("/wx/topup");
            }

            //get holded voucher
            IEnumerable<Voucher> vouchers = _store.Vouchers.Where(x => x.TransactionID == trans.ID);
            if (trans.PaidDate == null)
            {
                //update transaction
                trans.PaymentRef = "TESTREFNUM";
                trans.PaidDate = DateTime.Now;

                //flag vouchour to sold and send image to customer
                foreach (Voucher v in vouchers)
                {
                    v.IsSold = true;
                    WechatHelper.SendImage(customer.ReferenceID, v.Image);
                }
                _store.SaveChanges();
            }
        
            //send voucher
            ViewBag.Vouchers = vouchers;

            return View("topup-voucher");
        }
    }
}