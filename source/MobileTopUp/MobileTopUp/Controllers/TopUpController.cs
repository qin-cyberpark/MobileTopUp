using log4net;
using MobileTopUp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MobileTopUp.Models;

namespace MobileTopUp.Controllers
{
    public class TopUpController : Controller
    {
        /// <summary>
        /// INDEX
        /// </summary>
        /// <param name="brand"></param>
        /// <param name="quantiy"></param>
        /// <returns></returns>
        public ActionResult Index(string brand, int? quantiy)
        {
            Account account = (Account)Session["LoginAccount"];
            if (account == null)
            {
                //authorize is needed
                account = Store.GetAccountByWechatCode(Request["code"]);
                if (account == null)
                {
                    Store.BizInfo("HOME", string.Format("can not get visitor info cdoe=", Request["code"]));
                    return View("Error");
                }
                Session["LoginAccount"] = account;
                Store.BizInfo("HOME", string.Format("user [{0}]{1} login", account.ID, account.Name));
            }

            Store.BizInfo("HOME", string.Format("home page visited cdoe={0}, brand={1}, amount={2}", Request["code"], brand, quantiy));
            ViewBag.Brand = Store.VerifiedBrandOrDefault(brand);
            ViewBag.Qty = quantiy == null ? 1 : quantiy;
            return View();
        }

        /// <summary>
        /// CONFIRM
        /// </summary>
        /// <param name="brand"></param>
        /// <param name="quantiy"></param>
        /// <param name="payType"></param>
        /// <returns></returns>
        public ActionResult Confirm(string brand, int quantiy, string payType)
        {
            //get account
            Account account = (Account)Session["LoginAccount"];
            if (account == null)
            {
                return Redirect("/");
            }

            //verify brand, amount, pay type
            Transaction trans = new Transaction();
            if (!Store.VerfiyBrand(brand))
            {
                //unvalid brand
                return Redirect("/");
            }

            //verify pay type
            payType = payType.ToUpper();
            if (!Store.VerifyPayType(payType))
            {
                //unvalid brand
                return Redirect("/");
            }

            Store.BizInfo("COMFIRM", string.Format("comfirm page visited id={0}, brand={1}, amount={2}", account.ID, brand, quantiy));

            //transaction info
            trans.AccountID = account.ID;
            trans.Brand = brand;
            trans.Quantity = quantiy;
            trans.TotalDenomination = Store.VOUCHER_DEFAULT_DENOMINATION * trans.Quantity;

            trans.PaymentType = payType;

            if (Store.PaymentCodeToType(trans.PaymentType) != Store.PaymentTypes.WechatPay)
            {
                trans.Currency = Store.CurrencyTypeToCode(Store.Currencies.NZD);
                trans.ExchangeRate = Store.Configuration.Payment.ExchangeRateNZD;
                trans.SellingPrice = trans.TotalDenomination * Store.Configuration.Payment.Discount;
            }
            else
            {
                trans.Currency = Store.CurrencyTypeToCode(Store.Currencies.CNY);
                trans.ExchangeRate = Store.Configuration.Payment.ExchangeRateCNY;
                trans.SellingPrice = trans.TotalDenomination * Store.Configuration.Payment.Discount * trans.ExchangeRate;
            }
            trans.SellingPrice = Math.Round(trans.SellingPrice, 2);
            Session["CurrentTransaction"] = trans;

            ViewBag.Transaction = trans;
            ViewBag.IsAdministrator = AccountManager.IsAdministrator(account);
            return View();
        }

        /// <summary>
        /// PAY
        /// </summary>
        /// <returns></returns>
        public ActionResult Pay()
        {
            bool isSkip = !string.IsNullOrEmpty(Request["skip"]);
            bool skipToSuccess = isSkip ? "success".Equals(Request["skip"]) : false;
            bool skipToFail = isSkip ? "fail".Equals(Request["skip"]) : false;

            //verify account
            Account account = (Account)Session["LoginAccount"];
            if (account == null)
            {
                return Redirect("/");
            }

            //verify transaction
            Transaction trans = (Transaction)Session["CurrentTransaction"];
            Session.Remove("CurrentTransaction");
            if (trans == null)
            {
                return Redirect("/");
            }

            //verify skip
            if (isSkip && !AccountManager.IsAdministrator(account))
            {
                return Redirect("/");
            }

            //verify voucher stock
            int stock = VoucherManager.GetStock(trans.Brand);
            if (stock < trans.Quantity)
            {
                ViewBag.ImageUrl = "/img/soldout.png";
                return View("topup-msg");
            }

            //hold voucher
            if (!VoucherManager.Hold(trans))
            {
                ViewBag.Message = "Sorry, wrong";
                return View("topup-msg");
            }

            //redirect to pay
            string payUrl = null;
            string urlFail = string.Format("{0}/payFail/{1}", Store.Configuration.RootUrl, trans.ID);
            string urlSucces = string.Format("{0}/paid/{1}", Store.Configuration.RootUrl, trans.ID);

            if (isSkip && skipToSuccess)
            {
                //test
                return Redirect(urlSucces);
            }
            else if (isSkip && skipToSuccess)
            {
                //test
                return Redirect(urlFail);
            }
            else if (isSkip)
            {
                return Redirect("/");
            }
            else
            {
                payUrl = Accountant.GeneratePayURL(trans, urlSucces, urlFail);
                return Redirect(payUrl);
            }
        }

        /// <summary>
        /// PAID
        /// </summary>
        /// <param name="transID"></param>
        /// <returns></returns>
        public ActionResult Paid(int transactionId)
        {
            //verify customer
            Account account = (Account)Session["LoginAccount"];
            if (account == null)
            {
                return Redirect("/");
            }

            //verify transaction
            Transaction trans;
            bool isJustPaid = false;
            using (StoreEntities db = new StoreEntities())
            {
                trans = db.Transactions.Find(transactionId);
                if (trans == null || trans.AccountID != account.ID)
                {
                    return Redirect("/");
                }

                //update transaction
                if (trans.PaidDate == null)
                {
                    isJustPaid = true;
                    trans.PaymentRef = "TESTREFNUM";
                    trans.PaidDate = DateTime.Now;
                    db.SaveChanges();
                }
            }

            IList<Voucher> vouchers;
            if (isJustPaid)
            {
                vouchers = VoucherManager.Sold(trans.ID);
                foreach (Voucher v in vouchers)
                {
                    WechatHelper.SendImage(account.ReferenceID, v.Image);
                }
            }
            else
            {
                vouchers = VoucherManager.FindByTranscationId(trans.ID);
            }

            //send voucher
            ViewBag.Vouchers = vouchers;
            return View();
        }
    }
}