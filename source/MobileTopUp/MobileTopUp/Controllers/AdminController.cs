using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MobileTopUp.Utilities;

namespace MobileTopUp.Controllers
{
    public class AdminController : Controller
    {
        // GET: Voucher
        public ActionResult Scan()
        {
            return View();
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
                        string[] files = postedFile.FileName.Split(new char[] {'\\'});
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
    }
}