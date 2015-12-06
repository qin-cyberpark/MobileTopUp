using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace MobileTopUp.Controllers
{
    public class WxController : Controller
    {
        // GET: Wx
        public ActionResult Test()
        {
            bool correct = CheckSignature(Request["signature"], Request["timestamp"], Request["nonce"]);
            if (correct) {
                return Content(Request["echostr"]);
            }
            else
            {
                return Content("NG");
            }
        }

        private bool CheckSignature(string signature, string timestamp, string nonce)
        {
            string token = "qLiFe2015";

            List<string> list = new List<string>();
            list.Add(token);
            list.Add(timestamp);
            list.Add(nonce);
            list.Sort();

            list.Sort();

            string res = string.Join("", list.ToArray());


            Byte[] data1ToHash = Encoding.ASCII.GetBytes(res);
            byte[] hashvalue1 = ((HashAlgorithm)CryptoConfig.CreateFromName("SHA1")).ComputeHash(data1ToHash);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashvalue1)
            {
                sb.Append(b.ToString("x2"));
            }

            return signature == sb.ToString();
        }

        // GET: TopUp
        public ActionResult TopUp(string id)
        {
            string brand = string.IsNullOrEmpty(id)?"":id.ToUpper();
            switch (brand)
            {
                case "SPARK": case "VODAFONE": case "TWODEGREE": case "SKINNY": break;
                default: brand = "UNKNOWN"; break;
            }
            ViewBag.BrandType = brand;
            return View();
        }

    }
}