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
using Google.Cloud.Logging.Log4Net;

namespace _safe_project_name_
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Configure Stackdriver Logging via Log4Net.
            log4net.Config.XmlConfigurator.Configure();

            if (LogManager.GetRepository().GetAppenders().OfType<GoogleStackdriverAppender>().Any())
            {
                LogManager.GetLogger(nameof(MvcApplication))
                    .Info("Google Stackdriver Logging enabled: https://cloud.google.com/logs/");
            }

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
