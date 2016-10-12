using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics.Events
{
    /// <summary>
    /// This event is sent every time that there's an unhandled exception.
    /// </summary>
    internal static class UnhandledExceptionEvent
    {
        private const string UnhandledExceptionEventName = "unhandledException";
        private const string ExceptionProperty = "exception";

        public static AnalyticsEvent Create(AggregateException ex)
        {
            return new AnalyticsEvent(
                UnhandledExceptionEventName,
                ExceptionProperty, ex.InnerException.GetType().Name);
        }

        public static AnalyticsEvent Create(Exception ex)
        {
            return new AnalyticsEvent(
                UnhandledExceptionEventName,
                ExceptionProperty, ex.GetType().Name);
        }
    }
}
