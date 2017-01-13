using Google.Apis.Container.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialogSteps.GkeStep
{
    public class GkeStepViewModel : PublishDialogStepBase
    {
        private readonly GkeStepContent _content;
        private IPublishDialog _publishDialog;
        private Cluster _selectedCluster;
        private string _deploymentName;
        private string _deploymentVersion;
        private bool _exposeService = true;
        private bool _openWebsite = true;

        public AsyncPropertyValue<IList<Cluster>> Clusters { get; }

        public Cluster SelectedCluster
        {
            get { return _selectedCluster; }
            set
            {
                SetValueAndRaise(ref _selectedCluster, value);
                CanPublish = value != null;
            }
        }

        public string DeploymentName
        {
            get { return _deploymentName; }
            set { SetValueAndRaise(ref _deploymentName, value); }
        }

        public string DeploymentVersion
        {
            get { return _deploymentVersion; }
            set { SetValueAndRaise(ref _deploymentVersion, value); }
        }

        public bool ExposeService
        {
            get { return _exposeService; }
            set { SetValueAndRaise(ref _exposeService, value); }
        }

        public bool OpenWebsite
        {
            get { return _openWebsite; }
            set { SetValueAndRaise(ref _openWebsite, value); }
        }

        public GkeStepViewModel(GkeStepContent content)
        {
            _content = content;

            Clusters = new AsyncPropertyValue<IList<Cluster>>(GetAllClusters());
        }

        #region IPublishDialogStep overrides

        public override FrameworkElement Content => _content;

        public override void OnPushedToDialog(IPublishDialog dialog)
        {
            _publishDialog = dialog;

            DeploymentName = _publishDialog.Project.Name.ToLower();
            DeploymentVersion = GetDefaultVersion();
        }

        public override async void Publish()
        {
            var context = new GCloudContext
            {
                CredentialsPath = CredentialsStore.Default.CurrentAccountPath,
                ProjectId = CredentialsStore.Default.CurrentProjectId,
                AppName = GoogleCloudExtensionPackage.ApplicationName,
                AppVersion = GoogleCloudExtensionPackage.ApplicationVersion,
            };
            var options = new GkeDeployment.DeploymentOptions
            {
                Cluster = SelectedCluster.Name,
                Zone = SelectedCluster.Zone,
                DeploymentName = DeploymentName,
                DeploymentVersion = DeploymentVersion,
                ExposeService = ExposeService,
                Context = context,
                WaitingForServiceIpCallback = () => GcpOutputWindow.OutputLine("Waiting for Service IP address...")
            };
            var project = _publishDialog.Project;

            GcpOutputWindow.Activate();
            GcpOutputWindow.Clear();
            GcpOutputWindow.OutputLine($"Deploying {project.Name} to Container Engine");

            _publishDialog.FinishFlow();

            GkeDeploymentResult result;
            using (var frozen = StatusbarHelper.Freeze())
            using (var animationShown = StatusbarHelper.ShowDeployAnimation())
            using (var progress = StatusbarHelper.ShowProgressBar("Deploying to Container Engine"))
            using (var deployingOperation = ShellUtils.SetShellUIBusy())
            {
                result = await GkeDeployment.PublishProjectAsync(
                    project.FullPath,
                    options,
                    progress,
                    GcpOutputWindow.OutputLine);
            }

            if (result != null)
            {
                GcpOutputWindow.OutputLine($"Project {project.Name} deployed to Container Engine");
                if (result.WasExposed)
                {
                    if (result.ServiceIpAddress != null)
                    {
                        GcpOutputWindow.OutputLine($"Service {DeploymentName} ip address {result.ServiceIpAddress}");
                    }
                    else
                    {
                        GcpOutputWindow.OutputLine("Time out waiting for service ip address.");
                    }
                }
                StatusbarHelper.SetText(Resources.PublishSuccessStatusMessage);

                if (OpenWebsite && result.WasExposed && result.ServiceIpAddress != null)
                {
                    Process.Start($"http://{result.ServiceIpAddress}");
                }
            }
            else
            {
                GcpOutputWindow.OutputLine($"Failed to deploy {project.Name} to Container Engine");
                StatusbarHelper.SetText(Resources.PublishFailureStatusMessage);
            }
        }

        #endregion

        private async Task<IList<Cluster>> GetAllClusters()
        {
            var dataSource = new GkeDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            var clusters = await dataSource.GetClusterListAsync();
            return clusters.OrderBy(x => x.Name).ToList();

        }

        /// <summary>
        /// Creates a GKE step complete with behavior and visuals.
        /// </summary>
        /// <returns></returns>
        internal static GkeStepViewModel CreateStep()
        {
            var content = new GkeStepContent();
            var viewModel = new GkeStepViewModel(content);
            content.DataContext = viewModel;

            return viewModel;
        }

        private static string GetDefaultVersion()
        {
            var now = DateTime.Now;
            return String.Format(
                "{0:0000}{1:00}{2:00}t{3:00}{4:00}{5:00}",
                now.Year, now.Month, now.Day,
                now.Hour, now.Minute, now.Second);
        }
    }
}
