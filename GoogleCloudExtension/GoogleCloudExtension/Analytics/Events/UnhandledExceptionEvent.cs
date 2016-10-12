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

        public static AnalyticsEvent Create(Exception ex)
        {
            string name;
            if (ex is AggregateException)
            {
                var aggregate = (AggregateException)ex;
                name = aggregate.InnerException.GetType().Name;
            }
            else
            {
                name = ex.GetType().Name;
            }

            return new AnalyticsEvent(
                UnhandledExceptionEventName,
                ExceptionProperty, name);
        }
    }
}
