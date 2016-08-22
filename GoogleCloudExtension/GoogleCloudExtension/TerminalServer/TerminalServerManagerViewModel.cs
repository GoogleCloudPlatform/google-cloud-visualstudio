using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.TerminalServer
{
    public class TerminalServerManagerViewModel : ViewModelBase
    {
        private readonly Instance _instance;
        private readonly TerminalServerManagerWindow _owner;
        private WindowsInstanceCredentials _currentCredentials;

        public IEnumerable<WindowsInstanceCredentials> InstanceCredentials { get; }

        public WeakCommand OkCommand { get; }

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

            InstanceCredentials = WindowsCredentialsStore.Default.GetCredentialsForInstance(instance);
            OkCommand = new WeakCommand(OnOkCommand, canExecuteCommand: false);
            CurrentCredentials = InstanceCredentials.FirstOrDefault();
        }

        private void OnOkCommand()
        {
            TerminalServerManager.OpenSession(_instance, _currentCredentials);
            _owner.Close();
        }
    }
}
