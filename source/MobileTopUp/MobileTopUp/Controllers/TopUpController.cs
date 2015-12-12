using log4net;
using MobileTopUp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MobileTopUp.Models;
using System.Threading;

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
                Store.BizInfo("COMFIRM", string.Format("not login"));
                return Redirect("/");
            }

            //verify brand, amount, pay type
            Transaction trans = new Transaction();
            if (!Store.VerfiyBrand(brand))
            {
                //unvalid brand
                Store.BizInfo("COMFIRM", string.Format("brand not valid"));
                return Redirect("/");
            }

            //verify pay type
            payType = payType.ToUpper();
            if (!Store.VerifyPayType(payType))
            {
                //unvalid pay type
                Store.BizInfo("COMFIRM", string.Format("payment type not valid"));
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
                Store.BizInfo("PAY", string.Format("not login"));
                return Redirect("/");
            }

            //verify transaction
            Transaction trans = (Transaction)Session["CurrentTransaction"];
            Session.Remove("CurrentTransaction");
            if (trans == null)
            {
                Store.BizInfo("PAY", string.Format("no transaction in progress"));
                return Redirect("/");
            }

            //verify skip
            if (isSkip)
            {
                if (!AccountManager.IsAdministrator(account))
                {
                    Store.BizInfo("PAY", string.Format("skip payment but not administrator id={0}", account.ID));
                    return Redirect("/");
                }

                trans.PaymentType = Store.PAYMENT_CODE_SKIP;
            }

            //verify voucher stock
            int stock = VoucherManager.GetStock(trans.Brand);
            if (stock < trans.Quantity)
            {
                Store.BizInfo("PAY", string.Format("not enough voucher {0}/{1}", stock,trans.Quantity));
                ViewBag.ImageUrl = "/img/soldout.png";
                return View("topup-msg");
            }

            //hold voucher
            if (!VoucherManager.Hold(trans))
            {
                Store.BizInfo("PAY", string.Format("can not create transcation and hold voucher"));
                ViewBag.Message = "Sorry, wrong";
                return View("topup-msg");
            }

            //redirect to pay
            string payUrl = null;
            string urlFail = string.Format("{0}/payFail/{1}?payType={2}", Store.Configuration.RootUrl, trans.ID,trans.PaymentType);
            string urlSucces = string.Format("{0}/paid/{1}?payType={2}", Store.Configuration.RootUrl, trans.ID,trans.PaymentType);

            if (isSkip && skipToSuccess)
            {
                //test
                Store.BizInfo("PAY", string.Format("skip to pay success id={0}", account.ID));
                return Redirect(urlSucces);
            }
            else if (isSkip && skipToSuccess)
            {
                //test
                Store.BizInfo("PAY", string.Format("skip to pay fail id={0}", account.ID));
                return Redirect(urlFail);
            }
            else if (isSkip)
            {
                Store.BizInfo("PAY", string.Format("skip payment but go to neither Success nor fail", account.ID));
                return Redirect("/");
            }
            else
            {
                payUrl = Accountant.GeneratePayURL(trans, urlSucces, urlFail);
                Store.BizInfo("PAY", string.Format("payment url generated id={0}", account.ID));
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
                Store.BizInfo("PAID", string.Format("not login"));
                return Redirect("/");
            }

            //verify transaction
            Transaction trans = null;
            using (StoreEntities db = new StoreEntities())
            {
                trans = db.Transactions.Find(transactionId);
                if (trans == null)
                {
                    Store.BizInfo("PAID", string.Format("not exist transactionid={0}", trans.ID));
                    return Redirect("/");
                }
                
                if(trans.AccountID != account.ID)
                {
                    Store.BizInfo("PAID", string.Format("transaction owner not match trans.id={1}, id={0}", trans.ID, account.ID));
                    return Redirect("/");
                }
            }

            bool hasPaid = trans.PaidDate != null;
            if (hasPaid)
            {
                //has paid
                Store.BizInfo("PAID", string.Format("has paid transcation id={0}, go to show page", trans.ID));
                return Redirect(string.Format("Show/{0}", trans.ID));
            }

            //just paid
            //verify payment
            string payResultId = null;
            bool isSkipPayment = false;
            Store.PaymentTypes payType = Store.PaymentCodeToType(trans.PaymentType);
            switch (payType)
            {
                case Store.PaymentTypes.PxPayCreditCard:
                case Store.PaymentTypes.PxPayAccount2Account:
                    payResultId = Request["result"];
                    break;
                case Store.PaymentTypes.Skip:
                    isSkipPayment = true;
                    break;
                default:
                    break;
            }

            if(!isSkipPayment && !Accountant.VerifyPayment(trans.SellingPrice, payType, payResultId))
            {
                //not skip payment or payment not verified
                Store.BizInfo("PAID", string.Format("not skip payment or payment not verified id={0}", trans.ID));
                return Redirect("/");
            }

            //update transaction and voucher
            trans.PaymentRef = payResultId;
            Store.BizInfo("PAID", string.Format("update transaction and vouchers id={0}", trans.ID));
            IList<Voucher> vouchers = VoucherManager.Sold(trans);

            //send voucher
            int voucherLen = vouchers.Count;
            byte[][] imageBytes = new byte[voucherLen][];
            for(int i=0;i< voucherLen; i++)
            {
                imageBytes[i] = vouchers[i].Image;
            }
            WechatHelper.SendImagesAsync(account.ReferenceID, imageBytes);

            ViewBag.Vouchers = vouchers;
            return View("view");
        }

        public ActionResult View(int transactionId)
        {
            //verify customer
            Account account = (Account)Session["LoginAccount"];
            if (account == null)
            {
                Store.BizInfo("VIEW", string.Format("not login"));
                return Redirect("/");
            }

            //verify transaction
            Transaction trans;
            using (StoreEntities db = new StoreEntities())
            {
                trans = db.Transactions.Find(transactionId);
                if (trans == null)
                {
                    Store.BizInfo("VIEW", string.Format("not exist transactionid={0}", trans.ID));
                    return Redirect("/");
                }

                if (trans.AccountID != account.ID)
                {
                    Store.BizInfo("VIEW", string.Format("transaction owner not match trans.id={1}, id={0}", trans.ID, account.ID));
                    return Redirect("/");
                }
            }

            bool hasPaid = trans.PaidDate != null;
            if (!hasPaid)
            {
                //not paid
                Store.BizInfo("VIEW", string.Format("unpaid transaction id={0}", trans.ID));
                return Redirect("/");
            }

            IList<Voucher> vouchers = VoucherManager.FindByTranscationId(trans.ID);
            ViewBag.Vouchers = vouchers;
            return View();
        }
    }
}