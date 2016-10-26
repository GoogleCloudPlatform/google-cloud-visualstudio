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
    /// This interface abstracts away a class to send analytics data.
    /// </summary>
    public interface IAnalyticsReporter
    {
        /// <summary>
        /// Reports a single event as defined by Google Analytics.
        /// </summary>
        /// <param name="category">The cateogry of the event.</param>
        /// <param name="action">The action taken.</param>
        /// <param name="label">The label for the event, optional.</param>
        /// <param name="value">The value for the event, optional.</param>
        void ReportEvent(string category, string action, string label = null, int? value = null);

        /// <summary>
        /// Reports a page view.
        /// </summary>
        /// <param name="page">The URL of the page.</param>
        /// <param name="title">The title of the page.</param>
        /// <param name="host">The host name for the page.</param>
        /// <param name="customDimensions">Custom values to report using the custom dimensions.</param>
        void ReportPageView(string page, string title, string host, Dictionary<int, string> customDimensions = null);
    }
}
