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

using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using System;

namespace GoogleCloudExtension.Analytics.AnalyticsOptInDialog
{
    /// <summary>
    /// View model for user control AnalyticsOptInWindowContent.xaml.
    /// </summary>
    public class AnalyticsOptInWindowViewModel : ViewModelBase, IViewModelBase<bool>
    {
        private readonly Lazy<IBrowserService> _browserService;

        /// <summary>
        /// Result of the view model after the dialog window is closed. Remains
        /// false until an action buttion is clicked.
        /// </summary>
        public bool Result { get; private set; }

        /// <summary>
        /// Command for opting in into sharing Analytics info with Google.
        /// </summary>
        public ProtectedCommand OptInCommand { get; }

        /// <summary>
        /// The command to open the usage statistics explanation hyperlink.
        /// </summary>
        public ProtectedCommand AnalyticsLearnMoreLinkCommand { get; }

        private IBrowserService BrowserService => _browserService.Value;

        /// <summary>
        /// Event to close the parent window.
        /// </summary>
        public event Action Close;

        public AnalyticsOptInWindowViewModel()
        {
            _browserService = GoogleCloudExtensionPackage.Instance.GetMefServiceLazy<IBrowserService>();

            OptInCommand = new ProtectedCommand(OnOptInCommand);
            AnalyticsLearnMoreLinkCommand = new ProtectedCommand(
                () => BrowserService.OpenBrowser(AnalyticsLearnMoreConstants.AnalyticsLearnMoreLink));
        }

        private void OnOptInCommand()
        {
            Result = true;
            Close?.Invoke();
        }
    }
}
