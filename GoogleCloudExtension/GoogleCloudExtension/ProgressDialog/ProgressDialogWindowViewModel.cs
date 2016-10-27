using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.ProgressDialog
{
    public class ProgressDialogWindowViewModel : ViewModelBase
    {
        private readonly ProgressDialogWindow _owner;
        private readonly Task _task;

        public string Message { get; }

        public ProtectedCommand CancelCommand { get; }

        public bool WasCancelled { get; private set; }

        public ProgressDialogWindowViewModel(ProgressDialogWindow owner, string message, Task task)
        {
            _owner = owner;
            _task = task;

            Message = message;
            CancelCommand = new ProtectedCommand(OnCancelCommand);

            CloseOnTaskCompletion();
        }

        private void CloseOnTaskCompletion()
        {
            _task.ContinueWith(t =>
            {
                _owner.Dispatcher.Invoke(CloseOwner);
            });
        }

        private void CloseOwner()
        {
            if (WasCancelled)
            {
                return;
            }
            _owner.Close();
        }

        private void OnCancelCommand()
        {
            WasCancelled = true;
            _owner.Close();
        }
    }
}
