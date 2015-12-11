using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MobileTopUp.Controllers
{
    public class ErrorPageController : Controller
    {
        // GET: ErrorPage
        public ActionResult Index()
        {
            return View("error");
        }
        public ActionResult FileNotFound()
        {
            return View();
        }
        public ActionResult UnauthorizedAccess()
        {
            return View();
        }
    }
}