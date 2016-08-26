using EnvDTE;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialogSteps.GceStep
{
    public class GceStepViewModel : ViewModelBase, IPublishDialogStep
    {
        private readonly GceStepContent _content;
        private readonly Project _currentProject;

        private GceStepViewModel(GceStepContent content)
        {
            _content = content;

            _currentProject = SolutionHelper.CurrentSolution.StartupProject;
        }

        #region IPublishDialogTarget

        bool IPublishDialogStep.CanGoNext => false;

        bool IPublishDialogStep.CanPublish => true;

        FrameworkElement IPublishDialogStep.Content => _content;

        IPublishDialogStep IPublishDialogStep.Next()
        {
            throw new NotImplementedException();
        }

        void IPublishDialogStep.Publish()
        {
            throw new NotImplementedException();
        }

        void IPublishDialogStep.OnPushedToDialog(IPublishDialog dialog)
        { }

        #endregion

        internal static GceStepViewModel CreateStep()
        {
            var content = new GceStepContent();
            var viewModel = new GceStepViewModel(content);
            content.DataContext = viewModel;

            return viewModel;
        }

    }
}
