using System;
using System.Configuration;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using Google.Cloud.Diagnostics.AspNet;

namespace $safeprojectname$
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            // To enable Google Cloud Stackdrive Logging and Error Reporting
            // while running on your local machine edit Web.config and uncomment
            // the <projectId> value under the <log4net> section. Ensure that
            // the <projectId> is set to a valid Google Cloud Project Id.

            // [START error_reporting]
            // Check to ensure that projectId has been changed from placeholder value.
            var section = (XmlElement)ConfigurationManager.GetSection("log4net");
            var projectIdElement =
                (XmlElement)section.GetElementsByTagName("projectId").Item(0);
            string projectId =
                Google.Api.Gax.Platform.Instance().GceDetails?.ProjectId ??
                projectIdElement?.Attributes["value"]?.Value;
            if (string.IsNullOrEmpty(projectId))
            {
                throw new Exception("The logging and error reporting libraries need a project ID. "
                    + "Update Web.config and add a <projectId> entry in the <log4net> section.");
            }
            var serviceName = ConfigurationManager.AppSettings["google_error_reporting:serviceName"];
            var version = ConfigurationManager.AppSettings["google_error_reporting:version"];
            // Add a catch all to log all uncaught exceptions to Stackdriver Error Reporting.
            filters.Add(ErrorReportingExceptionFilter.Create(projectId, serviceName, version));
            // [END error_reporting]

            filters.Add(new HandleErrorAttribute());
        }
    }
}
