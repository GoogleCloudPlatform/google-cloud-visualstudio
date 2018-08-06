// Copyright 2018 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.Options;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using System.Windows.Input;

namespace GoogleCloudExtension.MenuBarControls
{
    public interface IGcpUserProjectViewModel
    {
        AsyncProperty<Project> CurrentProjectAsync { get; }
        AsyncProperty<string> ProfileNameAsync { get; }
        AsyncProperty<string> ProfilePictureUrlAsync { get; }
        AsyncProperty<string> ProfileEmailAsyc { get; }

        /// <summary>
        /// Setting this to true opens the GCP Menu Bar Popup.
        /// </summary>
        bool IsPopupOpen { get; set; }

        /// <summary>
        /// The command to show the manage accounts dialog.
        /// </summary>
        IProtectedCommand ManageAccountsCommand { get; }

        /// <summary>
        /// The command to execute to select a new GCP project.
        /// </summary>
        IProtectedCommand SelectProjectCommand { get; }

        /// <summary>
        /// The command to open the GCP Menu Bar Popup.
        /// </summary>
        ICommand OpenPopup { get; }

        /// <summary>
        /// The general options page.
        /// </summary>
        AnalyticsOptions Options { get; }

        /// <summary>
        /// Refreshes <see cref="CurrentProjectAsync"/>.
        /// </summary>
        void LoadCurrentProject();

        /// <summary>
        /// Refreshes <see cref="ProfileEmailAsyc"/>, <see cref="ProfileNameAsync"/> and
        /// <see cref="ProfilePictureUrlAsync"/>.
        /// </summary>
        void UpdateUserProfile();
    }
}