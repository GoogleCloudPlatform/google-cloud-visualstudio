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
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.FlexStep;
using GoogleCloudExtension.PublishDialogSteps.GceStep;
using GoogleCloudExtension.PublishDialogSteps.GkeStep;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace GoogleCloudExtension.PublishDialogSteps.ChoiceStep
{
    /// <summary>
    /// The view model for the publish dialog step that allows the user to choose the target
    /// for the publish process. This is always the first step to be shown in the wizard, and it
    /// navigates to the step specified by the user pressing the choice button.
    /// </summary>
    public class ChoiceStepViewModel : ValidatingViewModelBase, IPublishDialogStep
    {
        private const string AppEngineIconPath = "PublishDialogSteps/ChoiceStep/Resources/AppEngine_128px_Retina.png";
        private const string GceIconPath = "PublishDialogSteps/ChoiceStep/Resources/ComputeEngine_128px_Retina.png";
        private const string GkeIconPath = "PublishDialogSteps/ChoiceStep/Resources/ContainerEngine_128px_Retina.png";

        private static readonly Lazy<ImageSource> s_appEngineIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(AppEngineIconPath));
        private static readonly Lazy<ImageSource> s_gceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(GceIconPath));
        private static readonly Lazy<ImageSource> s_gkeIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(GkeIconPath));

        private readonly ChoiceStepContent _content;
        private IEnumerable<Choice> _choices = Enumerable.Empty<Choice>();

        // Disable compiler error CS0067.
        public event EventHandler CanGoNextChanged
        {
            add { }
            remove { }
        }
        public event EventHandler CanPublishChanged
        {
            add { }
            remove { }
        }

        /// <summary>
        /// The choices available for the project being published.
        /// </summary>
        public IEnumerable<Choice> Choices
        {
            get { return _choices; }
            set { SetValueAndRaise(ref _choices, value); }
        }

        protected internal IPublishDialog PublishDialog { get; private set; }

        /// <inheritdoc />
        public FrameworkElement Content => _content;

        /// <inheritdoc />
        public bool CanGoNext => false;

        /// <inheritdoc />
        public bool CanPublish => false;

        private ChoiceStepViewModel(ChoiceStepContent content)
        {
            _content = content;
        }

        public void OnVisible(IPublishDialog dialog)
        {
            PublishDialog = dialog;
            AddHandlers();
            Choices = GetChoicesForCurrentProject();
        }

        /// <summary>
        /// This step never goes next. <see cref="IPublishDialogStep.CanGoNext"/> is always <code>false</code>
        /// </summary>
        public IPublishDialogStep Next()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// This step never publishes. <see cref="IPublishDialogStep.CanPublish"/> is always <code>false</code>
        /// </summary>
        public void Publish()
        {
            throw new NotSupportedException();
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
            var nextStep = GkeStepViewModel.CreateStep();
            PublishDialog.NavigateToStep(nextStep);
        }

        private void OnAppEngineChoiceCommand()
        {
            var nextStep = FlexStepViewModel.CreateStep();
            PublishDialog.NavigateToStep(nextStep);
        }

        private void OnGceChoiceCommand()
        {
            var nextStep = GceStepViewModel.CreateStep();
            PublishDialog.NavigateToStep(nextStep);
        }

        protected internal virtual void OnFlowFinished()
        {
            RemoveHandlers();
            PublishDialog = null;
            Choices = Enumerable.Empty<Choice>();
        }

        private void OnFlowFinished(object sender, EventArgs e)
            => OnFlowFinished();

        private void AddHandlers()
        {
            // Checking for null in case it was never pushed in a dialog.
            if (PublishDialog != null)
            {
                PublishDialog.FlowFinished += OnFlowFinished;
            }
        }

        private void RemoveHandlers()
        {
            // Checking for null in case it was never pushed in a dialog.
            if (PublishDialog != null)
            {
                PublishDialog.FlowFinished -= OnFlowFinished;
            }
        }

        public static ChoiceStepViewModel CreateStep()
        {
            var content = new ChoiceStepContent();
            var viewModel = new ChoiceStepViewModel(content);
            content.DataContext = viewModel;

            return viewModel;
        }
    }
}
