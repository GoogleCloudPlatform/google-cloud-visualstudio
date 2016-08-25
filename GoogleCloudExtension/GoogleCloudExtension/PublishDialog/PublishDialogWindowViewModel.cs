using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialog
{
    public class PublishDialogWindowViewModel : ViewModelBase
    {
        private readonly PublishDialogWindow _owner;
        private readonly Stack<IPublishDialogStep> _stack = new Stack<IPublishDialogStep>();
        private FrameworkElement _content;

        public FrameworkElement Content
        {
            get { return _content; }
            set { SetValueAndRaise(ref _content, value); }
        }

        public WeakCommand PrevCommand { get; }

        public WeakCommand NextCommand { get; }

        public WeakCommand PublishCommand { get; }

        private IPublishDialogStep CurrentStep => _stack.Peek();

        public PublishDialogWindowViewModel(IPublishDialogStep initialStep, PublishDialogWindow owner)
        {
            _owner = owner;


            PrevCommand = new WeakCommand(OnPrevCommand);
            NextCommand = new WeakCommand(OnNextCommand);
            PublishCommand = new WeakCommand(OnPublishCommand);

            CurrentStepChanged();
        }

        private void OnNextCommand()
        {
            var nextStep = CurrentStep.Next();
            PushStep(nextStep);
        }

        private void OnPrevCommand()
        {
            PopStep();
        }

        private void OnPublishCommand()
        {
            CurrentStep.Publish();
        }

        private void PushStep(IPublishDialogStep step)
        {
            _stack.Push(step);
            CurrentStepChanged();
        }

        private void PopStep()
        {
            _stack.Pop();
            CurrentStepChanged();
        }

        private void CurrentStepChanged()
        {
            Content = CurrentStep.Content;
            PrevCommand.CanExecuteCommand = _stack.Count > 1;
            NextCommand.CanExecuteCommand = CurrentStep.CanGoNext;
            PublishCommand.CanExecuteCommand = CurrentStep.CanPublish;
        }
    }
}
