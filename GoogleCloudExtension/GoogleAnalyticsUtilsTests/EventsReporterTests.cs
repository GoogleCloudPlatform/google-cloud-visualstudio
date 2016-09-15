using GoogleAnalyticsUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

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
        private const string FakeProjectNumber = "1234";

        // SHA1 hash of project number 1234.
        private const string ExpectedProjectNumberHash = "7110eda4d09e062aa5e4a390b0a572ac0d2c0220";

        private readonly static Dictionary<string, string> s_metadata = new Dictionary<string, string>
            {
                { "thing", "1" },
                { "status", "success" },
                { "complex1", "this=that" },
                { "complex2", "this,that" },
                { "complex3", @"this\that" },
            };

        public TestContext TestContext { get; set; }

        [TestMethod]
        public void SimpleEventTest()
        {
            var fakeReporter = new FakeAnalyticsReporterForEventsImpl(
                expectedEventType: FakeEventType,
                expectedEventName: FakeEventName);
            var eventsReporter = new EventsReporter(fakeReporter);

            eventsReporter.ReportEvent(
                eventType: FakeEventType,
                eventName: FakeEventName);
        }

        [TestMethod]
        public void EventWithMetadataTest()
        {

            var fakeReporter = new FakeAnalyticsReporterForEventsImpl(
                expectedEventType: FakeEventType,
                expectedEventName: FakeEventName,
                expectedMetadata: s_metadata);
            var eventsReporter = new EventsReporter(fakeReporter);

            eventsReporter.ReportEvent(
                eventType: FakeEventType,
                eventName: FakeEventName,
                metadata: s_metadata);
        }

        [TestMethod]
        public void EventWithProjectHashTest()
        {
            var fakeReporter = new FakeAnalyticsReporterForEventsImpl(
                expectedEventType: FakeEventType,
                expectedEventName: FakeEventName,
                expectedMetadata: s_metadata,
                expectedProjectNumberHash: ExpectedProjectNumberHash);
            var eventsReporter = new EventsReporter(fakeReporter);

            eventsReporter.ReportEvent(
                eventType: FakeEventType,
                eventName: FakeEventName,
                metadata: s_metadata,
                projectNumber: FakeProjectNumber);
        }

    }
}
