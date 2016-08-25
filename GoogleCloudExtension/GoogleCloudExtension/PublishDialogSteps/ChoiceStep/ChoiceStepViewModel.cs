using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialogSteps.ChoiceStep
{
    public class ChoiceStepViewModel : ViewModelBase, IPublishDialogStep
    {
        private readonly ChoiceStepContent _content;

        public IEnumerable<Choice> Choices { get; }

        private ChoiceStepViewModel(ChoiceStepContent content)
        {
            _content = content;

            Choices = GetChoicesForCurrentProject();
        }

        private IEnumerable<Choice> GetChoicesForCurrentProject()
        {
            var command = new WeakCommand<Choice>(OnChoiceCommand);

            return new List<Choice>
            {
                new Choice { Name = "App Engine", Command = command },
                new Choice { Name = "Compute Engine", Command = command },
            };
        }

        private void OnChoiceCommand(Choice obj)
        {
            throw new NotImplementedException();
        }

        #region IPublishDialogStep

        bool IPublishDialogStep.CanGoNext => false;

        bool IPublishDialogStep.CanPublish => false;

        FrameworkElement IPublishDialogStep.Content => _content;

        IPublishDialogStep IPublishDialogStep.Next()
        {
            throw new NotImplementedException();
        }

        void IPublishDialogStep.Publish()
        {
            throw new NotImplementedException();
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
