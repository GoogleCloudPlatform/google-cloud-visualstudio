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
            // Otherwise, logging will only occur when deployed to GCP.
            
            string projectId =
                Google.Api.Gax.Platform.Instance().GceDetails?.ProjectId ??
                GetProjectIdFromConfig();
            if (!string.IsNullOrEmpty(projectId))
            {
                var serviceName = ConfigurationManager.AppSettings["google_error_reporting:serviceName"];
                var version = ConfigurationManager.AppSettings["google_error_reporting:version"];
                // Add a catch all to log all uncaught exceptions to Stackdriver Error Reporting.
                config.Services.Add(
                    typeof(IExceptionLogger),
                    ErrorReportingExceptionLogger.Create(projectId, serviceName, version));

                // Retrieve a logger for this context.
                ILog log = LogManager.GetLogger(typeof(WebApiConfig));
                // Log confirmation of set-up to Google Stackdriver Error Reporting.
                log.Info("Stackdriver Error Reporting enabled: https://console.cloud.google.com/errors/");
            }
            else
            {
                // Retrieve a logger for this context.
                ILog log = LogManager.GetLogger(typeof(WebApiConfig));
                // Log failure of set-up to Google Stackdriver Error Reporting.
                log.Warn("Stackdriver Error Reporting not enabled. ProjectId missing from configuration.");
            }

            // Web API configuration and services
            // Configure Web API to use only bearer token authentication.
            config.SuppressDefaultHostAuthentication();
            config.Filters.Add(new HostAuthenticationFilter(OAuthDefaults.AuthenticationType));

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new {id = RouteParameter.Optional}
            );
        }

        private static string GetProjectIdFromConfig()
        {
            var log4NetSection = ConfigurationManager.GetSection("log4net") as XmlElement;
            var projectIdElement = log4NetSection?.GetElementsByTagName("projectId")?.Item(0) as XmlElement;
            return projectIdElement?.Attributes["value"]?.Value;
        }
    }
}
