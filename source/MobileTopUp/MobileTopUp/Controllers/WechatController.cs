using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MobileTopUp.Utilities;

namespace MobileTopUp.Controllers
{
    public class WechatController : Controller
    {
        /// <summary>
        /// receive message & event from wechat
        /// </summary>
        /// <returns></returns>
        public ActionResult Receive()
        {
            /*
            bool correct = WechatHelper.CheckSignature(Request["signature"], Request["timestamp"], Request["nonce"]);
            if (correct && Request.HttpMethod == "GET")
            {
                //for wexin verify using
                return Content(Request["echostr"]);
            }
            else if (correct && Request.HttpMethod == "POST")
            {
                //process the message
                return Content(Request["echostr"]);
            }
            else
            {
                return Content("NG");
            }
            */
            return Content("NG");
        }
    }
}