using GoogleAnalyticsUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleAnalyticsUtilsTests
{
    internal class FakeAnalyticsReporterForEventsImpl : IAnalyticsReporter
    {
        private const string VirtualUrlPrefix = "/virtual/";

        private readonly string _expectedEventType;
        private readonly string _expectedEventName;
        private readonly Dictionary<string, string> _expectedMetadata;
        private readonly string _expectedProjectNumberHash;

        public FakeAnalyticsReporterForEventsImpl(
            string expectedEventType,
            string expectedEventName,
            Dictionary<string, string> expectedMetadata = null,
            string expectedProjectNumberHash = null)
        {
            _expectedEventType = expectedEventType;
            _expectedEventName = expectedEventName;
            _expectedMetadata = expectedMetadata;
            _expectedProjectNumberHash = expectedProjectNumberHash;
        }

        public void ReportEvent(string category, string action, string label = null, int? value = default(int?))
        {
            throw new NotImplementedException();
        }

        public void ReportPageView(string page, string title, Dictionary<int, string> customDimensions = null)
        {
            string actualEventType, actualEventName;
            ParsePageUrl(page, out actualEventType, out actualEventName);

            Assert.AreEqual(_expectedEventType, actualEventType);
            Assert.AreEqual(_expectedEventName, actualEventName);

            if (_expectedMetadata != null)
            {
                Assert.IsNotNull(title);
                var actualMetadata = ParseTitle(title);
                CollectionAssert.AreEqual(_expectedMetadata, actualMetadata, $"Invalid metadata.");
            }

            if (_expectedProjectNumberHash != null)
            {
                Assert.IsNotNull(customDimensions);
                Assert.AreEqual(_expectedProjectNumberHash, customDimensions[11]);
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
