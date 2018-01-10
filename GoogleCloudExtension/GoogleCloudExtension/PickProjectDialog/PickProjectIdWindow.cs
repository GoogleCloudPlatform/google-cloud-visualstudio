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

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.PickProjectDialog
{
    /// <summary>
    /// Window for a user to choose which projet to use.
    /// </summary>
    public class PickProjectIdWindow : CommonDialogWindowBase, IPickProjectIdWindow
    {
        private PickProjectIdViewModel ViewModel { get; }

        private PickProjectIdWindow(string helpContext, bool allowAccountChange)
            : base(GoogleCloudExtension.Resources.PublishDialogSelectGcpProjectTitle)
        {
            ViewModel = new PickProjectIdViewModel(this, helpContext, allowAccountChange);
            Content = new PickProjectIdWindowContent { DataContext = ViewModel };
        }

        /// <summary>
        /// Initalizes the Pick Project Window and waits for it to finish.
        /// </summary>
        /// <param name="helpMessage">The help message to display.</param>
        /// <param name="allowAccountChange">Whether to show the account change buttons/command.</param>
        /// <returns>
        /// The project ID selected, or an empty string if skipped, or null if canceled.
        /// </returns>
        public static Project PromptUser(string helpMessage, bool allowAccountChange)
        {
            var dialog = new PickProjectIdWindow(helpMessage, allowAccountChange);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
