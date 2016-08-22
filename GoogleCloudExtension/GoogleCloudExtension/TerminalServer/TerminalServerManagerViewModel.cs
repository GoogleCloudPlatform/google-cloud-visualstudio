using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.ManageWindowsCredentials;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace GoogleCloudExtension.TerminalServer
{
    public class TerminalServerManagerViewModel : ViewModelBase
    {
        private static readonly IEnumerable<WindowsInstanceCredentials> s_addNewCredentials = new List<WindowsInstanceCredentials>
        {
            new WindowsInstanceCredentials
            {
                User = Resources.TerminalServerManagerNoCredentialsFoundMessage,
            }
        };

        private readonly Instance _instance;
        private readonly TerminalServerManagerWindow _owner;
        private IEnumerable<WindowsInstanceCredentials> _instanceCredentials;
        private WindowsInstanceCredentials _currentCredentials;
        private bool _hasCredentials;

        public IEnumerable<WindowsInstanceCredentials> InstanceCredentials
        {
            get { return _instanceCredentials; }
            set { SetValueAndRaise(ref _instanceCredentials, value); }
        }

        public WeakCommand OkCommand { get; }

        public ICommand ManageCredentialsCommand { get; }

        public WindowsInstanceCredentials CurrentCredentials
        {
            get { return _currentCredentials; }
            set { SetValueAndRaise(ref _currentCredentials, value); }
        }

        public bool HasCredentials
        {
            get { return _hasCredentials; }
            set { SetValueAndRaise(ref _hasCredentials, value); }
        }

        public TerminalServerManagerViewModel(Instance instance, TerminalServerManagerWindow owner)
        {
            _instance = instance;
            _owner = owner;

            OkCommand = new WeakCommand(OnOkCommand, canExecuteCommand: false);
            ManageCredentialsCommand = new WeakCommand(OnManageCredentialsCommand);

            LoadCredentials();
        }

        private void OnManageCredentialsCommand()
        {
            ManageWindowsCredentialsWindow.PromptUser(_instance);
            LoadCredentials();
        }

        private void LoadCredentials()
        {
            InstanceCredentials = WindowsCredentialsStore.Default.GetCredentialsForInstance(_instance);
            if (InstanceCredentials.Count() == 0)
            {
                InstanceCredentials = s_addNewCredentials;
                HasCredentials = false;
            }
            else
            {
                HasCredentials = true;
            }
            OkCommand.CanExecuteCommand = HasCredentials;
            if (CurrentCredentials == null)
            {
                CurrentCredentials = InstanceCredentials.FirstOrDefault();
            }
        }

        private void OnOkCommand()
        {
            TerminalServerManager.OpenSession(_instance, _currentCredentials);
            _owner.Close();
        }
    }
}
