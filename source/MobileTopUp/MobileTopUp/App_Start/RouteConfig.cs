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
                 "TopUpConfirm",
                 "confirm/{brand}/{quantiy}/{paytype}",
                new
                {
                    controller = "TopUp",
                    action = "Confirm",
                }
                , new { quantiy = @"[1-5]?" }
            );
            routes.MapRoute(
                "TopUpProcess",
                "{Action}/{transactionId}",
                new
                {
                    controller = "TopUp"
                },
                new { transactionId = @"\d+" }
                );
            routes.MapRoute(
                "ErrorPage",
                "ErrorPage/{Action}",
                new
                {
                    controller = "ErrorPage",
                    action = "index"
                }
                );
            routes.MapRoute(
                 "Default",
                 "{Action}/{brand}/{quantiy}",
                 new
                 {
                     controller = "TopUp",
                     action = "Home",
                     brand = UrlParameter.Optional,
                     quantiy = UrlParameter.Optional
                 },
                 new { quantiy = @"[1-5]?" }
            );
        }
    }
}
