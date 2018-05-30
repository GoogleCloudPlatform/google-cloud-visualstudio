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
using GoogleCloudExtension.PublishDialog.Steps.Flex;
using GoogleCloudExtension.PublishDialog.Steps.Gce;
using GoogleCloudExtension.PublishDialog.Steps.Gke;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GoogleCloudExtension.PublishDialog.Steps.Choice
{
    /// <summary>
    /// The view model for the publish dialog step that allows the user to choose the target
    /// for the publish process. This is always the first step to be shown in the wizard, and it
    /// navigates to the step specified by the user pressing the choice button.
    /// </summary>
    public class ChoiceStepViewModel : ValidatingViewModelBase, IPublishDialogStep
    {
        private const string AppEngineIconPath = "PublishDialog/Steps/Choice/Resources/AppEngine_128px_Retina.png";
        private const string GceIconPath = "PublishDialog/Steps/Choice/Resources/ComputeEngine_128px_Retina.png";
        private const string GkeIconPath = "PublishDialog/Steps/Choice/Resources/ContainerEngine_128px_Retina.png";

        private static readonly Lazy<ImageSource> s_appEngineIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(AppEngineIconPath));
        private static readonly Lazy<ImageSource> s_gceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(GceIconPath));
        private static readonly Lazy<ImageSource> s_gkeIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(GkeIconPath));

        private IEnumerable<Choice> _choices = Enumerable.Empty<Choice>();

        /// <summary>
        /// The choices available for the project being published.
        /// </summary>
        public IEnumerable<Choice> Choices
        {
            get { return _choices; }
            set { SetValueAndRaise(ref _choices, value); }
        }

        private IPublishDialog PublishDialog { get; }

        public ChoiceStepViewModel(IPublishDialog publishDialog)
        {
            PublishDialog = publishDialog;
        }

        private IEnumerable<Choice> GetChoicesForCurrentProject()
        {
            KnownProjectTypes projectType = PublishDialog.Project.ProjectType;

            return new List<Choice>
            {
                new Choice
                {
                    Name = Resources.PublishDialogChoiceStepAppEngineFlexName,
                    Command = new ProtectedCommand(
                        OnAppEngineChoiceCommand,
                        canExecuteCommand: PublishDialog.Project.IsAspNetCoreProject()),
                    Icon = s_appEngineIcon.Value,
                    ToolTip = Resources.PublishDialogChoiceStepAppEngineToolTip
                },
                new Choice
                {
                    Name = Resources.PublishDialogChoiceStepGkeName,
                    Command = new ProtectedCommand(
                        OnGkeChoiceCommand,
                        canExecuteCommand: PublishDialog.Project.IsAspNetCoreProject()),
                    Icon = s_gkeIcon.Value,
                    ToolTip = Resources.PublishDialogChoiceStepGkeToolTip
                },
                new Choice
                {
                    Name = Resources.PublishDialogChoiceStepGceName,
                    Command = new ProtectedCommand(
                        OnGceChoiceCommand,
                        canExecuteCommand: projectType == KnownProjectTypes.WebApplication),
                    Icon = s_gceIcon.Value,
                    ToolTip = Resources.PublishDialogChoiceStepGceToolTip
                },
            };
        }

        private void OnGkeChoiceCommand()
        {
            var nextStep = new GkeStepContent(PublishDialog);
            PublishDialog.NavigateToStep(nextStep);
        }

        private void OnAppEngineChoiceCommand()
        {
            var nextStep = new FlexStepContent(PublishDialog);
            PublishDialog.NavigateToStep(nextStep);
        }

        private void OnGceChoiceCommand()
        {
            var nextStep = new GceStepContent(PublishDialog);
            PublishDialog.NavigateToStep(nextStep);
        }

        public void OnNotVisible()
        {
            RemoveHandlers();
            Choices = Enumerable.Empty<Choice>();
        }

        private void OnFlowFinished(object sender, EventArgs e) => OnNotVisible();

        private void AddHandlers()
        {
            PublishDialog.FlowFinished += OnFlowFinished;
        }

        private void RemoveHandlers()
        {
            PublishDialog.FlowFinished -= OnFlowFinished;
        }

        public IProtectedCommand PublishCommand { get; } =
            new ProtectedCommand(() => throw new NotSupportedException(), false);

        /// <summary>
        /// Called every time that this step is at the top of the navigation stack and therefore visible.
        /// </summary>
        public Task OnVisibleAsync()
        {
            Choices = GetChoicesForCurrentProject();
            AddHandlers();
            return Task.CompletedTask;
        }
    }
}
