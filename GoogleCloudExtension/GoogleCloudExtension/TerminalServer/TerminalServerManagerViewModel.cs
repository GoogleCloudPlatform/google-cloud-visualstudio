using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.ManageWindowsCredentials;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.TerminalServer
{
    public class TerminalServerManagerViewModel : ViewModelBase
    {
        private static readonly WindowsInstanceCredentials s_addNewCredentials = new WindowsInstanceCredentials
        {
            User = "Add new credentials...",
            Password = "xxx"
        };

        private readonly Instance _instance;
        private readonly TerminalServerManagerWindow _owner;
        private IEnumerable<WindowsInstanceCredentials> _instanceCredentials;
        private WindowsInstanceCredentials _currentCredentials;

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
            set
            {
                SetValueAndRaise(ref _currentCredentials, value);
                OkCommand.CanExecuteCommand = _currentCredentials != null;
            }
        }

        public TerminalServerManagerViewModel(Instance instance, TerminalServerManagerWindow owner)
        {
            _instance = instance;
            _owner = owner;

            OkCommand = new WeakCommand(OnOkCommand, canExecuteCommand: false);
            ManageCredentialsCommand = new WeakCommand(OnManageCredentialsCommand);

            InstanceCredentials = WindowsCredentialsStore.Default.GetCredentialsForInstance(instance);
            CurrentCredentials = InstanceCredentials.FirstOrDefault();
        }

        private void OnManageCredentialsCommand()
        {
            ManageWindowsCredentialsWindow.PromptUser(_instance);
            InstanceCredentials = WindowsCredentialsStore.Default.GetCredentialsForInstance(_instance);
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
