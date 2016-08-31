using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.GceStep;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace GoogleCloudExtension.PublishDialogSteps.ChoiceStep
{
    public class ChoiceStepViewModel : PublishDialogStepBase
    {
        private const string AppEngineIconPath = "PublishDialogSteps/ChoiceStep/Resources/AppEngine_128px_Retina.png";
        private const string GceIconPath = "PublishDialogSteps/ChoiceStep/Resources/ComputeEngine_128px_Retina.png";

        private static readonly Lazy<ImageSource> s_appEngineIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(AppEngineIconPath));
        private static readonly Lazy<ImageSource> s_gceIcon = new Lazy<ImageSource>(() => ResourceUtils.LoadImage(GceIconPath));

        private readonly ChoiceStepContent _content;
        private IPublishDialog _dialog;
        private IEnumerable<Choice> _choices;

        public IEnumerable<Choice> Choices
        {
            get { return _choices; }
            set { SetValueAndRaise(ref _choices, value); }
        }

        private ChoiceStepViewModel(ChoiceStepContent content)
        {
            _content = content;
        }

        private IEnumerable<Choice> GetChoicesForCurrentProject()
        {
            var projectType = _dialog.Project.GetProjectType();

            return new List<Choice>
            {
                new Choice
                {
                    Name = "App Engine Flex",
                    Command = new WeakCommand(
                        OnAppEngineChoiceCommand,
                        canExecuteCommand: false),
                    Icon = s_appEngineIcon.Value,
                    ToolTip = "Deploy to App Engine Flex"
                },
                new Choice
                {
                    Name = "Compute Engine",
                    Command = new WeakCommand(
                        OnGceChoiceCommand,
                        canExecuteCommand: projectType == KnownProjectTypes.WebApplication),
                    Icon = s_gceIcon.Value,
                    ToolTip = "Deploy to Compute Engine"
                },
            };
        }

        private void OnAppEngineChoiceCommand()
        {
            throw new NotImplementedException();
        }

        private void OnGceChoiceCommand()
        {
            var nextStep = GceStepViewModel.CreateStep();
            _dialog.PushStep(nextStep);
        }

        #region IPublishDialogStep

        public override FrameworkElement Content => _content;

        public override void OnPushedToDialog(IPublishDialog dialog)
        {
            _dialog = dialog;

            Choices = GetChoicesForCurrentProject();
        }

        #endregion

        public static IPublishDialogStep CreateStep()
        {
            var content = new ChoiceStepContent();
            var viewModel = new ChoiceStepViewModel(content);
            content.DataContext = viewModel;

            return viewModel;
        }
    }
}
