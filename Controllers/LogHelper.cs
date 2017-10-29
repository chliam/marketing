using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace marketing.Controllers
{
    public class LogHelper
    {
        private readonly static ILog mInfoLogger = LogManager.GetLogger("infoAppender");
        private readonly static ILog mErrorLogger = LogManager.GetLogger("errorAppender");

        public static void LogInfo(string message)
        {
            mInfoLogger.Info(message);
        }

        public static void LogError(string message)
        {
            mErrorLogger.Error(message);
        }

        public static void LogError(Exception exception)
        {
            mErrorLogger.Error(exception.Message, exception);
        }
    }
}