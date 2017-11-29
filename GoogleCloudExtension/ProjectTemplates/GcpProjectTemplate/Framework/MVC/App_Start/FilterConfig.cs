using System;
using System.Configuration;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using Google.Cloud.Diagnostics.AspNet;

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

            // [START error_reporting]
            // Check to ensure that projectId has been changed from placeholder value.
            string projectId =
                Google.Api.Gax.Platform.Instance().GceDetails?.ProjectId ??
                GetProjectIdFromConfig();
            if (!string.IsNullOrEmpty(projectId))
            {
            var serviceName = ConfigurationManager.AppSettings["google_error_reporting:serviceName"];
            var version = ConfigurationManager.AppSettings["google_error_reporting:version"];
            // Add a catch all to log all uncaught exceptions to Stackdriver Error Reporting.
            filters.Add(ErrorReportingExceptionFilter.Create(projectId, serviceName, version));
                // [END error_reporting]
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
