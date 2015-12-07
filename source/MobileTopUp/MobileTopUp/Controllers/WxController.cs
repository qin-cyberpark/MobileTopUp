using System;
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
        public ActionResult TopUp(string brand, int? amount)
        {
            //get customer
            Customer customer = (Customer)Session["LoginCustomer"];
            if(customer == null) {
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

            return View("topup-home");
        }

        public ActionResult TopUpConfirm(string brand, int amount, string payType)
        {
            //verify customer
            Customer customer = (Customer)Session["LoginCustomer"];
            if (customer == null) {
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
            trans.NeededVoucherNumber = TopUpStoreHelper.CalcNeededVoucherNumber(trans.TotalDenomination);
            trans.PaymentType = payType;
           
            if (TopUpStoreHelper.PaymentCodeToType(trans.PaymentType) != PaymentTypes.WechatPay)
            {
                trans.Currency = TopUpStoreHelper.CurrencyTypeToString(CurrencyTypes.NZD);
                trans.SellingPrice = trans.TotalDenomination * _store.Discount;
            }
            else
            {
                trans.Currency = TopUpStoreHelper.CurrencyTypeToString(CurrencyTypes.CNY);
                trans.ExchangeRate = _store.GetExchangeRate(CurrencyTypes.CNY);
                trans.SellingPrice = trans.TotalDenomination * _store.Discount * trans.ExchangeRate;
            }
            trans.SellingPrice = Math.Round(trans.SellingPrice, 2);

            ViewBag.Transaction = trans;

            return View("topup-confirm");
        }
    }
}