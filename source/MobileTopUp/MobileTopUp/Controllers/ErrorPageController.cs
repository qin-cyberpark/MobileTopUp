using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MobileTopUp.ViewModels;
namespace MobileTopUp.Controllers
{
    public class ErrorPageController : Controller
    {
        // GET: ErrorPage
        public ActionResult Index()
        {
            ErrorViewModel model = new ErrorViewModel
            {
                Message = "error"
            };
            return View("error", model);
        }
        public ActionResult FileNotFound()
        {
            ErrorViewModel model = new ErrorViewModel
            {
                Message = "FileNotFound"
            };
            return View("error", model);
        }
        public ActionResult UnauthorizedAccess()
        {
            ErrorViewModel model = new ErrorViewModel
            {
                Message = "UnauthorizedAccess"
            };
            return View("error", model);
        }

        public ActionResult Unavailable()
        {
            ErrorViewModel model = new ErrorViewModel(ErrorViewModel.ErrorType.OutOfStock);
            return View("error", model);
        }
    }
}