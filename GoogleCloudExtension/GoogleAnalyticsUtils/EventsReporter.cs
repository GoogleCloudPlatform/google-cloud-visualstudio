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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GoogleAnalyticsUtils
{
    /// <summary>
    /// Reports events using the provided analytics reporter.
    /// </summary>
    public class EventsReporter
    {
        private const int ProjectIdHashIndex = 11;

        private readonly IAnalyticsReporter _reporter;

        public EventsReporter(IAnalyticsReporter reporter)
        {
            _reporter = Preconditions.CheckNotNull(reporter, nameof(reporter));
        }

        public void ReportEventAsync(
            string eventType,
            string eventName,
            string projectNumber = null,
            Dictionary<string, string> metadata = null)
        {
            Preconditions.CheckNotNull(eventType, nameof(eventType));
            Preconditions.CheckNotNull(eventName, nameof(eventName));

            var customDimensions = projectNumber != null ? new Dictionary<int, string>
                {
                    { ProjectIdHashIndex, GetHash(projectNumber) }
                } : null;
            var serializedMetadata = metadata != null ? SerializeEventMetadata(metadata) : null;

            _reporter.ReportPageView(
                page: GetPageViewURI(eventType: eventType, eventName: eventName),
                title: serializedMetadata,
                customDimensions: customDimensions);
        }

        private static string GetHash(string projectId)
        {
            var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(projectId));

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }

        private static string SerializeEventMetadata(Dictionary<string, string> metadata)
        {
            return String.Join(",", metadata.Select(SerializeMetadataEntry));
        }

        private static string SerializeMetadataEntry(KeyValuePair<string, string> entry) =>
            $"{entry.Key}={EscapeValue(entry.Value)}";

        private static string EscapeValue(string value)
        {
            var result = new StringBuilder();
            foreach (var c in value)
            {
                switch (c)
                {
                    case ',':
                    case '=':
                    case '\\':
                        result.Append($@"\{c}");
                        break;

                    default:
                        result.Append(c);
                        break;
                }
            }
            return result.ToString();
        }

        private static string GetPageViewURI(string eventType, string eventName) =>
            $"/virtual/{eventType}/{eventName}";
    }
}
