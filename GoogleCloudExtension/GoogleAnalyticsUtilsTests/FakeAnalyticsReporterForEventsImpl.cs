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
using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleAnalyticsUtilsTests
{
    internal class FakeAnalyticsReporterForEventsImpl : IAnalyticsReporter
    {
        // Indexes for the custom dimensions.
        private const int IsUserSignedInIndex = 16;
        private const int IsInternalUserIndex = 17;
        private const int EventTypeIndex = 19;
        private const int EventNameIndex = 20;
        private const int IsEventHitIndex = 21;
        private const int ProjectNumberHashIndex = 31;

        private const string VirtualUrlPrefix = "/virtual/";

        private readonly string _expectedEventType;
        private readonly string _expectedEventName;
        private readonly string _expectedHostName;
        private readonly Dictionary<string, string> _expectedMetadata;
        private readonly string _expectedProjectNumberHash;
        private readonly bool _expectedUserLoggedIn;

        public FakeAnalyticsReporterForEventsImpl(
            string expectedEventType,
            string expectedEventName,
            string expectedHostName,
            string expectedProjectNumberHash = null,
            bool expectedUserLoggedIn = false,
            Dictionary<string, string> expectedMetadata = null)
        {
            _expectedEventType = expectedEventType;
            _expectedEventName = expectedEventName;
            _expectedHostName = expectedHostName;
            _expectedProjectNumberHash = expectedProjectNumberHash;
            _expectedUserLoggedIn = expectedUserLoggedIn;
            _expectedMetadata = expectedMetadata;
        }

        public void ReportEvent(string category, string action, string label = null, int? value = default(int?))
        {
            throw new NotImplementedException();
        }

        public void ReportPageView(string page, string title, string host, Dictionary<int, string> customDimensions = null)
        {
            string actualEventType, actualEventName;
            ParsePageUrl(page, out actualEventType, out actualEventName);

            Assert.IsNotNull(customDimensions);
            Assert.AreEqual(_expectedEventType, actualEventType);
            Assert.AreEqual(_expectedEventName, actualEventName);
            Assert.AreEqual(_expectedHostName, host);

            if (_expectedMetadata != null)
            {
                Assert.IsNotNull(title);
                var actualMetadata = ParseTitle(title);
                CollectionAssert.AreEqual(_expectedMetadata, actualMetadata, $"Invalid metadata.");
            }

            Assert.AreEqual(_expectedUserLoggedIn ? "true" : "false", customDimensions[IsUserSignedInIndex]);
            Assert.AreEqual("false", customDimensions[IsInternalUserIndex]);
            Assert.AreEqual(_expectedEventType, customDimensions[EventTypeIndex]);
            Assert.AreEqual(_expectedEventName, customDimensions[EventNameIndex]);
            Assert.AreEqual("true", customDimensions[IsEventHitIndex]);
            if (_expectedProjectNumberHash != null)
            {
                Assert.IsNotNull(customDimensions);
                Assert.AreEqual(_expectedProjectNumberHash, customDimensions[ProjectNumberHashIndex]);
            }
        }

        private static Dictionary<string, string> ParseTitle(string src)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            StringBuilder value = new StringBuilder();
            int i = 0;
            while (i < src.Length)
            {
                var c = src[i];
                if (c == '\\')
                {
                    i++;
                    Assert.IsTrue(i < src.Length);
                    c = src[i];
                    switch (c)
                    {
                        case '\\':
                        case '=':
                        case ',':
                            value.Append(c);
                            break;

                        default:
                            Assert.Fail($"Unknown escape sequence \\{c}");
                            break;
                    }
                }
                else if (c == ',')
                {
                    var entry = ParseValue(value.ToString());
                    result.Add(entry.Key, entry.Value);
                    value.Clear();
                }
                else
                {
                    value.Append(c);
                }
                i++;
            }

            // Add the very last value.
            {
                var entry = ParseValue(value.ToString());
                result.Add(entry.Key, entry.Value);
            }
            return result;
        }

        private static KeyValuePair<string, string> ParseValue(string src)
        {
            var separator = src.IndexOf('=');
            Assert.IsTrue(separator > 0, $"Invalid value {src}");
            return new KeyValuePair<string, string>(
                src.Substring(0, separator),
                src.Substring(separator + 1));
        }

        private static void ParsePageUrl(string page, out string eventType, out string eventName)
        {
            Assert.IsNotNull(page);
            Assert.IsTrue(page.StartsWith(VirtualUrlPrefix));

            var parts = page.Substring(VirtualUrlPrefix.Length).Split('/');
            Assert.AreEqual(2, parts.Length, $"Invalid page URL '{page}', invalid number of parts {parts.Length}");

            eventType = parts[0];
            eventName = parts[1];
        }
    }
}
