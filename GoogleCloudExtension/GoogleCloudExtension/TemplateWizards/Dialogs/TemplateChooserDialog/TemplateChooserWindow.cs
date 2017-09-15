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

using GoogleCloudExtension.TemplateWizards.Dialogs.ProjectIdDialog;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog
{
    /// <summary>
    /// The Template Chooser dialog displayed by the <see cref="GoogleProjectTemplateSelectorWizard"/>.
    /// </summary>
    public class TemplateChooserWindow : CommonDialogWindowBase
    {
        private TemplateChooserViewModel ViewModel { get; }

        private TemplateChooserWindow(string projectName)
            : base(string.Format(GoogleCloudExtension.Resources.WizardTemplateChooserTitle, projectName))
        {
            ViewModel = new TemplateChooserViewModel(Close, PickProjectIdWindow.PromptUser);
            Content = new TemplateChooserWindowContent { DataContext = ViewModel };
        }

        /// <summary>
        /// Prompts the user to select properties of their template.
        /// </summary>
        /// <param name="projectName">The name of the project being created.</param>
        /// <returns>The result of the dialog. Will return null when the dialog is canceled.</returns>
        public static TemplateChooserViewModelResult PromptUser(string projectName)
        {
            var dialog = new TemplateChooserWindow(projectName);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
