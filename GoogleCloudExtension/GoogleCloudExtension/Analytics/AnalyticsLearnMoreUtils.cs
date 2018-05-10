// Copyright 2017 Google Inc. All Rights Reserved.
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

using System.Diagnostics;

namespace GoogleCloudExtension.Analytics
{
    /// <summary>
    /// Class to hold URL Usage Statistics explanation link and the code for opening it.
    /// </summary>
    internal static class AnalyticsLearnMoreUtils
    {
        /// <summary>
        /// Url of the page containing a detailed explanation on how and why we collect Usage Statistics.
        /// </summary>
        private const string AnalyticsLearnMoreLink = "https://cloud.google.com/tools/visual-studio/docs/usage-reporting";
        
        /// <summary>
        /// Method that opens the Usage Statistics explanation link
        /// </summary>
        public static void OpenLearnMoreLink()
        {
            Process.Start(AnalyticsLearnMoreLink);
        }
    }
}
