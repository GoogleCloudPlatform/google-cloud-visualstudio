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
	    // To enable Google Cloud Stackdriver Error Reporting:
            // 1. Enable the Stackdriver Error Reporting API: 
            //    https://console.cloud.google.com/apis/api/clouderrorreporting.googleapis.com
            // 2. Edit Web.config. Replace "YOUR-PROJECT-ID" with your Google Cloud Project ID

            // [START error_reporting]
            // Check to ensure that projectId has been changed from placeholder value.
            var section = (XmlElement)ConfigurationManager.GetSection("log4net");
            XmlElement element =
                (XmlElement)section.GetElementsByTagName("projectId").Item(0);
            string projectId = element.Attributes["value"].Value;
            if (projectId == ("YOUR-PROJECT-ID"))
            {
                throw new Exception("Update Web.config and replace YOUR-PROJECT-ID"
                   + " with your Google Cloud Project ID, and recompile.");
            }
            element =
                (XmlElement)section.GetElementsByTagName("serviceName").Item(0);
            string serviceName = element.Attributes["value"].Value;
            element =
                (XmlElement)section.GetElementsByTagName("version").Item(0);
            string version = element.Attributes["value"].Value;
            // Add a catch all to log all uncaught exceptions to Stackdriver Error Reporting.
	    filters.Add(ErrorReportingExceptionFilter.Create(projectId, serviceName, version));
	    // [END error_reporting]

            filters.Add(new HandleErrorAttribute());
        }
    }
}
