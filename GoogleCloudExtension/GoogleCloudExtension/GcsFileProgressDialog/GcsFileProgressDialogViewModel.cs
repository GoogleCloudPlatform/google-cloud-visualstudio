// Copyright 2017 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.GcsUtils;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;

namespace GoogleCloudExtension.GcsFileProgressDialog
{
    /// <summary>
    /// The view model for the progress dialog.
    /// </summary>
    public class GcsFileProgressDialogViewModel : ViewModelBase
    {
        private readonly GcsFileProgressDialogWindow _owner;
        private readonly CancellationTokenSource _tokenSource;
        private int _completed = 0;
        private string _caption = Resources.UiCancelButtonCaption;

        /// <summary>
        /// The message to display in the dialog.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The list of operations.
        /// </summary>
        public ObservableCollection<GcsFileOperation> Operations { get; }

        /// <summary>
        /// The caption for the dialog.
        /// </summary>
        public string Caption
        {
            get { return _caption; }
            set { SetValueAndRaise(ref _caption, value); }
        }

        /// <summary>
        /// The command to execute for the action button in the dialog.
        /// </summary>
        public ICommand ActionCommand { get; }

        /// <summary>
        /// Returns whether the operation is complete.
        /// </summary>
        private bool IsComplete => _completed >= Operations.Count;

        public GcsFileProgressDialogViewModel(
            string message,
            GcsFileProgressDialogWindow owner,
            IEnumerable<GcsFileOperation> operations,
            CancellationTokenSource tokenSource)
        {
            _owner = owner;
            _tokenSource = tokenSource;

            Message = message;
            Operations = new ObservableCollection<GcsFileOperation>(operations);
            foreach (var operation in Operations)
            {
                operation.Completed += OnOperationCompleted;
            }

            ActionCommand = new ProtectedCommand(OnActionCommand);
        }

        private void OnOperationCompleted(object sender, EventArgs e)
        {
            _completed += 1;
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
                return;
            }
            _owner.Close();
        }
    }
}
