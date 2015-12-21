using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MobileTopUp.Utilities;
using MobileTopUp.Models;
using MobileTopUp.ViewModels;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace MobileTopUp.Controllers
{
    public class AdminController : Controller
    {
        // GET: Voucher

        public ActionResult Index()
        {
            Account account = null;
            if (!VerifyInfo("ADMIN-IDX", out account))
            {
                return Redirect("~/admin/login");
            }

            return Redirect("~/admin/voucher");
        }

        #region login
        public ActionResult Login()
        {
            Account account = null;
            if (VerifyInfo("ADMIN-IDX", out account))
            {
                //has login
                return Redirect("~/admin/voucher");
            }

            //login
            ViewBag.LoginCode = GetLoginCode();
            return View();
        }

        public ActionResult Verify()
        {
            Account account = null;
            if (!VerifyInfo("ADMIN-VERIFY", out account))
            {
                //authorize is needed
                account = Store.GetAccountByWechatCode(Request["code"]);
                if (account == null)
                {
                    return View("Error", new ErrorViewModel
                    {
                        Message = "login failed"
                    });
                }

                if (!AccountManager.IsAdministrator(account))
                {
                    return View("Error", new ErrorViewModel
                    {
                        Message = "unauthorized"
                    });
                }

                //
            }
            //verify one code
            VerifyLoginCode(account);
            return Content("<H1>OK</H1>");
        }

        public ActionResult CheckLoginCode()
        {
            if (string.IsNullOrEmpty(Request["code"]))
            {
                return Content("FAILED");
            }

            int code = int.Parse(Request["code"]);
            Account account = null;
            DateTime dtStart = DateTime.Now;
            var stopwatch = Stopwatch.StartNew();
            while (!CheckLoginCode(code, out account))
            {
                if (stopwatch.Elapsed.Seconds > 30)
                {
                    return Content("FAILED");
                }
                System.Threading.Thread.Sleep(200);
            }

            if (account == null)
            {
                return Content("FAILED");
            }

            Session["LoginAdmin"] = account;
            return Content("SUCCESS");
        }


        private bool VerifyInfo(string module, out Account account)
        {
            //verify account
            account = (Account)Session["LoginAdmin"];
            if (account == null)
            {
                //authorize is needed
                account = Store.GetAccountByWechatCode(Request["code"]);
                if (account == null)
                {
                    Store.BizInfo("ADMIN-VERIFY", null, string.Format("can not get admin info cdoe=", Request["code"]));
                    return false;
                }

                if (!AccountManager.IsAdministrator(account))
                {
                    Store.BizInfo(module, null, string.Format("unauthroized access to admin page"));
                    return false;
                }
                Session["LoginAdmin"] = account;
                Store.BizInfo("ADMIN - VERIFY", account.ID, string.Format("admin login, name={0}", account.Name));
            }

            return true;
        }

        private int GetLoginCode()
        {

            int code = new Random().Next(1000, 9999);
            Dictionary<int, Account> verifyWaitList = (Dictionary<int, Account>)HttpContext.Application["VERIFIED_ADMIN"];
            if (verifyWaitList == null)
            {
                verifyWaitList = new Dictionary<int, Account>();
            }

            verifyWaitList.Add(code, null);
            HttpContext.Application["VERIFIED_ADMIN"] = verifyWaitList;
            Store.BizInfo("ADMIN - VERIFY", null, string.Format("code {0} created for admin login", code));
            return code;
        }

        private bool VerifyLoginCode(Account account)
        {
            Dictionary<int, Account> verifyWaitList = (Dictionary<int, Account>)HttpContext.Application["VERIFIED_ADMIN"];
            if (verifyWaitList == null || verifyWaitList.Count == 0)
            {
                return false;
            }

            int key = verifyWaitList.First(x => x.Value == null).Key;
            verifyWaitList[key] = account;
            HttpContext.Application["VERIFIED_ADMIN"] = verifyWaitList;
            return true;
        }

        private bool CheckLoginCode(int code, out Account account)
        {
            Store.BizInfo("ADMIN - VERIFY", null, string.Format("check code {0} verified or not", code));
            account = null;
            Dictionary<int, Account> verifyWaitList = (Dictionary<int, Account>)HttpContext.Application["VERIFIED_ADMIN"];
            if (verifyWaitList == null)
            {
                Store.BizInfo("ADMIN - VERIFY", null, string.Format("verify Wait List is empty"));
                return false;
            }

            if (!verifyWaitList.ContainsKey(code))
            {
                Store.BizInfo("ADMIN - VERIFY", null, string.Format("code {0} not exist", code));
                return false;
            }

            account = verifyWaitList[code];
            if (account == null)
            {
                Store.BizInfo("ADMIN - VERIFY", null, string.Format("account for code {0} not set", code));
                return false;
            }
            verifyWaitList.Remove(code);
            HttpContext.Application["VERIFIED_ADMIN"] = verifyWaitList;
            return true;
        }
        #endregion

        public ActionResult Voucher()
        {
            Account account = null;
            if (!VerifyInfo("ADMIN-IDX", out account))
            {
                return Redirect("~/admin/login");
            }

            return View();
        }


        [HttpPost]
        public ActionResult AddVoucher()
        {
            Account account = null;
            if (!VerifyInfo("ADMIN-IDX", out account))
            {
                return Content("{\"fail\":1,\"message:\":\"unauthorized operation\"}");
            }

            Voucher v = new Voucher
            {
                Brand = (BrandType)Request["brand"],
                Denomination = VoucherType.Twenty,
                TopUpNumber = Request["topup_number"],
                SerialNumber = Request["serial_number"]
            };

            Voucher existV = VoucherManager.FindBySerialOrTopupNumber(v);
            if (existV != null)
            {
                string numberStr = null;
                if (existV.SerialNumber.Equals(v.SerialNumber))
                {
                    numberStr = "SN-" + v.SerialNumber;
                }
                else if (existV.TopUpNumber.Equals(v.TopUpNumber))
                {
                    numberStr = v.TopUpNumber;
                }
                string response = "{\"fail\":1,\"message\":" + string.Format("\"{0} {1} is exist\"", v.Brand, numberStr) + "}";
                return Content(response);
            }

            VoucherManager mnger = new VoucherManager(account);
            if (mnger.Add(v))
            {
                string response = string.Format("{{\"added\":1,\"voucher_brand\":\"{0}\",\"voucher_sn\":\"{1}\", \"voucher_no\":\"{2}\"}}"
                    , v.Brand, v.SerialNumber, v.TopUpNumber);
                return Content(response);
            }

            return Content("{\"fail\":1,\"message:\":\"fail to add voucher\"}");
        }

        [HttpPost]
        public ActionResult UpdateVoucher()
        {
            Account account = null;
            if (!VerifyInfo("ADMIN-IDX", out account))
            {
                return Content("{\"fail\":1,\"message:\":\"unauthorized operation\"}");
            }

            //ori
            Voucher oriV = new Voucher
            {
                Brand = (BrandType)Request["ori_brand"],
                TopUpNumber = Request["ori_topup_number"],
                SerialNumber = Request["ori_serial_number"]
            };

            //new
            Voucher newV = new Voucher
            {
                Brand = (BrandType)Request["brand"],
                TopUpNumber = Request["topup_number"],
                SerialNumber = Request["serial_number"]
            };

            Voucher existV = null;
            if (oriV.Brand != newV.Brand)
            {
                //change brand
                existV = VoucherManager.FindBySerialOrTopupNumber(newV);
                if (existV != null)
                {
                    string numberStr = null;
                    if (existV.SerialNumber.Equals(newV.SerialNumber))
                    {
                        numberStr = "SN-" + newV.SerialNumber;
                    }
                    else if (existV.TopUpNumber.Equals(newV.TopUpNumber))
                    {
                        numberStr = newV.TopUpNumber;
                    }
                    string response = "{\"fail\":1,\"message\":" + string.Format("\"{0} {1} is exist\"", newV.Brand, numberStr) + "}";
                    return Content(response);
                }
            }
            else if (oriV.SerialNumber.Equals(newV.SerialNumber) && !oriV.TopUpNumber.Equals(newV.TopUpNumber))
            {
                //change top up number
                existV = VoucherManager.FindByTopUpNumber(newV);
                if (existV != null)
                {
                    string response = "{\"fail\":1,\"message\":" + string.Format("\"{0} {1} is exist\"", newV.Brand, newV.TopUpNumber) + "}";
                    return Content(response);
                }
            }
            else if (!oriV.SerialNumber.Equals(newV.SerialNumber) && oriV.TopUpNumber.Equals(newV.TopUpNumber))
            {
                //change serial number
                existV = VoucherManager.FindBySerialNumber(newV);
                if (existV != null)
                {
                    string response = "{\"fail\":1,\"message\":" + string.Format("\"{0} SN-{1} is exist\"", newV.Brand, newV.SerialNumber) + "}";
                    return Content(response);
                }
            }

            VoucherManager mnger = new VoucherManager(account);
            if (mnger.Update(oriV, newV))
            {
                string response = string.Format("{{\"updated\":1,\"ori_voucher_brand\":\"{0}\",\"ori_voucher_sn\":\"{1}\", \"ori_voucher_no\":\"{2}\""
                                                    , oriV.Brand, oriV.SerialNumber, oriV.TopUpNumber);
                response += string.Format(",\"voucher_brand\":\"{0}\",\"voucher_sn\":\"{1}\", \"voucher_no\":\"{2}\"}}"
                                                    , newV.Brand, newV.SerialNumber, newV.TopUpNumber);
                return Content(response);
            }

            return Content("{\"fail\":1,\"message:\":\"fail to update voucher\"}");
        }

        [HttpPost]
        public ActionResult DeleteVoucher()
        {
            Account account = null;
            if (!VerifyInfo("ADMIN-IDX", out account))
            {
                return Content("{\"fail\":1,\"message:\":\"unauthorized operation\"}");
            }

       
            //new
            Voucher delV = new Voucher
            {
                Brand = (BrandType)Request["brand"],
                TopUpNumber = Request["topup_number"],
                SerialNumber = Request["serial_number"]
            };

            VoucherManager mnger = new VoucherManager(account);
            if (mnger.Delete(delV))
            {
                string response = string.Format("{{\"deleted\":1,\"voucher_brand\":\"{0}\",\"voucher_sn\":\"{1}\", \"voucher_no\":\"{2}\"}}"
                                                    , delV.Brand, delV.SerialNumber, delV.TopUpNumber);

                return Content(response);
            }

            return Content("{\"fail\":1,\"message:\":\"fail to delete voucher\"}");
        }

        public ActionResult GetStatistic()
        {
            Account account = null;
            if (!VerifyInfo("ADMIN-IDX", out account))
            {
                return Content("{\"fail\":1,\"message:\":\"unauthorized operation\"}");
            }

            VoucherStatistic statistic = VoucherManager.GetStatistic();
            return Content(statistic.ToJson());
        }
    }
}