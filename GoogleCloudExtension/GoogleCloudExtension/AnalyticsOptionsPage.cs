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

using GoogleCloudExtension.Analytics;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace GoogleCloudExtension
{
    /// <summary>
    /// This class represents the extension's analytics settings.
    /// </summary>
    public class AnalyticsOptionsPage : DialogPage
    {
        /// <summary>
        /// Whether the user is opt-in or not into report usage statistics. By default is false.
        /// </summary>
        [Category("Usage Report")]
        [DisplayName("Report Usage Statistics Enabled")]
        [Description("Whether to report usage statistics to Google")]
        public bool OptIn { get; set; }

        /// <summary>
        /// Whether the analitics dialog has been shown to the user, as it will only be shown once.
        /// </summary>
        [Browsable(false)]
        public bool DialogShown { get; set; }

        /// <summary>
        /// The client id to use to report usage statistics.
        /// </summary>
        [Browsable(false)]
        public string ClientId { get; set; }

        /// <summary>
        /// Reset all the settings to their default values.
        /// </summary>
        public override void ResetSettings()
        {
            OptIn = false;
            DialogShown = false;
            ClientId = null;
        }

        /// <summary>
        /// Perform all of the setting alterantions necessary before finally saving the settings
        /// to storage.
        /// </summary>
        public override void SaveSettingsToStorage()
        {
            if (OptIn)
            {
                if (ClientId == null)
                {
                    Debug.WriteLine("Creating new Client ID");
                    ClientId = Guid.NewGuid().ToString();
                }
            }
            else
            {
                ClientId = null;
            }

            ExtensionAnalytics.AnalyticsOptInStateChanged();

            base.SaveSettingsToStorage();
        }
    }
}
