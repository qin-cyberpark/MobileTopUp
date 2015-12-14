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
        private Account _account = null;
        private Transaction _trans = null;
        
        /// <summary>
        /// INDEX
        /// </summary>
        /// <param name="brand"></param>
        /// <param name="quantiy"></param>
        /// <returns></returns>
        public ActionResult Home(string brand, int? quantiy)
        {
            Store.BizInfo("HOME", null, string.Format("visited, cdoe={0}, brand={1}, quantiy={2}", Request["code"], brand, quantiy));
            if (!VerifyInfo("HOME"))
            {
                //authorize is needed
                _account = Store.GetAccountByWechatCode(Request["code"]);
                if (_account == null)
                {
                    Store.BizInfo("HOME", null, string.Format("can not get visitor info cdoe=", Request["code"]));
                    return View("Error");
                }
                Session["LoginAccount"] = _account;
                Store.BizInfo("HOME", _account.ID, string.Format("login, name={0}", _account.Name));
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
            if (!VerifyInfo("CONFIM"))
            {
                return Redirect("/errorpage");
            }

            //verify brand, amount, pay type
            Transaction trans = new Transaction();
            if (!BrandType.Contains(brand.ToUpper()))
            {
                //unvalid brand
                Store.BizInfo("CONFIM", _account.ID, string.Format("brand not valid, code={0}", brand.ToUpper()));
                return Redirect("/errorpage");
            }

            //verify pay type
            payType = payType.ToUpper();
            if (!payType.Contains(payType.ToUpper()))
            {
                //unvalid pay type
                Store.BizInfo("CONFIM", _account.ID, string.Format("payment type not valid, code={0}", payType.ToUpper()));
                return Redirect("/errorpage");
            }

            Store.BizInfo("CONFIM", _account.ID, string.Format("visited, brand={0}, quantiy={1}", brand, quantiy));

            //transaction info
            trans.Consumer = _account;
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
            if (IsAdministrator && !Store.Configuration.Payment.IsFullCharge)
            {
                trans.ChargeAmount = 0.01M;
            }
            Session["CurrentTransaction"] = trans;

            ViewBag.Transaction = trans;
            ViewBag.IsAdministrator = IsAdministrator;
            Store.BizInfo("CONFIM", _account.ID, string.Format("ready to pay transaction, brand={0}, quantiy={1}, payment={2}", trans.Brand, trans.Quantity, trans.PaymentType));
            return View();
        }

        /// <summary>
        /// PAY
        /// </summary>
        /// <returns></returns>
        public ActionResult Pay(int transactionId)
        {
            bool isSkip = string.IsNullOrEmpty(Request["skip"]) ? false : bool.Parse(Request["skip"]);
            int attempt = string.IsNullOrEmpty(Request["attempt"])? 0 : int.Parse(Request["attempt"]);

            //get account
            if (!VerifyInfo("PAY", true))
            {
                return Redirect("/errorpage");
            }

            isSkip = _trans.PaymentType == PaymentType.Skip ? true : isSkip;
            //verify skip
            if (isSkip)
            {
                if (!IsAdministrator)
                {
                    Store.BizInfo("PAY", _account.ID, "unauthorized skip payment");
                    return Redirect("/errorpage");
                }
                _trans.PaymentType = PaymentType.Skip;
            }

            if (_trans.ID == 0)
            {
                //verify voucher stock
                int stock = VoucherManager.GetStock(_trans.Brand);
                if (stock < _trans.Quantity)
                {
                    Store.BizInfo("PAY", _account.ID, string.Format("not enough {0} voucher stock {1}/{2}", _trans.Brand, stock, _trans.Quantity));
                    return Redirect("/errorpage/unavailable");
                }

                //hold voucher
                if (!VoucherManager.Hold(_trans))
                {
                    Store.BizInfo("PAY", _account.ID, string.Format("can not create transcation and hold voucher"));
                    return Redirect("/errorpage");
                }

                //remove transaction for session
                Session.Remove("CurrentTransaction");
            }

            //redirect to pay
            string payUrl = null;
            string urlPaid = string.Format("{0}/paid/{1}?attempt={2}", Store.Configuration.RootUrl, _trans.ID, attempt + 1);

            if (isSkip)
            {
                //test
                Store.BizInfo("PAY", _account.ID, "skip to test pay payment");
                return Redirect(string.Format("/fakePay/{0}",_trans.ID));
            }else
            {
                payUrl = Accountant.GeneratePayURL(_trans, urlPaid, urlPaid, attempt);
                Store.BizInfo("PAY", _account.ID, string.Format("go to payment url generated:{0}", payUrl));
                return Redirect(payUrl);
            }
        }

        public ActionResult FakePay(int transactionId)
        {
            //get account
            if (!VerifyInfo("FAKEPAY"))
            {
                return Redirect("/errorpage");
            }
            if (!IsAdministrator)
            {
                Store.BizInfo("FAKEPAY", _account.ID, "unauthorized skip payment");
                return Redirect("/errorpage");
            }
            ViewBag.TransactionID = transactionId;
            ViewBag.IsAdministrator = IsAdministrator;
            return View();
        }

        /// <summary>
        /// PAID
        /// </summary>
        /// <param name="transID"></param>
        /// <returns></returns>
        public ActionResult Paid(int transactionId)
        {
            int attempt = string.IsNullOrEmpty(Request["attempt"]) ? 0 : int.Parse(Request["attempt"]);
            Transaction trans = null;

            //verify customer
            if (!VerifyInfo("PAID"))
            {
                return Redirect("/errorpage");
            }

            if (!LoadTransaction(_account, transactionId, out trans))
            {
                return Redirect("/errorpage");
            }

            bool hasPaid = trans.PaidDate != null;
            if (hasPaid)
            {
                //has paid
                Store.BizInfo("PAID", _account.ID, string.Format("transcation id={0} has paid, go to view page", trans.ID));
                return Redirect(string.Format("View/{0}", trans.ID));
            }

            //just paid
            //verify payment
            string payResultId = null;
            if (trans.PaymentType == PaymentType.PxPay)
            {
                payResultId = Request["result"];
                Store.BizInfo("PAID", _account.ID, string.Format("Px Pay result id={0}", payResultId));
            }

            string authCode = null;
            string response = null;
            bool paymentVerified = false;
            if (trans.PaymentType != PaymentType.Skip)
            {
                //normal payment
                paymentVerified = Accountant.VerifyPayment(trans.ChargeAmount, trans.PaymentType, payResultId, out authCode, out response);
                //update response
                using (StoreEntities db = new StoreEntities()) { 
                    db.Transactions.Attach(trans);
                    trans.PaymentRef = (string.IsNullOrEmpty(trans.PaymentRef)?"": trans.PaymentRef) + response;
                    db.SaveChanges();
                }
            }
            else
            {
                //skip payment
                Store.BizInfo("PAID", _account.ID, string.Format("skip payment {0}, id={1}", Request["result"], trans.ID));
                if ("success".Equals(Request["result"]))
                {
                    paymentVerified = true;
                }
            }

            if (!paymentVerified)
            {
                //payment info
                Store.BizInfo("PAID", _account.ID, string.Format("invalid payment, id={0}", trans.ID));

                //repay
                return Redirect(string.Format("/Repay/{0}?attempt={1}",trans.ID, attempt));
            }

            //update transaction and voucher
            trans.AuthCode = authCode;
            Store.BizInfo("PAID", _account.ID, string.Format("payment success, id={0}", trans.ID));
            VoucherManager.Sold(trans);

            //send voucher
            byte[][] imageBytes = new byte[trans.Vouchers.Count][];
            int idx = 0;
            foreach (Voucher v in trans.Vouchers)
            {
                imageBytes[idx++] = v.Image;
            }
            WechatHelper.SendImagesAsync(_account.ReferenceID, imageBytes);
            WechatHelper.SendMessageAsync(_account.ReferenceID, string.Format("Your {0} voucher number:{1}", trans.Brand, trans.VoucherNumberString));

            ViewBag.Vouchers = trans.Vouchers;
            return View("view");
        }

        public ActionResult Repay(int transactionId)
        {
            int attempt = string.IsNullOrEmpty(Request["attempt"]) ? 0 : int.Parse(Request["attempt"]);

            //get account
            if (!VerifyInfo("REPAY"))
            {
                return Redirect("/errorpage");
            }

            Transaction trans = null;
            if (!LoadTransaction(_account, transactionId, out trans))
            {
                return Redirect("/errorpage");
            }

            //repay
            Session["CurrentTransaction"] = trans;
            ViewBag.Transaction = trans;
            ViewBag.IsAdministrator = IsAdministrator;
            ViewBag.Attempt = attempt;
            return View("Confirm");
        }

        public ActionResult View(int transactionId)
        {
            Transaction trans = null;

            //verify customer
            if (!VerifyInfo("VIEW"))
            {
                return Redirect("/errorpage");
            }

            if (!LoadTransaction(_account, transactionId, out trans))
            {
                return Redirect("/errorpage");
            }

            bool hasPaid = trans.PaidDate != null;
            if (!hasPaid)
            {
                //not paid
                Store.BizInfo("VIEW", _account.ID, string.Format("unpaid transaction, id={0}", trans.ID));
                return Redirect("/errorpage");
            }

            ViewBag.Vouchers = trans.Vouchers;
            return View();
        }

        private bool IsAdministrator
        {
            get
            {
                return AccountManager.IsAdministrator(_account);
            }
        }
        private bool VerifyInfo(string module, bool verifyTransaction = false)
        {
            //verify account
            _account = (Account)Session["LoginAccount"];
            if (_account == null)
            {
                Store.BizInfo(module, null, string.Format("no account info"));
                return false;
            }
            //verify transaction
            _trans = (Transaction)Session["CurrentTransaction"];
            if (verifyTransaction && _trans == null)
            {
                Store.BizInfo(module, _account.ID, string.Format("no transaction in progress"));
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