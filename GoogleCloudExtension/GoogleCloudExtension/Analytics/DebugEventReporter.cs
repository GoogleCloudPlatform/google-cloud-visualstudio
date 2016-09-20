using GoogleAnalyticsUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics
{
    internal class DebugEventReporter : IEventsReporter
    {
        private readonly IEventsReporter _reporter;

        public DebugEventReporter(IAnalyticsReporter analyticsReporter)
        {
            _reporter = new EventsReporter(analyticsReporter);
        }

        public void ReportEvent(
            string eventType,
            string eventName,
            string projectNumber = null,
            Dictionary<string, string> metadata = null)
        {
            Debug.WriteLine($"Analytics Event] Event: {eventType}/{eventName} Project: {projectNumber ?? "(No Project)"} Metadata: {SerializeMetadata(metadata)}");

            _reporter.ReportEvent(
                eventType,
                eventName,
                projectNumber,
                metadata);
        }

        private static string SerializeMetadata(Dictionary<string, string> metadata)
        {
            if (metadata == null)
            {
                return "(No metadata)";
            }

            return String.Join(",", metadata.Select(x => $"{x.Key}={x.Value}"));
        }
    }
}
