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
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using GoogleCloudExtension.Analytics;
using Microsoft.VisualStudio.Shell;

namespace GoogleCloudExtension.Options
{
    /// <summary>
    /// This class represents the extension's general options. It can not be renamed to keep backwards compatiblity.
    /// </summary>
    [DesignerCategory("Code")]
    public class AnalyticsOptions : UIElementDialogPage, INotifyPropertyChanged
    {
        /// <summary>
        /// The canonical nonlocalized name of the General Options page subcategory. This value must not change.
        /// </summary>
        public const string PageName = "Usage Report";

        /// <summary>
        /// The WPF page to actually show.
        /// </summary>
        private readonly GeneralOptionsPage _generalOptionsPage;

        /// <summary>
        /// Whether the user is opt-in or not into report usage statistics. By default is false.
        /// </summary>
        public bool OptIn
        {
            get => _generalOptionsPage.ViewModel.OptIn;
            set => _generalOptionsPage.ViewModel.OptIn = value;
        }

        /// <summary>
        /// Determins whether the Google Cloud Platform User/Project control on the main menu bar is visible or hidden.
        /// </summary>
        public bool HideUserProjectControl
        {
            get => _generalOptionsPage.ViewModel.HideUserProjectControl;
            set => _generalOptionsPage.ViewModel.HideUserProjectControl = value;
        }

        public bool DoNotShowAspNetCoreGceWarning
        {
            get => _generalOptionsPage.ViewModel.DoNotShowAspNetCoreGceWarning;
            set => _generalOptionsPage.ViewModel.DoNotShowAspNetCoreGceWarning = value;
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
        protected override UIElement Child => _generalOptionsPage;

        public event PropertyChangedEventHandler PropertyChanged = (sender, args) => { };

        public AnalyticsOptions()
        {
            _generalOptionsPage = new GeneralOptionsPage();
            _generalOptionsPage.ViewModel.PropertyChanged += (sender, args) => PropertyChanged(this, args);
        }

        /// <summary>
        /// Reset all the settings to their default values.
        /// </summary>
        public override void ResetSettings()
        {
            OptIn = false;
            DialogShown = false;
            ClientId = null;
            HideUserProjectControl = false;
            DoNotShowAspNetCoreGceWarning = false;
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
