// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
        private const string FakeHostName = "my_host_name";
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
                expectedEventName: FakeEventName,
                expectedHostName: FakeHostName);
            var eventsReporter = new EventsReporter(fakeReporter);

            eventsReporter.ReportEvent(
                source: FakeHostName,
                eventType: FakeEventType,
                eventName: FakeEventName,
                userLoggedIn: false);
        }

        [TestMethod]
        public void EventWithMetadataTest()
        {
            var fakeReporter = new FakeAnalyticsReporterForEventsImpl(
                expectedEventType: FakeEventType,
                expectedEventName: FakeEventName,
                expectedHostName: FakeHostName,
                expectedMetadata: s_metadata);
            var eventsReporter = new EventsReporter(fakeReporter);

            eventsReporter.ReportEvent(
                source: FakeHostName,
                eventType: FakeEventType,
                eventName: FakeEventName,
                userLoggedIn: false,
                metadata: s_metadata);
        }

        [TestMethod]
        public void EventWithProjectHashTest()
        {
            var fakeReporter = new FakeAnalyticsReporterForEventsImpl(
                expectedEventType: FakeEventType,
                expectedEventName: FakeEventName,
                expectedHostName: FakeHostName,
                expectedMetadata: s_metadata,
                expectedProjectNumberHash: ExpectedProjectNumberHash);
            var eventsReporter = new EventsReporter(fakeReporter);

            eventsReporter.ReportEvent(
                source: FakeHostName,
                eventType: FakeEventType,
                eventName: FakeEventName,
                userLoggedIn: false,
                metadata: s_metadata,
                projectNumber: FakeProjectNumber);
        }
    }
}
