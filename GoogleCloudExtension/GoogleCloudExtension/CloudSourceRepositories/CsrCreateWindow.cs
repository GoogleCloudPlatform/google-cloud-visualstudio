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
using StringResources = GoogleCloudExtension.Resources;
using GoogleCloudExtension.Theming;
using System.Collections.Generic;

namespace GoogleCloudExtension.CloudSourceRepositories
{
    /// <summary>
    /// Dialog to create a new Google Cloud Source Repository.
    /// </summary>
    public class CsrCreateWindow : CommonDialogWindowBase
    {
        private  CsrCreateWindowViewModel ViewModel { get; }

        private CsrCreateWindow(IList<Project> projects): base(StringResources.CsrCreateWindowTitle)
        {
            ViewModel = new CsrCreateWindowViewModel(this, projects);
            Content = new CsrCreateWindowContent { DataContext = ViewModel };
        }

        /// <summary>
        /// Show create a new repository dialog
        /// </summary>
        /// <param name="projects">A list of GCP <seealso cref="Project"/>.</param>
        /// <returns>The created and cloned repo item</returns>
        public static RepoItemViewModel PromptUser(IList<Project> projects)
        {
            var dialog = new CsrCreateWindow(projects);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
