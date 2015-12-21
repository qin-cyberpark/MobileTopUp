using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Http;
using Senparc.Weixin.MP.CommonAPIs;
using System.Web.Configuration;
using Quartz;
using Quartz.Impl;

namespace MobileTopUp
{
    public class Global : HttpApplication
    {
        private IScheduler scheduler = null;

        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            AccessTokenContainer.Register(Store.Configuration.Wechat.Id, Store.Configuration.Wechat.Key);

            // quartz scheduler
            // construct a scheduler factory
            ISchedulerFactory schedFact = new StdSchedulerFactory();

            // get a scheduler
            IScheduler sched = schedFact.GetScheduler();
            sched.Start();

            // define the job and tie it to our HelloJob class
            IJobDetail job = JobBuilder.Create<MobileTopUp.Jobs.StockMonitor>()
                .WithIdentity("StockMonitor", "Monitor")
                .Build();

            // Trigger the job to run now, and then every 40 seconds
            ITrigger trigger = TriggerBuilder.Create()
              .WithIdentity("StockMonitorTrigger", "MonitorTrigger")
              .StartNow()
              .WithSimpleSchedule(x => x
                  .WithIntervalInMinutes(2)
                  .RepeatForever())
              .Build();

            sched.ScheduleJob(job, trigger);
        }
    }
}