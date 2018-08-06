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

using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using System;

namespace GoogleCloudExtension.Options
{
    /// <summary>
    /// The View Model for the AnalyticsOptionsPage.
    /// </summary>
    public class GeneralOptionsPageViewModel : ViewModelBase
    {
        private bool _optIn;
        private bool _hideUserProjectControl;
        private readonly Lazy<IBrowserService> _browserService;

        /// <summary>
        /// True if the user has opted-into report usage statistics. False by default.
        /// </summary>
        public bool OptIn
        {
            get { return _optIn; }
            set { SetValueAndRaise(ref _optIn, value); }
        }

        /// <summary>
        /// The command to open the usage statistics explanation hyperlink.
        /// </summary>
        public ProtectedCommand AnalyticsLearnMoreLinkCommand { get; }

        public bool HideUserProjectControl
        {
            get => _hideUserProjectControl;
            set => SetValueAndRaise(ref _hideUserProjectControl, value);
        }

        private IBrowserService BrowserService => _browserService.Value;

        public GeneralOptionsPageViewModel()
        {
            _browserService = new Lazy<IBrowserService>(
                () => GoogleCloudExtensionPackage.Instance.GetMefService<IBrowserService>());
            AnalyticsLearnMoreLinkCommand = new ProtectedCommand(
                () => BrowserService.OpenBrowser(AnalyticsLearnMoreConstants.AnalyticsLearnMoreLink));
        }
    }
}