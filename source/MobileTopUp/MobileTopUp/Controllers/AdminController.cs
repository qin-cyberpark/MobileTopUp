using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MobileTopUp.Utilities;
using MobileTopUp.Models;
using MobileTopUp.ViewModels;
using System.Diagnostics;

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
                if(stopwatch.Elapsed.Seconds > 30)
                {
                    return Content("FAILED");
                }
                System.Threading.Thread.Sleep(200);
            }

            if(account == null)
            {
                return Content("FAILED");
            }

            Session["LoginAdmin"] = account;
            return Content("SUCCESS");
        }

        public ActionResult Upload()
        {
            try
            {
                if (Request.QueryString["upload"] != null)
                {
                    string pathrefer = Request.UrlReferrer.ToString();
                    string uploadFolder = "StoreConfig.TempFolder";
                    string filePath = null;

                    var postedFile = Request.Files[0];

                    string file;

                    //In case of IE
                    if (Request.Browser.Browser.ToUpper() == "IE")
                    {
                        string[] files = postedFile.FileName.Split(new char[] { '\\' });
                        file = files[files.Length - 1];
                    }
                    else // In case of other browsers
                    {
                        file = postedFile.FileName;
                    }

                    filePath = uploadFolder + file;
                    if (Request.QueryString["fileName"] != null)
                    {
                        file = Request.QueryString["fileName"];
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);
                        }
                    }

                    string ext = System.IO.Path.GetExtension(filePath);
                    file = Guid.NewGuid() + ext; // Creating a unique name for the file 
                    postedFile.SaveAs(filePath);
                    Response.AddHeader("Vary", "Accept");
                    try
                    {
                        if (Request["HTTP_ACCEPT"].Contains("application/json"))
                            Response.ContentType = "application/json";
                        else
                            Response.ContentType = "text/plain";
                    }
                    catch
                    {
                        Response.ContentType = "text/plain";
                    }

                    //string text = OCRHelper.ScanVoucherNumber(filePath);
                    System.IO.File.Delete(filePath);
                    //return Content(text);
                    return null;
                }
            }
            catch (Exception exp)
            {
                return Content(exp.InnerException.Message);
            }

            return Content("NONE");
        }

        private bool VerifyInfo(string module, out Account account)
        {
            //verify account
            account = (Account)Session["LoginAdmin"];
            if (account == null)
            {
                Store.BizInfo(module, null, string.Format("no admin info"));
                return false;
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
            return code;
        }

        private bool VerifyLoginCode(Account account)
        {
            Dictionary<int, Account> verifyWaitList = (Dictionary<int, Account> )HttpContext.Application["VERIFIED_ADMIN"];
            if (verifyWaitList == null || verifyWaitList.Count == 0)
            {
                return false;
            }

            int key =  verifyWaitList.First(x => x.Value == null).Key;
            verifyWaitList[key] = account;
            HttpContext.Application["VERIFIED_ADMIN"] = verifyWaitList;
            return true;
        }

        private bool CheckLoginCode(int code, out Account account)
        {
            account = null;
            Dictionary<int, Account> verifyWaitList = (Dictionary<int, Account>)HttpContext.Application["VERIFIED_ADMIN"];
            if (verifyWaitList == null)
            {
                return false;
            }

            if (!verifyWaitList.ContainsKey(code))
            {
                return false;
            }

            account = verifyWaitList[code];
            if (account == null)
            {
                return false;
            }
            verifyWaitList.Remove(code);
            HttpContext.Application["VERIFIED_ADMIN"] = verifyWaitList;
            return true;
        }
    }
}