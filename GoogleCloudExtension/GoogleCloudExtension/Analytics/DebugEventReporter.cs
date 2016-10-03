﻿// Copyright 2016 Google Inc. All Rights Reserved.
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            string source,
            string eventType,
            string eventName,
            bool userLoggedIn = false,
            string projectNumber = null,
            Dictionary<string, string> metadata = null)
        {
            Debug.WriteLine($"Analytics Event] Event: {eventType}/{eventName} Project: {projectNumber ?? "(No Project)"} Metadata: {SerializeMetadata(metadata)}");

            _reporter.ReportEvent(
                source: source,
                eventType: eventType,
                eventName: eventName,
                userLoggedIn: userLoggedIn,
                projectNumber: projectNumber,
                metadata: metadata);
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
