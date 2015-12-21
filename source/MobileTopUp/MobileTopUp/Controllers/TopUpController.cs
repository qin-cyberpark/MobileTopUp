using log4net;
using MobileTopUp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MobileTopUp.Models;
using System.Threading;
using System.Data.Entity;
using MobileTopUp.ViewModels;
using System.Drawing;

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
        public ActionResult Index()
        {
            Store.BizInfo("INDEX", null, string.Format("visited, cdoe={0}, brand={1}, quantiy={2}", Request["code"], Request["brand"], Request["quantiy"]));
            string brand = Request["brand"];
            int quantiy = 1;
            if (Request["qty"] != null)
            {
                try
                {
                    quantiy = int.Parse(Request["qty"]);
                    if (quantiy < 1 || quantiy > 5)
                    {
                        quantiy = 1;
                    }
                }
                catch
                {
                    quantiy = 1;
                }
            }

            Account account = null;
            if (!VerifyInfoOrLogin("INDEX", Request["code"], out account))
            {
                ErrorViewModel errorModel = new ErrorViewModel
                {
                    Message = "Unauthorized"
                };
                return View("Error", errorModel);
            }
            ViewBag.Brand = BrandType.VerifiedOrDefault(brand);
            ViewBag.Qty = quantiy;
            return View();
        }

        /// <summary>
        /// CONFIRM
        /// </summary>
        /// <param name="brand"></param>
        /// <param name="quantiy"></param>
        /// <param name="payType"></param>
        /// <returns></returns>
        public ActionResult Confirm()
        {
            //get account
            Account account = null;
            if (!VerifyInfo("CONFIM", out account))
            {
                return Redirect("~");
            }


            Transaction trans = new Transaction();

            //verify brand
            string brand = Request.Form["brand"];
            if (brand == null)
            {
                brand = "";
            }
            if (!BrandType.Contains(brand.ToUpper()))
            {
                //unvalid brand
                Store.BizInfo("CONFIM", account.ID, string.Format("brand not valid, brand={0}", brand));
                return Redirect("~");
            }

            //verify quanlity
            string qtyStr = Request.Form["qty"];
            int quantiy = 0;
            if (Request["qty"] != null)
            {
                try
                {
                    quantiy = int.Parse(Request.Form["qty"]);
                }
                catch
                {
                    quantiy = 0;
                }
            }
            if (quantiy < 1 || quantiy > 5)
            {
                //unvalid brand
                Store.BizInfo("CONFIM", account.ID, string.Format("quantity not valid, qty={0}", qtyStr));
                return Redirect("~");
            }

            //verify pay type
            string payType = Request.Form["paytype"];
            if (!payType.Contains(payType.ToUpper()))
            {
                //unvalid pay type
                Store.BizInfo("CONFIM", account.ID, string.Format("payment type not valid, paytype={0}", payType));
                return Redirect("/errorpage");
            }

            Store.BizInfo("CONFIM", account.ID, string.Format("visited, brand={0}, quantiy={1}, paytype={2}", brand, quantiy, payType));

            //transaction info
            trans.Consumer = account;
            trans.Brand = (BrandType)brand;
            trans.Quantity = quantiy;
            trans.TotalDenomination = VoucherType.Twenty * trans.Quantity;

            trans.PaymentType = (PaymentType)payType;

            if (trans.PaymentType != PaymentType.WechatPay)
            {
                trans.Currency = CurrencyType.NZD;
                trans.ExchangeRate = Store.Configuration.Payment.ExchangeRateNZD;
                trans.SellingPrice = trans.TotalDenomination * Store.Configuration.Payment.Discount;
            }
            else
            {
                trans.Currency = CurrencyType.CNY;
                trans.ExchangeRate = Store.Configuration.Payment.ExchangeRateCNY;
                trans.SellingPrice = trans.TotalDenomination * Store.Configuration.Payment.Discount * trans.ExchangeRate;
            }
            trans.SellingPrice = Math.Round(trans.SellingPrice, 2);
            trans.ChargeAmount = trans.SellingPrice;
            if (AccountManager.IsAdministrator(account) && !Store.Configuration.Payment.IsFullCharge)
            {
                trans.ChargeAmount = 0.01M;
            }
            Session["CurrentTransaction"] = trans;

            ViewBag.Transaction = trans;
            ViewBag.IsAdministrator = AccountManager.IsAdministrator(account);
            Store.BizInfo("CONFIM", account.ID, string.Format("ready to pay transaction, brand={0}, quantiy={1}, payment={2}", trans.Brand, trans.Quantity, trans.PaymentType));
            return View();
        }

        /// <summary>
        /// PAY
        /// </summary>
        /// <returns></returns>
        public ActionResult Pay()
        {
            bool isSkip = string.IsNullOrEmpty(Request["skip"]) ? false : bool.Parse(Request["skip"]);

            //get account
            Account account = null;
            Transaction trans = null;
            if (!VerifyInfo("PAY", out account, out trans))
            {
                return Redirect("~");
            }

            if (trans.PaidDate != null)
            {
                //paid
                Store.BizInfo("PAY", account.ID, string.Format("transaction has paid, trans id={0}", trans.ID));
                return Redirect("~/errorpage");
            }

            isSkip = trans.PaymentType == PaymentType.Skip ? true : isSkip;
            //verify skip
            if (isSkip)
            {
                if (!AccountManager.IsAdministrator(account))
                {
                    Store.BizInfo("PAY", account.ID, "unauthorized skip payment");
                    return Redirect("~/errorpage");
                }
                trans.PaymentType = PaymentType.Skip;
            }

            if (trans.ID == 0)
            {
                //hold voucher
                if (!VoucherManager.Hold(trans))
                {
                    Store.BizInfo("PAY", account.ID, string.Format("can not create transcation and hold voucher"));
                    return Redirect("~/errorpage/unavailable");
                }
            }
            //remove transaction for session
            Session.Remove("CurrentTransaction");

            //redirect to pay
            string payUrl = null;
            string urlSuccess = string.Format("{0}/TopUp/PxPayCallBack/{1}?paid=SUCCESS", Store.Configuration.RootUrl, trans.ID);
            string urlFail = string.Format("{0}/TopUp/PxPayCallBack/{1}?paid=FAIL", Store.Configuration.RootUrl, trans.ID);

            if (isSkip)
            {
                //test
                Store.BizInfo("PAY", trans.ID, "skip to test pay payment");
                return Redirect(string.Format("~/TopUp/fakePay/{0}", trans.ID));
            }
            else
            {
                try
                {
                    payUrl = Accountant.GeneratePayURL(trans, urlFail, urlSuccess, trans.PayFailedCount);
                    Store.BizInfo("PAY", account.ID, string.Format("go to payment url generated:{0}", payUrl));
                    return Redirect(payUrl);
                }
                catch
                {
                    Store.BizInfo("PAY", account.ID, string.Format("fail to generate payment url:{0}", payUrl));
                    return Redirect("~/errorpage");
                }
            }
        }

        public ActionResult PxPayCallBack(int transactionId)
        {
            string paidFlag = Request["paid"];
            if (paidFlag == null)
            {
                Store.SysError("MSG", "without paid result parameter");
                return Redirect("~/ErrorPage");
            }
            Store.SysInfo("MSG", string.Format("PxPay call back [{0}]:{1}", paidFlag, Request.Url.ToString()));
            bool isSuccess = "SUCCESS".Equals(paidFlag.ToUpper());

            string payResultId = Request["result"];
            if (string.IsNullOrEmpty(payResultId))
            {
                Store.SysError("MSG", "without result id");
                return Redirect("~/ErrorPage");
            }

            int paidTransId = 0;
            if (!Accountant.VerifyPxPayPayment(payResultId, isSuccess, out paidTransId))
            {
                Store.BizInfo("MSG", null, "px payment not verified, result id=" + payResultId);
                return Redirect("~/ErrorPage");
            }

            if (paidTransId != transactionId)
            {
                Store.BizInfo("MSG", null, string.Format("transaction not matched, px {0} <> url {1}", paidTransId, transactionId));
                return Redirect("~/ErrorPage");
            }

            if (isSuccess)
            {
                return Redirect("~/topup/paid/" + transactionId);
            }
            else
            {
                return Redirect("~/topup/repay/" + transactionId);
            }
        }

        public ActionResult FakePay(int transactionId)
        {
            //get account
            Account account = null;
            if (!VerifyInfo("FAKEPAY", out account))
            {
                return Redirect("~");
            }

            //get account
            if (!AccountManager.IsAdministrator(account))
            {
                Store.BizInfo("FAKEPAY", account.ID, "unauthorized skip payment");
                return Redirect("~/errorpage");
            }

            Transaction trans;
            if (!LoadTransaction(account, transactionId, out trans))
            {
                return Redirect("~/errorpage");
            }

            bool? isSuccess = null;
            if ("success".Equals(Request["result"]))
            {
                isSuccess = true;
            }
            else if ("fail".Equals(Request["result"]))
            {
                isSuccess = false;
            }

            if (isSuccess == null)
            {
                //remove transaction for session
                Session.Remove("CurrentTransaction");
                ViewBag.TransactionID = transactionId;
                ViewBag.IsAdministrator = AccountManager.IsAdministrator(account);
                return View();
            }
            else if (isSuccess == true)
            {
                //paid
                using (var db = new StoreEntities())
                {
                    db.Transactions.Attach(trans);
                    trans.Paid("FAKE");
                    db.SaveChanges();
                    Store.BizInfo("FAKEPAY", account.ID, string.Format("transaction set to paid, id={0}", trans.ID));
                }
                Session.Remove("CurrentTransaction");
                return Redirect("~/topup/paid/" + transactionId);
            }
            else
            {
                //repay
                using (var db = new StoreEntities())
                {
                    db.Transactions.Attach(trans);
                    trans.PayFailedCount++;
                    db.SaveChanges();
                    Store.BizInfo("FAKEPAY", account.ID, string.Format("pay fail count increased , id={0}", trans.ID));
                }
                Session.Remove("CurrentTransaction");
                return Redirect("~/topup/repay/" + transactionId);
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
            Account account = null;
            if (!VerifyInfo("PAID", out account))
            {
                return Redirect("~");
            }

            Transaction trans = null;
            if (!LoadTransaction(account, transactionId, out trans))
            {
                return Redirect("~/errorpage");
            }

            bool hasPaid = trans.PaidDate != null;
            if (!hasPaid)
            {
                Store.BizInfo("PAID", account.ID, string.Format("transcation id={0} has not paid", trans.ID));
                return Redirect("~/errorpage");
            }

            return Redirect(string.Format("~/topup/View/{0}", trans.ID));
        }

        public ActionResult Repay(int transactionId)
        {
            //get account
            Account account = null;
            if (!VerifyInfo("REPAY", out account))
            {
                return Redirect("~");
            }

            Transaction trans = null;
            if (!LoadTransaction(account, transactionId, out trans))
            {
                return Redirect("~/errorpage");
            }

            //repay
            Session["CurrentTransaction"] = trans;
            ViewBag.Transaction = trans;
            ViewBag.IsAdministrator = AccountManager.IsAdministrator(account);
            ViewBag.IsContiuous = Request["contiuous"]==null? true :bool.Parse(Request["contiuous"]);
            return View("Confirm");
        }

        public ActionResult View(int transactionId)
        {
            //verify customer
            Account account = null;
            if (!VerifyInfo("VIEW", out account))
            {
                return Redirect("~");
            }

            Transaction trans = null;
            if (!LoadTransaction(account, transactionId, out trans))
            {
                return Redirect("~/errorpage");
            }

            bool hasPaid = trans.PaidDate != null;
            if (!hasPaid)
            {
                //not paid
                Store.BizInfo("VIEW", account.ID, string.Format("transcation id={0} has not paid", trans.ID));
                return Redirect("~/errorpage");
            }

            bool hasVoucherSent = trans.VoucherSendDate != null;
            if (!hasVoucherSent && !VoucherManager.SendVoucher(trans))
            {
                Store.BizInfo("VIEW", account.ID, string.Format("fail to send voucher to transId={0}", trans.ID));
            }

            ViewBag.Transaction = trans;
            return View();
        }

        public ActionResult MyVoucher()
        {
            //verify customer
            Account account = null;
            if (!VerifyInfoOrLogin("MY_VOUCHER", Request["code"], out account))
            {
                ErrorViewModel errorModel = new ErrorViewModel
                {
                    Message = "Unauthorized"
                };
                return View("Error", errorModel);
            }

            //get voucher
            ViewBag.Transactions = Store.GetTransactionByConsumer(account);

            return View();

        }

        public ActionResult HowToUse()
        {
            return View();
        }

        private bool VerifyInfo(string module, out Account account)
        {
            //verify account
            account = (Account)Session["LoginAccount"];
            if (account == null)
            {
                Store.BizInfo(module, null, string.Format("no account info"));
                return false;
            }

            return true;
        }

        private bool VerifyInfoOrLogin(string module, string code,  out Account account)
        {
            account = null;
            if (!VerifyInfo(module, out account))
            {
                //authorize is needed
                account = Store.GetAccountByWechatCode(code);
                if (account == null)
                {
                    Store.BizInfo(module, null, string.Format("can not get visitor info cdoe=", Request["code"]));
                    ErrorViewModel errorModel = new ErrorViewModel
                    {
                        Message = "Unauthorized"
                    };
                    return false;
                }
                Session["LoginAccount"] = account;
                Store.BizInfo(module, account.ID, string.Format("login, name={0}", account.Name));
            }

            return true;
        }

        private bool VerifyInfo(string module, out Account account, out Transaction trans)
        {
            //verify account
            account = null;
            trans = null;
            if (!VerifyInfo(module, out account))
            {
                return false;
            }

            //verify transaction
            trans = (Transaction)Session["CurrentTransaction"];
            if (trans == null)
            {
                Store.BizInfo(module, account.ID, string.Format("no transaction in progress"));
                return false;
            }

            return true;
        }

        private bool LoadTransaction(Account account, int transactionId, out Transaction trans)
        {
            trans = null;
            if (account != null && account.ID > 0)
            {
                trans = Store.GetTransactionByConsumer(account, transactionId);
            }

            return trans != null;
        }
    }
}