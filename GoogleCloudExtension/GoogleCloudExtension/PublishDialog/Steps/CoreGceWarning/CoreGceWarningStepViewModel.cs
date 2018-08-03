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

using GoogleCloudExtension.Projects;
using GoogleCloudExtension.PublishDialog.Steps.Choice;
using GoogleCloudExtension.PublishDialog.Steps.Gce;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Validation;
using System;

namespace GoogleCloudExtension.PublishDialog.Steps.CoreGceWarning
{
    public class CoreGceWarningStepViewModel : ValidatingViewModelBase, IPublishDialogStep
    {
        public const string AspNetCoreIisDocsLink = "https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/";
        private readonly IPublishDialog _publishDialog;
        private readonly Lazy<IBrowserService> _browserService;

        /// <summary>
        /// The title of the publish dialog in this step.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// A disabled command that throws <see cref="NotSupportedException"/>.
        /// </summary>
        public IProtectedCommand PublishCommand { get; } =
            new ProtectedCommand(() => throw new NotSupportedException(), false);

        /// <summary>
        /// The command to continue on to the GCE publish step.
        /// </summary>
        public ProtectedCommand OkCommand { get; }

        /// <summary>
        /// The command to open the browser to docs for hosting ASP.NET Core on IIS.
        /// </summary>
        public ProtectedCommand BrowseAspNetCoreIisDocs { get; }
        private IBrowserService BrowserService => _browserService.Value;

        public CoreGceWarningStepViewModel(IPublishDialog publishDialog)
        {
            _publishDialog = publishDialog;
            BrowseAspNetCoreIisDocs = new ProtectedCommand(OnBrowseAspNetCoreIisDocs);
            Title = string.Format(Resources.GcePublishStepTitle, publishDialog.Project.Name);
            OkCommand = new ProtectedCommand(OnOkCommand);
            _browserService = new Lazy<IBrowserService>(
                () => GoogleCloudExtensionPackage.Instance.GetMefService<IBrowserService>());
        }

        private void OnBrowseAspNetCoreIisDocs() =>
            BrowserService.OpenBrowser(AspNetCoreIisDocsLink);

        private void OnOkCommand()
        {
            _publishDialog.Project.SaveUserProperty(
                ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName,
                ChoiceType.Gce.ToString());
            _publishDialog.NavigateToStep(new GceStepContent(_publishDialog));
        }

        /// <summary>
        /// Called every time this step moves on to the top of the navigation stack.
        /// </summary>
        /// <param name="previousStep">The previously shown dialog step.</param>
        public void OnVisible(IPublishDialogStep previousStep)
        {
            if (previousStep is GceStepViewModel)
            {
                // Skip this step when going back, but ensure this warning will be shown next time.
                _publishDialog.Project.DeleteUserProperty(ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName);
                _publishDialog.PopStep();
            }
            else if (previousStep is ChoiceStepViewModel)
            {
                string previousChoiceId =
                    _publishDialog.Project.GetUserProperty(ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName);
                // Skip this warning step if the dialog was previously closed on the GCE step.
                if (Enum.TryParse(previousChoiceId, out ChoiceType previousChoice) && previousChoice == ChoiceType.Gce)
                {
                    _publishDialog.NavigateToStep(new GceStepContent(_publishDialog));
                }
            }
        }

        /// <summary>
        /// Called every time this step moves off the top of the navigation stack.
        /// </summary>
        public void OnNotVisible() { }
    }
}
