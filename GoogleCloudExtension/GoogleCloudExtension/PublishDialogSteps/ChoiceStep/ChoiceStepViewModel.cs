using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.GceStep;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialogSteps.ChoiceStep
{
    public class ChoiceStepViewModel : PublishDialogStepBase
    {
        private readonly ChoiceStepContent _content;
        private IPublishDialog _dialog;

        public IEnumerable<Choice> Choices { get; }

        private ChoiceStepViewModel(ChoiceStepContent content)
        {
            _content = content;

            Choices = GetChoicesForCurrentProject();
        }

        private IEnumerable<Choice> GetChoicesForCurrentProject()
        {
            return new List<Choice>
            {
                new Choice { Name = "App Engine", Command = null },
                new Choice { Name = "Compute Engine", Command = new WeakCommand(OnGceChoiceCommand) },
            };
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
