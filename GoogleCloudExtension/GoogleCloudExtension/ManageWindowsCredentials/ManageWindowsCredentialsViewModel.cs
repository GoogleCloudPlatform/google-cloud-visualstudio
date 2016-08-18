using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ResetPassword;
using GoogleCloudExtension.ShowPassword;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.ManageWindowsCredentials
{
    public class ManageWindowsCredentialsViewModel : ViewModelBase
    {
        private readonly ManageWindowsCredentialsWindow _owner;
        private IEnumerable<WindowsCredentials> _credentials;
        private readonly Instance _instance;
        private WindowsCredentials _selectedCredentials;

        public WeakCommand AddCredentialsCommand { get; }
        
        public WeakCommand DeleteCredentialsCommand { get; }

        public WeakCommand ShowCredentialsCommand { get; }

        public string InstanceName => _instance.Name;

        public WindowsCredentials SelectedCredentials
        {
            get { return _selectedCredentials; }
            set
            {
                SetValueAndRaise(ref _selectedCredentials, value);
                UpdateCommands();
            }
        }

        public IEnumerable<WindowsCredentials> CredentialsList
        {
            get { return _credentials; }
            set { SetValueAndRaise(ref _credentials, value); }
        }

        public ManageWindowsCredentialsViewModel(Instance instance, ManageWindowsCredentialsWindow owner)
        {
            _instance = instance;
            _owner = owner;

            CredentialsList = LoadCredentialsForInstance(instance);

            AddCredentialsCommand = new WeakCommand(OnAddCredentialsCommand);
            DeleteCredentialsCommand = new WeakCommand(OnDeleteCredentialsCommand, canExecuteCommand: false);
            ShowCredentialsCommand = new WeakCommand(OnShowCredentialsCommand, canExecuteCommand: false);
        }

        private IEnumerable<WindowsCredentials> LoadCredentialsForInstance(Instance instance)
        {
            return WindowsCredentialsStore.Default.GetCredentialsForInstance(instance);
        }

        private void OnShowCredentialsCommand()
        {
            ShowPasswordWindow.PromptUser(
                userName: SelectedCredentials.UserName,
                password: SelectedCredentials.Password,
                instanceName: _instance.Name);
        }

        private void OnDeleteCredentialsCommand()
        {
            throw new NotImplementedException();
        }

        private void OnAddCredentialsCommand()
        {
            var credentials = ResetPasswordWindow.PromptUser(_instance, CredentialsStore.Default.CurrentProjectId);
            if (credentials != null)
            {
                WindowsCredentialsStore.Default.AddCredentialsToInstance(_instance, credentials);
                CredentialsList = WindowsCredentialsStore.Default.GetCredentialsForInstance(_instance);
            }
        }

        private void UpdateCommands()
        {
            if (_selectedCredentials == null)
            {
                DeleteCredentialsCommand.CanExecuteCommand = false;
                ShowCredentialsCommand.CanExecuteCommand = false;
            }
            else
            {
                DeleteCredentialsCommand.CanExecuteCommand = true;
                ShowCredentialsCommand.CanExecuteCommand = true;
            }
        }
    }
}
