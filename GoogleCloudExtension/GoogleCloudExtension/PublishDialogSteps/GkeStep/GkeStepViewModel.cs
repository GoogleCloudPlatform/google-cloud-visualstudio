using Google.Apis.Container.v1.Data;
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
        private string _serviceName;
        private string _serviceVersion;
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

        public string ServiceName
        {
            get { return _serviceName; }
            set { SetValueAndRaise(ref _serviceName, value); }
        }

        public string ServiceVersion
        {
            get { return _serviceVersion; }
            set { SetValueAndRaise(ref _serviceVersion, value); }
        }

        public bool ExposeService
        {
            get { return _exposeService; }
            set { SetValueAndRaise(ref _exposeService, value); }
        }
        
        public GkeStepViewModel(GkeStepContent content)
        {
            _content = content;
        }

        #region IPublishDialogStep overrides

        public override FrameworkElement Content => _content;

        public override void OnPushedToDialog(IPublishDialog dialog)
        {
            _publishDialog = dialog;
        }

        public override void Publish()
        {
            
        }

        #endregion

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
