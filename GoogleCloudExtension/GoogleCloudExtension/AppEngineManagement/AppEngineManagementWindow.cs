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

namespace GoogleCloudExtension.AppEngineManagement
{
    /// <summary>
    /// This class is the Window for the dialog.
    /// </summary>
    public class AppEngineManagementWindow : CommonDialogWindowBase
    {
        private AppEngineManagementViewModel ViewModel { get; }

        private AppEngineManagementWindow(string projectId)
            : base(GoogleCloudExtension.Resources.AppEngineManagementWindowTitle)
        {
            ViewModel = new AppEngineManagementViewModel(this, projectId);
            Content = new AppEngineManagementWindowContent { DataContext = ViewModel };
        }

        /// <summary>
        /// Prompts the user to choose a region for the App Engine app.
        /// </summary>
        /// <param name="projectId">The project ID to use.</param>
        /// <returns>The region chosen by the user.</returns>
        public static string PromptUser(string projectId)
        {
            var dialog = new AppEngineManagementWindow(projectId);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
