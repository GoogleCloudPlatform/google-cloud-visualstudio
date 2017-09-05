using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Xml;
using System.Configuration;
using log4net;

namespace $safeprojectname$
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Configure Stackdriver Logging via Log4Net.
            log4net.Config.XmlConfigurator.Configure();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // [START enable_logging]
            // Retrieve a logger for this context.
            ILog log = LogManager.GetLogger(typeof(MvcApplication));
            // Log confirmation of set-up to Google Stackdriver Logging.
            log.Info("Stackdriver Logging with Log4net successfully configured for use.");
            log.Info("Stackdriver Error Reporting enabled: " +
                "https://console.cloud.google.com/errors/");
            // [END enable_logging]
        }
    }
}
