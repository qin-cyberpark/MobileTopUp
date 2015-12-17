using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MobileTopUp.Utilities;
using MobileTopUp.Models;
using MobileTopUp.ViewModels;
using System.Diagnostics;
using Tesseract;
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

            return View();
        }

        #region login
        public ActionResult Login()
        {
            Account account = null;
            if (VerifyInfo("ADMIN-IDX", out account))
            {
                //has login
                return Redirect("~/admin/index");
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
            return Content("OK");
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

        public ActionResult AddVoucher()
        {
            Account account = null;
            if (!VerifyInfo("ADMIN-IDX", out account))
            {
                return Redirect("~/admin/login");
            }

            return View();
        }

        [HttpPost]
        public ActionResult RecognizeVoucherNumber()
        {
            try
            {
                string imgBase64 = Request.Form.Get("imgBase64");
                imgBase64 = imgBase64.Substring(imgBase64.LastIndexOf(',') + 1);
                List<string> candidates = VoucherManager.RecognizeVoucherNumber(imgBase64);
                if (candidates == null || candidates.Count == 0)
                {
                    return Content("NG");
                }

                string jsonStr = "";
                foreach (string c in candidates)
                {
                    if (!string.IsNullOrEmpty(jsonStr))
                    {
                        jsonStr += ",";
                    }
                    jsonStr += "\"" + c.Replace(" ", "").Replace("-", "") + "\"";
                }

                jsonStr = "{\"candidates\":[" + jsonStr + "]}";

                return Content(jsonStr);
            }
            catch (Exception ex)
            {
                Store.SysError("RECOGNIZE", "Fail to recognize", ex);
                throw ex;
            }
        }

        [HttpPost]
        public ActionResult UploadVoucher()
        {
            Account account = null;
            if (!VerifyInfo("ADMIN-IDX", out account))
            {
                return Content("{\"fail\":1,\"message:\":\"unauthorized operation\"}");
            }
            /*
            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (file != null && file.ContentLength > 0)
                {
                    int fileSizeInBytes = file.ContentLength;
                    MemoryStream target = new MemoryStream();
                    file.InputStream.CopyTo(target);
                    byte[] data = target.ToArray();


                    Voucher v = new Voucher
                    {
                        Brand = (BrandType)Request["brand_code"],
                        Denomination = VoucherType.Twenty,
                        Number = Request["voucher_number"],
                        Image = data
                    };
                    if (VoucherManager.IsExist(v))
                    {
                        string response = "{\"state\":200,\"message\":" + string.Format("\"{0} {1} is exist\"", v.Brand, v.Number) + "}";
                        return Content(response);
                    }

                    VoucherManager mnger = new VoucherManager(account);
                    mnger.Add(v);
                    if (v.ID > 0)
                    {
                        string response = "{" + string.Format("\"state\":{0},\"result\":\"~/voucher/{0}\",\"message\":\"{1}\"", 200, v.ID, "OK");
                        response += string.Format(",\"voucher_brand\":\"{0}\",\"voucher_id\":\"{1}\", \"voucher_no\":\"{2}\"", v.Brand, v.ID, v.Number) + "}";
                        return Content(response);
                    }
                }
            }

            return Content("{\"state\":200,\"message:\":\"fail to add voucher\"}");
            */

            Voucher v = new Voucher
            {
                Brand = (BrandType)Request["brand_code"],
                Denomination = VoucherType.Twenty,
                Number = Request["voucher_number"],
                SerialNumber = Request["voucher_serial_number"]
            };

            if (VoucherManager.IsExist(v))
            {
                string response = "{\"fail\":1,\"message\":" + string.Format("\"{0} {1} is exist\"", v.Brand, v.SerialNumber) + "}";
                return Content(response);
            }

            VoucherManager mnger = new VoucherManager(account);
            mnger.Add(v);
            if (v.ID > 0)
            {
                string response = "{" + string.Format("\"success\":1,\"result\":\"~/voucher/{0}\",\"message\":\"{1}\"", v.ID, "OK");
                response += string.Format(",\"voucher_brand\":\"{0}\",\"voucher_id\":\"{1}\", \"voucher_no\":\"{2}\"", v.Brand, v.ID, v.Number) + "}";
                return Content(response);
            }

            return Content("{\"fail\":1,\"message:\":\"fail to add voucher\"}");
        }
    }
}