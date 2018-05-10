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
using System.Windows;

namespace GoogleCloudExtension.Options
{
    /// <summary>
    /// This class represents the extension's analytics settings.
    /// </summary>
    [DesignerCategory("Code")]
    public class AnalyticsOptions : UIElementDialogPage
    {
        /// <summary>
        /// The WPF page to actually show.
        /// </summary>
        private readonly AnalyticsOptionsPage _analyticsOptionsPage = new AnalyticsOptionsPage();

        /// <summary>
        /// Whether the user is opt-in or not into report usage statistics. By default is false.
        /// </summary>
        public bool OptIn
        {
            get { return _analyticsOptionsPage.ViewModel.OptIn; }
            set { _analyticsOptionsPage.ViewModel.OptIn = value; }
        }

        /// <summary>
        /// Whether the analitics dialog has been shown to the user, as it will only be shown once.
        /// </summary>
        public bool DialogShown { get; set; }

        /// <summary>
        /// The client id to use to report usage statistics.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// The version of the extension already installed in the system.
        /// </summary>
        public string InstalledVersion { get; set; }

        /// <inheritdoc />
        protected override UIElement Child => _analyticsOptionsPage;

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

            EventsReporterWrapper.AnalyticsOptInStateChanged();

            base.SaveSettingsToStorage();
        }
    }
}
