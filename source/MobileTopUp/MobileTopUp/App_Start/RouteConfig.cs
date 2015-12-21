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
            routes.MapRoute(
                 "Default",
                 "{controller}/{action}/{transactionId}",
                  new
                  {
                      controller = "TopUp",
                      action = "Index",
                      transactionId = UrlParameter.Optional
                  },
                new { transactionId = @"\d*" }
           );
        }
    }
}
