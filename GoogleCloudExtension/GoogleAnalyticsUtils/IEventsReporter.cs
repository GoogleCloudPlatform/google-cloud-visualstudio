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

using System.Collections.Generic;

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
        /// <param name="source">The source of the events.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="eventName">The event name.</param>
        /// <param name="userLoggedIn">Is there a logged in user.</param>
        /// <param name="projectNumber">The project number, optional.</param>
        /// <param name="metadata">Extra metadata for the event, optional.</param>
        void ReportEvent(
            string source,
            string eventType,
            string eventName,
            bool userLoggedIn,
            string projectNumber = null,
            Dictionary<string, string> metadata = null);
    }
}
