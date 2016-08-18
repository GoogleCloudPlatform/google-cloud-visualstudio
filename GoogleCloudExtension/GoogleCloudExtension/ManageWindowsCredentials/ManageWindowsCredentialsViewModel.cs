using Google.Apis.Compute.v1.Data;
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

        public IEnumerable<WindowsCredentials> Credentials
        {
            get { return _credentials; }
            set { SetValueAndRaise(ref _credentials, value); }
        }

        public ManageWindowsCredentialsViewModel(Instance instance, ManageWindowsCredentialsWindow owner)
        {
            _instance = instance;
            _owner = owner;

            Credentials = LoadCredentialsForInstance(instance);

            AddCredentialsCommand = new WeakCommand(OnAddCredentialsCommand);
            DeleteCredentialsCommand = new WeakCommand(OnDeleteCredentialsCommand);
            ShowCredentialsCommand = new WeakCommand(OnShowCredentialsCommand);
        }

        private IEnumerable<WindowsCredentials> LoadCredentialsForInstance(Instance instance)
        {
            return WindowsCredentialsStore.Default.GetCredentialsForInstance(instance);
        }

        private void OnShowCredentialsCommand()
        {
            throw new NotImplementedException();
        }

        private void OnDeleteCredentialsCommand()
        {
            throw new NotImplementedException();
        }

        private void OnAddCredentialsCommand()
        {
            throw new NotImplementedException();
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
