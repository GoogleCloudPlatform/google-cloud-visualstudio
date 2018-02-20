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
using GoogleCloudExtension.PickProjectDialog;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog
{
    /// <summary>
    /// The Template Chooser dialog displayed by the <see cref="GoogleProjectTemplateSelectorWizard"/>.
    /// </summary>
    public class TemplateChooserWindow : CommonDialogWindowBase
    {
        private TemplateChooserViewModelBase ViewModel { get; }

        private TemplateChooserWindow(string dialogTitle, TemplateType templateType) : base(dialogTitle)
        {

            switch (templateType)
            {
                case TemplateType.AspNet:
                    ViewModel = new AspNetTemplateChooserViewModel(Close, PromptPickProject);
                    Content = new AspNetTemplateChooserWindowContent { DataContext = ViewModel };
                    break;
                case TemplateType.AspNetCore:
                default:
                    ViewModel = new AspNetCoreTemplateChooserViewModel(Close, PromptPickProject);
                    Content = new AspNetCoreTemplateChooserWindowContent { DataContext = ViewModel };
                    break;
            }
        }

        private static Project PromptPickProject()
        {
            return PickProjectIdWindow.PromptUser(
                GoogleCloudExtension.Resources.TemplateWizardPickProjectIdHelpText, allowAccountChange: true);
        }

        /// <summary>
        /// Prompts the user to select properties of their template.
        /// </summary>
        /// <param name="projectName">The name of the project being created.</param>
        /// <param name="templateType">The type of template being created.</param>
        /// <returns>The result of the dialog. Will return null when the dialog is canceled.</returns>
        public static TemplateChooserViewModelResult PromptUser(string projectName, TemplateType templateType)
        {
            var dialog = new TemplateChooserWindow(
                string.Format(GoogleCloudExtension.Resources.WizardTemplateChooserTitle, projectName), templateType);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
