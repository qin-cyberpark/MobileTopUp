using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Quartz;
using MobileTopUp.Models;

namespace MobileTopUp.Jobs
{
    public class StockMonitor : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            VoucherManager.ReleaseUnpaidVouchers();
            //VoucherManager.UpdateStatistic();
        }
    }
}