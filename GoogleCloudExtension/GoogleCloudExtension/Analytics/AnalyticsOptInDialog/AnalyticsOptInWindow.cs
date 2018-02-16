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

using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.Analytics.AnalyticsOptInDialog
{
    /// <summary>
    /// Analytics Opt In Dialog.
    /// </summary>
    public class AnalyticsOptInWindow : CommonDialogWindowBase
    {
        private AnalyticsOptInWindowViewModel ViewModel { get; }

        private AnalyticsOptInWindow()
            : base(GoogleCloudExtension.Resources.AnalyticsPromptTitle)
        {
            ViewModel = new AnalyticsOptInWindowViewModel(this);
            Content = new AnalyticsOptInWindowContent { DataContext = ViewModel };
        }

        /// <summary>
        /// Prompt user to share or not their usage info.
        /// </summary>        
        /// <returns>
        /// true if the user opts in, the Yes button is cliked.
        /// false otherwise, the No button is cliked.
        /// </returns>
        public static bool PromptUser()
        {
            var dialog = new AnalyticsOptInWindow();
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
