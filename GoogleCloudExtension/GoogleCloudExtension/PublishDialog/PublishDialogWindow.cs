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

using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.PublishDialog
{
    /// <summary>
    /// This class implements the window that hosts the publish dialog wizard.
    /// </summary>
    public class PublishDialogWindow : CommonDialogWindowBase
    {
        private PublishDialogWindowViewModel ViewModel { get; }

        private PublishDialogWindow(IParsedDteProject project) :
            base(string.Format(GoogleCloudExtension.Resources.PublishDialogCaption, project.Name))
        {
            ViewModel = new PublishDialogWindowViewModel(project, Close);
            Content = new PublishDialogWindowContent { DataContext = ViewModel };
            Closed += (sender, args) => ViewModel.FinishFlow();
        }

        /// <summary>
        /// Starts the publish wizard for the given <paramref name="project"/>.
        /// </summary>
        /// <param name="project">The project to publish.</param>
        public static void PromptUser(IParsedDteProject project)
        {
            var dialog = new PublishDialogWindow(project);
            dialog.ShowModal();
        }

        /// <summary>
        /// Returns true if <paramref name="project"/> can be published using this wizard.
        /// </summary>
        /// <param name="project">The project to check.</param>
        /// <returns>True if the project is supported by this wizard, false otherwise.</returns>
        public static bool CanPublish(IParsedProject project)
        {
            KnownProjectTypes projectType = project.ProjectType;
            return projectType == KnownProjectTypes.WebApplication ||
                projectType == KnownProjectTypes.NetCoreWebApplication1_0 ||
                projectType == KnownProjectTypes.NetCoreWebApplication1_1 ||
                projectType == KnownProjectTypes.NetCoreWebApplication2_0;
        }
    }
}
