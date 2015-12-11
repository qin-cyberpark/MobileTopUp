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
                ,new { quantiy = @"[1-5]?" }
            );
            routes.MapRoute(
               "Paid",
               "Paid/{transactionId}",
              new
              {
                  controller = "TopUp",
                  action = "Paid"
              }
            );
            routes.MapRoute(
               "Pay",
               "pay",
              new
              {
                  controller = "TopUp",
                  action = "Pay"
              }
            );
            routes.MapRoute(
                 "TopUpHome",
                 "{brand}/{quantiy}",
                 new
                 {
                     controller = "TopUp",
                     action = "Index",
                     brand = UrlParameter.Optional,
                     quantiy = UrlParameter.Optional
                 },
                 new {quantiy=@"[1-5]?"}
            );
        }
    }
}
