using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MobileTopUp
{
    public partial class Store
    {
        private static readonly log4net.ILog _sysLogger = log4net.LogManager.GetLogger("SysLogger");
        private static readonly log4net.ILog _bizLogger = log4net.LogManager.GetLogger("BizLogger");

        public static void SysInfo(object model, object message)
        {
            _sysLogger.Info(string.Format("[{0}]{1}", model, message));
        }
        public static void SysError(object model, object message)
        {
            _sysLogger.Error(string.Format("[{0}]{1}", model, message));
        }
        public static void SysError(object model, object message, Exception exception)
        {
            _sysLogger.Error(string.Format("[{0}]{1}", model, message), exception);
        }
        public static void BizInfo(object model, string message)
        {
            _bizLogger.Info(string.Format("[{0}]{1}", model, message));
        }
    }
}