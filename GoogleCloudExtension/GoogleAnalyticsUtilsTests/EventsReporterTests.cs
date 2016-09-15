using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using GoogleAnalyticsUtils;

namespace GoogleAnalyticsUtilsTests
{
    /// <summary>
    /// Summary description for EventsReporterTests
    /// </summary>
    [TestClass]
    public class EventsReporterTests
    {
        private const string FakeEventType = "my_event_type";
        private const string FakeEventName = "my_event_name";

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void SimpleEventTest()
        {
            var fakeReporter = new FakeAnalyticsReporterForEventsImpl(
                expectedEventType: FakeEventType,
                expectedEventName: FakeEventName);
            var eventsReporter = new EventsReporter(fakeReporter);

            eventsReporter.ReportEventAsync(
                eventType: FakeEventType,
                eventName: FakeEventName);
        }

        [TestMethod]
        public void EventWithMetadataTest()
        {
            var metadata = new Dictionary<string, string>
            {
                { "thing", "1" },
                { "status", "success" },
                { "complex1", "this=that" },
                { "complex2", "this,that" },
                { "complex3", @"this\that" },
            };
            var fakeReporter = new FakeAnalyticsReporterForEventsImpl(
                expectedEventType: FakeEventType,
                expectedEventName: FakeEventName,
                expectedMetadata: metadata);
            var eventsReporter = new EventsReporter(fakeReporter);

            eventsReporter.ReportEventAsync(
                eventType: FakeEventType,
                eventName: FakeEventName,
                metadata: metadata);
        }

    }
}
