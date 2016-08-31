// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.ManageWindowsCredentials;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtension.PublishDialogSteps.GceStep
{
    public class GceStepViewModel : PublishDialogStepBase
    {
        private readonly GceStepContent _content;
        private IPublishDialog _publishDialog;
        private Instance _selectedInstance;
        private IEnumerable<WindowsInstanceCredentials> _credentials;
        private WindowsInstanceCredentials _selectedCredentials;
        private bool _busy;

        public AsyncPropertyValue<IEnumerable<Instance>> Instances { get; }

        public Instance SelectedInstance
        {
            get { return _selectedInstance; }
            set
            {
                SetValueAndRaise(ref _selectedInstance, value);
                UpdateCredentials();
                ManageCredentialsCommand.CanExecuteCommand = value != null;
            }
        }

        public IEnumerable<WindowsInstanceCredentials> Credentials
        {
            get { return _credentials; }
            private set { SetValueAndRaise(ref _credentials, value); }
        }

        public WindowsInstanceCredentials SelectedCredentials
        {
            get { return _selectedCredentials; }
            set
            {
                SetValueAndRaise(ref _selectedCredentials, value);
                RaisePropertyChanged(nameof(HasSelectedCredentials));
                CanPublish = value != null;
            }
        }

        public bool HasSelectedCredentials => SelectedCredentials != null;

        public WeakCommand ManageCredentialsCommand { get; }

        public bool Busy
        {
            get { return _busy; }
            set { SetValueAndRaise(ref _busy, value); }
        }

        private GceStepViewModel(GceStepContent content)
        {
            _content = content;

            Instances = AsyncPropertyValueUtils.CreateAsyncProperty(GetAllWindowsInstances());

            ManageCredentialsCommand = new WeakCommand(OnManageCredentialsCommand, canExecuteCommand: false);
        }

        private void OnManageCredentialsCommand()
        {
            ManageWindowsCredentialsWindow.PromptUser(SelectedInstance);
            UpdateCredentials();
        }

        private async Task<IEnumerable<Instance>> GetAllWindowsInstances()
        {
            var dataSource = new GceDataSource(
                CredentialsStore.Default.CurrentProjectId,
                CredentialsStore.Default.CurrentGoogleCredential,
                GoogleCloudExtensionPackage.ApplicationName);
            var instances = await dataSource.GetInstanceListAsync();
            return instances.Where(x => x.IsRunning() && x.IsWindowsInstance()).OrderBy(x => x.Name);
        }

        #region IPublishDialogStep

        public override FrameworkElement Content => _content;

        public override void Publish()
        {
            GcpOutputWindow.Activate();
            GcpOutputWindow.Clear();
            GcpOutputWindow.OutputLine($"Publishing {_publishDialog.Project.Name} to Compute Engine");
            AspNetPublisher.StartPublishApp(
                _publishDialog.Project,
                SelectedInstance,
                SelectedCredentials,
                (l) => GcpOutputWindow.OutputLine(l));
            _publishDialog.Finished();
        }

        public override void OnPushedToDialog(IPublishDialog dialog)
        {
            _publishDialog = dialog;
        }

        #endregion

        internal static GceStepViewModel CreateStep()
        {
            var content = new GceStepContent();
            var viewModel = new GceStepViewModel(content);
            content.DataContext = viewModel;

            return viewModel;
        }

        private void UpdateCredentials()
        {
            if (SelectedInstance == null)
            {
                Credentials = Enumerable.Empty<WindowsInstanceCredentials>();
            }
            else
            {
                Credentials = WindowsCredentialsStore.Default.GetCredentialsForInstance(SelectedInstance);
            }
            SelectedCredentials = Credentials.FirstOrDefault();
        }
    }
}
