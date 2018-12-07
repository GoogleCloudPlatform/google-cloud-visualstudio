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
using System;
using System.Threading.Tasks;

namespace GoogleCloudExtension.ProgressDialog
{
    /// <summary>
    /// This class is the view model for the ProgressDialog.
    /// </summary>
    public class ProgressDialogWindowViewModel : ViewModelBase
    {
        private readonly ProgressDialogWindow _owner;
        private readonly Task _task;
        private readonly ProgressDialogWindow.Options _options;
        private bool _closingOwner;

        /// <summary>
        /// The message to show for the user.
        /// </summary>
        public string Message => _options.Message;

        /// <summary>
        /// The command to execute on cancel.
        /// </summary>
        public ProtectedCommand CancelCommand { get; }

        /// <summary>
        /// Whether the operation was cancelled or not.
        /// </summary>
        public bool WasCancelled { get; set; }

        public ProgressDialogWindowViewModel(ProgressDialogWindow owner, ProgressDialogWindow.Options options, Task task)
        {
            _owner = owner;
            _task = task;
            _options = options;

            CancelCommand = new ProtectedCommand(OnCancelCommand, canExecuteCommand: _options.IsCancellable);

            CloseOnTaskCompletion();
            owner.Closed += OnOwnerClosed;
        }

        /// <summary>
        /// This method will wait for the task to complete (whether successfully or not) and then it will
        /// close the dialog.
        /// </summary>
        private void CloseOnTaskCompletion() => ErrorHandlerUtils.HandleExceptionsAsync(
            async () =>
            {
                await _task;
                CloseOwner();
            });

        private void CloseOwner()
        {
            if (WasCancelled)
            {
                return;
            }
            _closingOwner = true;
            _owner.Close();
        }

        private void OnCancelCommand()
        {
            WasCancelled = true;
            _closingOwner = true;
            _owner.Close();
        }

        private void OnOwnerClosed(object sender, EventArgs e)
        {
            // If the view model is the one closing the dialog then nothing to do.
            if (_closingOwner)
            {
                return;
            }

            // The user closed the dialog, this counts as a cancellation.
            WasCancelled = true;
        }
    }
}
