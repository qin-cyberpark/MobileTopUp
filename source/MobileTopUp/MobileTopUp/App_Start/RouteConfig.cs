using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MobileTopUp
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute(
                 "MobileTopUpConfirm",
                 @"Wx/topup/confirm/{brand}/{amount}/{paytype}",
                new
                {
                    controller = "Wx",
                    action = "TopUpConfirm",
                }
            );
            routes.MapRoute(
                 "MobileTopUpPay",
                 "Wx/topup/pay",
                defaults: new
                {
                    controller = "Wx",
                    action = "TopUpPay",
                }
            );
            routes.MapRoute(
                "MobileTopUpResult",
                 "Wx/topup/paid/{transID}",
                defaults: new
                {
                    controller = "Wx",
                    action = "TopUpPaid",
                }
            );
            routes.MapRoute(
               "MobileTopUpHome",
                "Wx/topup/{brand}/{amount}",
                new
                {
                    controller = "Wx",
                    action = "TopUpIndex",
                    brand = UrlParameter.Optional,
                    amount = UrlParameter.Optional
                }
            );
            routes.MapRoute(
                 "Default",
                 "{controller}/{action}/{id}",
                 new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
