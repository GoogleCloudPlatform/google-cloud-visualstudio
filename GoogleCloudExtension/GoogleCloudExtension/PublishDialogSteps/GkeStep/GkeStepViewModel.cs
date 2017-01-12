using Google.Apis.Container.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            var now = DateTime.Now;
            DeploymentName = _publishDialog.Project.Name.ToLower();
            DeploymentVersion = $"{now.Year}{now.Month}{now.Day}{now.Hour}{now.Minute}";
        }

        public override void Publish()
        {

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
    }
}
