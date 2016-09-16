using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleAnalyticsUtils
{
    /// <summary>
    /// This interface abstracts a generic events reporter for analytics.
    /// </summary>
    public interface IEventsReporter
    {
        /// <summary>
        /// Report an event to analytics.
        /// </summary>
        /// <param name="eventType">The event type of the event.</param>
        /// <param name="eventName">The event name.</param>
        /// <param name="projectNumber">The project number, optional.</param>
        /// <param name="metadata">Extra metadata for the event, optional.</param>
        void ReportEvent(
           string eventType,
           string eventName,
           string projectNumber = null,
           Dictionary<string, string> metadata = null);
    }
}
