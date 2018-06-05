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
using GoogleCloudExtension.PublishDialog.Steps.Flex;
using GoogleCloudExtension.PublishDialog.Steps.Gce;
using GoogleCloudExtension.PublishDialog.Steps.Gke;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public const string GaeChoiceId = "Gae";
        public const string GkeChoiceId = "Gke";
        public const string GceChoiceId = "Gce";
        public const string GoogleCloudPublishChoicePropertyName = "GoogleCloudPublishChoice";

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

        public IProtectedCommand PublishCommand { get; } =
            new ProtectedCommand(() => throw new NotSupportedException(), false);

        private IPublishDialog PublishDialog { get; }

        public ChoiceStepViewModel(IPublishDialog publishDialog)
        {
            PublishDialog = publishDialog;
        }

        /// <summary>
        /// Called every time that this step is at the top of the navigation stack and therefore visible.
        /// </summary>
        public void OnVisible()
        {
            AddHandlers();
            Choices = GetChoicesForCurrentProject();
        }

        /// <summary>
        /// Called every time that this step is no longer at the top of the navigation stack.
        /// </summary>
        public void OnNotVisible() => RemoveHandlers();

        /// <summary>
        /// Called when the dialog first loads to move past this step to the prevously chosen step, if it exists.
        /// </summary>
        public void ExecutePreviousChoice()
        {
            string previousChoiceId = PublishDialog.Project.GetUserProperty(GoogleCloudPublishChoicePropertyName);
            if (!string.IsNullOrEmpty(previousChoiceId))
            {
                Choices.FirstOrDefault(c => c.Id == previousChoiceId)?.Command.Execute(null);
            }
        }

        private IEnumerable<Choice> GetChoicesForCurrentProject()
        {
            return new List<Choice>
            {
                new Choice(
                    GaeChoiceId,
                    Resources.PublishDialogChoiceStepAppEngineFlexName,
                    Resources.PublishDialogChoiceStepAppEngineToolTip,
                    s_appEngineIcon.Value,
                    new ProtectedCommand(OnAppEngineChoiceCommand, PublishDialog.Project.IsAspNetCoreProject())),
                new Choice(
                    GkeChoiceId,
                    Resources.PublishDialogChoiceStepGkeName,
                    Resources.PublishDialogChoiceStepGkeToolTip,
                    s_gkeIcon.Value,
                    new ProtectedCommand(OnGkeChoiceCommand, PublishDialog.Project.IsAspNetCoreProject())),
                new Choice(
                    GceChoiceId,
                    Resources.PublishDialogChoiceStepGceName,
                    Resources.PublishDialogChoiceStepGceToolTip,
                    s_gceIcon.Value,
                    new ProtectedCommand(OnGceChoiceCommand, PublishDialog.Project.ProjectType == KnownProjectTypes.WebApplication))
            };
        }

        private void OnGkeChoiceCommand()
        {
            PublishDialog.Project.SaveUserProperty(GoogleCloudPublishChoicePropertyName, GkeChoiceId);
            var nextStep = new GkeStepContent(PublishDialog);
            PublishDialog.NavigateToStep(nextStep);
        }

        private void OnAppEngineChoiceCommand()
        {
            PublishDialog.Project.SaveUserProperty(GoogleCloudPublishChoicePropertyName, GaeChoiceId);
            var nextStep = new FlexStepContent(PublishDialog);
            PublishDialog.NavigateToStep(nextStep);
        }

        private void OnGceChoiceCommand()
        {
            PublishDialog.Project.SaveUserProperty(GoogleCloudPublishChoicePropertyName, GceChoiceId);
            var nextStep = new GceStepContent(PublishDialog);
            PublishDialog.NavigateToStep(nextStep);
        }

        private void OnFlowFinished(object sender, EventArgs e)
        {
            PublishDialog.Project.DeleteUserProperty(GoogleCloudPublishChoicePropertyName);
            RemoveHandlers();
        }

        private void AddHandlers()
        {
            PublishDialog.FlowFinished += OnFlowFinished;
        }

        private void RemoveHandlers()
        {
            PublishDialog.FlowFinished -= OnFlowFinished;
        }
    }
}
