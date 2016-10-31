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

using GoogleCloudExtension.Utils;
using System.Threading.Tasks;

namespace GoogleCloudExtension.ProgressDialog
{
    public class ProgressDialogWindowViewModel : ViewModelBase
    {
        private readonly ProgressDialogWindow _owner;
        private readonly Task _task;
        private readonly ProgressDialogWindow.Options _options;

        public string Message { get; }

        public ProtectedCommand CancelCommand { get; }

        public bool WasCancelled { get; set; }

        public string CancelToolTip => _options.CancelToolTip;

        public ProgressDialogWindowViewModel(ProgressDialogWindow owner, ProgressDialogWindow.Options options, Task task)
        {
            _owner = owner;
            _task = task;
            _options = options;

            Message = _options.Message;
            CancelCommand = new ProtectedCommand(OnCancelCommand, canExecuteCommand: _options.IsCancellable);

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
