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
using System.Collections.Generic;
using StringResources = GoogleCloudExtension.Resources;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// Encapsulates a repo item and a flag that indicates if the repo is just created.
    /// </summary>
    public class CloneDialogResult
    {
        /// <summary>
        /// Indicates if the repo was newly created.
        /// </summary>
        public bool JustCreatedRepo { get; set; }

        /// <summary>
        /// Gets a <seealso cref="RepoItemViewModel"/> object.
        /// </summary>
        public RepoItemViewModel RepoItem { get; set; }
    }

    /// <summary>
    /// Dialog to clone Google Cloud Source Repository.
    /// </summary>
    public class CsrCloneWindow : CommonDialogWindowBase
    {
        private CsrCloneWindowViewModel ViewModel { get; }

        private CsrCloneWindow(IList<Project> projects) : base(StringResources.CsrCloneWindowTitle)
        {
            ViewModel = new CsrCloneWindowViewModel(Close, projects);
            Content = new CsrCloneWindowContent { DataContext = ViewModel };
        }

        /// <summary>
        /// Clone a repository from Google Cloud Source Repository.
        /// </summary>
        /// <param name="projects">A list of GCP <seealso cref="Project"/>.</param>
        /// <returns>
        /// The cloned repo item or null if no repo is cloned.
        /// </returns>
        public static CloneDialogResult PromptUser(IList<Project> projects)
        {
            var dialog = new CsrCloneWindow(projects);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
