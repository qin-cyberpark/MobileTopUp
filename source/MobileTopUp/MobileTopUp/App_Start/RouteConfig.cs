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
                name: "MobileTopUpPay",
                url: "Wx/topup/confirm/{brand}/{amount}/{paytype}",
                defaults: new
                {
                    controller = "Wx",
                    action = "TopUpConfirm",
                }
            );
            routes.MapRoute(
                name: "MobileTopUpHome",
                url: "Wx/topup/{brand}/{amount}",
                defaults: new
                {
                    controller = "Wx",
                    action = "TopUp",
                    brand = UrlParameter.Optional,
                    amount = UrlParameter.Optional
                }
            );
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
