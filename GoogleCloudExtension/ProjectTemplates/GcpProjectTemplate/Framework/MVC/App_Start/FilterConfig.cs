using System;
using System.Configuration;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using Google.Cloud.Diagnostics.AspNet;
using log4net;

namespace _safe_project_name_
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            // To enable Google Cloud Stackdriver Logging and Error Reporting
            // while running on your local machine edit Web.config and uncomment
            // the <projectId> value under the <log4net> section. Ensure that
            // the <projectId> is set to a valid Google Cloud Project Id.
            // Otherwise, the features will only work once deployed to GCP.
            
            string projectId =
                Google.Api.Gax.Platform.Instance().GceDetails?.ProjectId ??
                GetProjectIdFromConfig();
            if (!string.IsNullOrEmpty(projectId))
            {
                var serviceName = ConfigurationManager.AppSettings["google_error_reporting:serviceName"];
                var version = ConfigurationManager.AppSettings["google_error_reporting:version"];

                // Add a catch all to log all uncaught exceptions to Stackdriver Error Reporting.
                filters.Add(ErrorReportingExceptionFilter.Create(projectId, serviceName, version));

                // Retrieve a logger for this context.
                ILog log = LogManager.GetLogger(typeof(FilterConfig));
                // Log confirmation of set-up to Google Stackdriver Error Reporting.
                log.Info("Stackdriver Error Reporting enabled: https://console.cloud.google.com/errors/");
            }
            else
            {
                // Retrieve a logger for this context.
                ILog log = LogManager.GetLogger(typeof(FilterConfig));
                // Log warning of missing config for Google Stackdriver Error Reporting.
                log.Warn("Stackdriver Error Reporting not enabled. ProjectId missing from configuration.");
            }

            filters.Add(new HandleErrorAttribute());
        }

        private static string GetProjectIdFromConfig()
        {
            var log4NetSection = ConfigurationManager.GetSection("log4net") as XmlElement;
            var projectIdElement =log4NetSection?.GetElementsByTagName("projectId")?.Item(0) as XmlElement;
            return projectIdElement?.Attributes["value"]?.Value;
        }
    }
}
