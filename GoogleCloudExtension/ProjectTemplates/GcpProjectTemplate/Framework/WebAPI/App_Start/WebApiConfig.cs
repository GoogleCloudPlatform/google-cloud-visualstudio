using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json.Serialization;
using System.Xml;
using System.Configuration;
using log4net;
using Google.Cloud.Diagnostics.AspNet;

namespace _safe_project_name_
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // To enable Google Cloud Stackdrive Logging and Error Reporting
            // while running on your local machine edit Web.config and uncomment
            // the <projectId> value under the <log4net> section. Ensure that
            // the <projectId> is set to a valid Google Cloud Project Id.

            // [START logging_and_error_reporting]
            // Check to ensure that projectId has been changed from placeholder value.
            var section = (XmlElement)ConfigurationManager.GetSection("log4net");
            XmlElement projectIdElement =
                (XmlElement)section.GetElementsByTagName("projectId").Item(0);
            string projectId =
                Google.Api.Gax.Platform.Instance().GceDetails?.ProjectId ??
                projectIdElement?.Attributes["value"].Value;
            if (string.IsNullOrEmpty(projectId))
            {
                throw new Exception("The logging and error reporting libraries need a project ID. "
                    + "Update Web.config and add a <projectId> entry in the <log4net> section.");
            }
            // [START enable_error_reporting]
            var serviceName = ConfigurationManager.AppSettings["google_error_reporting:serviceName"];
            var version = ConfigurationManager.AppSettings["google_error_reporting:version"];
            // Add a catch all to log all uncaught exceptions to Stackdriver Error Reporting.
            config.Services.Add(typeof(IExceptionLogger),
                ErrorReportingExceptionLogger.Create(projectId, serviceName, version));
            // [END enable_error_reporting]
            // [START enable_logging]
            // Retrieve a logger for this context.
            ILog log = LogManager.GetLogger(typeof(WebApiConfig));
            // [END enable_logging]
            // Log confirmation of set-up to Google Stackdriver Logging.
            log.Info("Stackdriver Logging with Log4net successfully configured for use.");
            log.Info("Stackdriver Error Reporting enabled: " +
                "https://console.cloud.google.com/errors/");
            // [END logging_and_error_reporting]

            // Web API configuration and services
            // Configure Web API to use only bearer token authentication.
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
