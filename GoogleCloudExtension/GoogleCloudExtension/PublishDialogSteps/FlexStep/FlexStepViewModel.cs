using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialogSteps.FlexStep
{
    public class FlexStepViewModel : PublishDialogStepBase
    {
        private readonly FlexStepContent _content;
        private IPublishDialog _publishDialog;
        private string _version;
        private bool _promote;

        public string Version
        {
            get { return _version; }
            set { SetValueAndRaise(ref _version, value); }
        }

        public bool Promote
        {
            get { return _promote; }
            set { SetValueAndRaise(ref _promote, value); }
        }

        private FlexStepViewModel(FlexStepContent content)
        {
            _content = content;

            CanPublish = true;
        }

        #region IPublishDialogStep

        public override FrameworkElement Content => _content;

        public override void OnPushedToDialog(IPublishDialog dialog)
        {
            _publishDialog = dialog;
        }

        public override async void Publish()
        {
            var options = new NetCoreDeployment.DeploymentOptions { Version = Version, Promote = Promote };
            var project = _publishDialog.Project;

            GcpOutputWindow.Activate();
            GcpOutputWindow.Clear();
            GcpOutputWindow.OutputLine(String.Format(Resources.GcePublishStepStartMessage, project.Name));

            _publishDialog.FinishFlow();

            var result = await NetCoreDeployment.PublishProjectAsync(
                project.FullPath,
                options,
                (l) => GcpOutputWindow.OutputLine(l));
            if (result)
            {
                GcpOutputWindow.OutputLine($"Project {project.Name} deployed to App Engine Flex.");
                /*if (OpenWebsite)
                {
                    var url = SelectedInstance.GetDestinationAppUri();
                    Process.Start(url);
                }*/
            }
            else
            {
                GcpOutputWindow.OutputLine($"Failed to deploy project {project.Name} to App Engine Flex.");
            }
        }

        #endregion

        internal static FlexStepViewModel CreateStep()
        {
            var content = new FlexStepContent();
            var viewModel = new FlexStepViewModel(content);
            content.DataContext = viewModel;

            return viewModel;
        }
    }
}
