using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GoogleCloudExtension.UploadProgressDialog
{
    class UploadProgressDialogViewModel : ViewModelBase
    {
        private readonly UploadProgressDialogWindow _owner;
        private readonly CancellationTokenSource _tokenSource;
        private int _completed = 0;
        private string _caption = Resources.UiCancelButtonCaption;

        public ObservableCollection<UploadOperation> Operations { get; }

        public string Caption
        {
            get { return _caption; }
            set { SetValueAndRaise(ref _caption, value); }
        }

        public ICommand ActionCommand { get; }

        private bool IsComplete => _completed >= Operations.Count;

        public UploadProgressDialogViewModel(
            UploadProgressDialogWindow owner,
            IEnumerable<UploadOperation> operations,
            CancellationTokenSource tokenSource)
        {
            _owner = owner;
            _tokenSource = tokenSource;

            Operations = new ObservableCollection<UploadOperation>(operations);
            foreach (var operation in Operations)
            {
                operation.Completed += OnOperationCompleted;
            }

            ActionCommand = new ProtectedCommand(OnActionCommand);
        }

        private void OnOperationCompleted(object sender, EventArgs e)
        {
            _completed++;
            if (IsComplete)
            {
                Caption = Resources.UiCloseButtonCaption;
            }
        }

        private void OnActionCommand()
        {
            if (!IsComplete)
            {
                _tokenSource.Cancel();
            }
            _owner.Close();
        }
    }
}
