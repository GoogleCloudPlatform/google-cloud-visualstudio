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

using EnvDTE;
using GoogleCloudExtension.PublishDialogSteps.ChoiceStep;
using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;

namespace GoogleCloudExtension.PublishDialog
{
    public class PublishDialogWindow : CommonDialogWindowBase
    {
        private PublishDialogWindowViewModel ViewModel { get; }

        private PublishDialogWindow(Project project) : base("Publish Application", 500, 400)
        {
            var initialStep = ChoiceStepViewModel.CreateStep();

            ViewModel = new PublishDialogWindowViewModel(project, initialStep, this);
            Content = new PublishDialogWindowContent { DataContext = ViewModel };
        }

        public static void PromptUser(Project project)
        {
            var dialog = new PublishDialogWindow(project);
            dialog.ShowModal();
        }

        public static bool CanPublish(Project project)
        {
            var type = project.GetProjectType();
            return type == KnownProjectTypes.WebApplication;
        }
    }
}
