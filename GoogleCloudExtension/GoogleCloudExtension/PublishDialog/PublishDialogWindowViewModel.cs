using EnvDTE;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialog
{
    public class PublishDialogWindowViewModel : ViewModelBase, IPublishDialog
    {
        private readonly PublishDialogWindow _owner;
        private readonly Project _project;
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

        public PublishDialogWindowViewModel(Project project, IPublishDialogStep initialStep, PublishDialogWindow owner)
        {
            _owner = owner;
            _project = project;

            PrevCommand = new WeakCommand(OnPrevCommand);
            NextCommand = new WeakCommand(OnNextCommand);
            PublishCommand = new WeakCommand(OnPublishCommand);

            PushStep(initialStep);
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
            RemoveStepEvents();
            _stack.Push(step);
            AddStepEvents();

            step.OnPushedToDialog(this);
            CurrentStepChanged();
        }

        private void AddStepEvents()
        {
            var top = _stack.Peek();
            top.CanGoNextChanged += OnCanGoNextChanged;
            top.CanPublishChanged += OnCanPublishChanged;
        }

        private void RemoveStepEvents()
        {
            if (_stack.Count > 0)
            {
                var top = _stack.Peek();
                top.CanGoNextChanged -= OnCanGoNextChanged;
                top.CanPublishChanged -= OnCanPublishChanged;
            }
        }

        private void PopStep()
        {
            RemoveStepEvents();
            _stack.Pop();
            AddStepEvents();

            CurrentStepChanged();
        }

        private void CurrentStepChanged()
        {
            Content = CurrentStep.Content;
            PrevCommand.CanExecuteCommand = _stack.Count > 1;
            NextCommand.CanExecuteCommand = CurrentStep.CanGoNext;
            PublishCommand.CanExecuteCommand = CurrentStep.CanPublish;
        }

        private void OnCanPublishChanged(object sender, EventArgs e)
        {
            PublishCommand.CanExecuteCommand = CurrentStep.CanPublish;
        }

        private void OnCanGoNextChanged(object sender, EventArgs e)
        {
            NextCommand.CanExecuteCommand = CurrentStep.CanGoNext;
        }

        #region IPublishDialog

        Project IPublishDialog.Project => _project;

        void IPublishDialog.PushStep(IPublishDialogStep step)
        {
            PushStep(step);
        }

        void IPublishDialog.Finished()
        {
            _owner.Close();
        }

        #endregion
    }
}
