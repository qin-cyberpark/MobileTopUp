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
        public ActionResult Home(string brand, int? quantiy)
        {
            Store.BizInfo("HOME", null, string.Format("visited, cdoe={0}, brand={1}, quantiy={2}", Request["code"], brand, quantiy));
            Account account = null;
            if (!VerifyInfo("HOME",out account))
            {
                //authorize is needed
                account = Store.GetAccountByWechatCode(Request["code"]);
                if (account == null)
                {
                    Store.BizInfo("HOME", null, string.Format("can not get visitor info cdoe=", Request["code"]));
                    return View("Error");
                }
                Session["LoginAccount"] = account;
                Store.BizInfo("HOME", account.ID, string.Format("login, name={0}", account.Name));
            }
            ViewBag.Brand = BrandType.VerifiedOrDefault(brand);
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
            Account account = null;
            if (!VerifyInfo("CONFIM", out account))
            {
                return Redirect("/errorpage");
            }

            //verify brand, amount, pay type
            Transaction trans = new Transaction();
            if (!BrandType.Contains(brand.ToUpper()))
            {
                //unvalid brand
                Store.BizInfo("CONFIM", account.ID, string.Format("brand not valid, code={0}", brand.ToUpper()));
                return Redirect("/errorpage");
            }

            //verify pay type
            payType = payType.ToUpper();
            if (!payType.Contains(payType.ToUpper()))
            {
                //unvalid pay type
                Store.BizInfo("CONFIM", account.ID, string.Format("payment type not valid, code={0}", payType.ToUpper()));
                return Redirect("/errorpage");
            }

            Store.BizInfo("CONFIM", account.ID, string.Format("visited, brand={0}, quantiy={1}", brand, quantiy));

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
        public ActionResult Pay(int transactionId)
        {
            bool isSkip = string.IsNullOrEmpty(Request["skip"]) ? false : bool.Parse(Request["skip"]);

            //get account
            Account account = null;
            Transaction trans = null;
            if (!VerifyInfo("PAY", out account, out trans))
            {
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
                //new transaction
                //verify voucher stock
                if (!VoucherManager.CheckStock(trans.Brand, trans.Quantity))
                {
                    Store.BizInfo("PAY", account.ID, string.Format("not enough {0} voucher, qty={1}", trans.Brand, trans.Quantity));
                    return Redirect("~/errorpage/unavailable");
                }

                //hold voucher
                if (!VoucherManager.Hold(trans))
                {
                    Store.BizInfo("PAY", account.ID, string.Format("can not create transcation and hold voucher"));
                    return Redirect("~/errorpage");
                }

                //remove transaction for session
                Session.Remove("CurrentTransaction");
            }

            //redirect to pay
            string payUrl = null;
            string urlSuccess = string.Format("{0}/Message/PxPayCallBack/SUCCESS", Store.Configuration.RootUrl);
            string urlFail = string.Format("{0}/Message/PxPayCallBack/FAIL", Store.Configuration.RootUrl);

            if (isSkip)
            {
                //test
                Store.BizInfo("PAY", trans.ID, "skip to test pay payment");
                return Redirect(string.Format("~/fakePay/{0}", trans.ID));
            }
            else
            {
                payUrl = Accountant.GeneratePayURL(trans, urlFail, urlSuccess, trans.PayFailedCount);
                Store.BizInfo("PAY", account.ID, string.Format("go to payment url generated:{0}", payUrl));
                return Redirect(payUrl);
            }
        }

        public ActionResult FakePay(int transactionId)
        {
            //get account
            Account account = null;
            if (!VerifyInfo("FAKEPAY", out account))
            {
                return Redirect("~/errorpage");
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
                    trans.Paid();
                    db.SaveChanges();
                    Store.BizInfo("FAKEPAY", account.ID, string.Format("transaction set to paid, id={0}", trans.ID));
                }
                Session.Remove("CurrentTransaction");
                return Redirect("~/paid/" + transactionId);
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
                return Redirect("~/repay/" + transactionId);
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
                return Redirect("~/errorpage");
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

            return Redirect(string.Format("~/View/{0}", trans.ID));
        }

        public ActionResult Repay(int transactionId)
        {
            //get account
            Account account = null;
            if (!VerifyInfo("REPAY", out account))
            {
                return Redirect("~/errorpage");
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
            ViewBag.IsPayFail = true;
            return View("Confirm");
        }

        public ActionResult View(int transactionId)
        {
            //verify customer
            Account account = null;
            if (!VerifyInfo("VIEW", out account))
            {
                return Redirect("~/errorpage");
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

            ViewBag.Vouchers = trans.Vouchers;
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
            if (account == null || account.ID == 0)
            {
                return false;
            }

            using (StoreEntities db = new StoreEntities())
            {
                trans = db.Transactions.Include(t => t.Consumer).Include(t => t.Vouchers)
                            .FirstOrDefault(t => t.ID == transactionId && t.AccountID == account.ID);

                return trans != null;
            }
        }
    }
}