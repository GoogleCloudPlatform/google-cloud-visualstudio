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
using System.Linq;
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
        private bool _hasCancellation;
        private bool _operationsPending = true;
        private string _progressMessage;
        private string _caption = Resources.UiCancelButtonCaption;
        private bool _detailsExpanded = false;
        
        /// <summary>
        /// The message to display in the dialog.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The message for the overall progres.
        /// </summary>
        public string ProgressMessage
        {
            get
            {
                if (IsCancelling)
                {
                    return "Cancelling operation";
                }
                else if (IsCancelled)
                {
                    return "Operation cancelled";
                }
                else if (OperationsPending)
                {
                    return "Operation starting";
                }
                else
                {
                    return String.Format(_progressMessage, Completed, Operations.Count);
                }
            }
        }
           
        public bool OperationsPending
        {
            get { return _operationsPending; }
            private set
            {
                SetValueAndRaise(ref _operationsPending, value);
                RaisePropertyChanged(nameof(ProgressMessage));
            }
        }

        /// <summary>
        /// The list of operations.
        /// </summary>
        public ObservableCollection<GcsFileOperation> Operations { get; }

        /// <summary>
        /// The count of completed operations.
        /// </summary>
        public int Completed
        {
            get { return _completed; }
            private set
            {
                SetValueAndRaise(ref _completed, value);
                RaisePropertyChanged(nameof(ProgressMessage));
            }
        }

        /// <summary>
        /// The caption for the dialog.
        /// </summary>
        public string Caption
        {
            get { return _caption; }
            private set { SetValueAndRaise(ref _caption, value); }
        }

        /// <summary>
        /// The command to execute for the action button in the dialog.
        /// </summary>
        public ICommand ActionCommand { get; }

        public bool DetailsExpanded
        {
            get { return _detailsExpanded; }
            set
            {
                SetValueAndRaise(ref _detailsExpanded, value);
                RaisePropertyChanged(nameof(ExpandCollapseMessage));
            }
        }

        public ICommand ExpandCollapseDetailsCommand { get; }

        public string ExpandCollapseMessage => DetailsExpanded ? "Hide details" : "Show details";

        /// <summary>
        /// Returns whether the operation is complete.
        /// </summary>
        private bool IsComplete => Completed >= Operations.Count;

        /// <summary>
        /// Returns whether the operation was cancelled.
        /// </summary>
        private bool IsCancelled => _hasCancellation && IsComplete;

        /// <summary>
        /// Whether the operations are being cancelled.
        /// </summary>
        private bool IsCancelling => _hasCancellation && !IsComplete;

        public GcsFileProgressDialogViewModel(
            string message,
            string progressMessage,
            GcsFileProgressDialogWindow owner,
            IEnumerable<GcsFileOperation> operations,
            CancellationTokenSource tokenSource)
        {
            _owner = owner;
            _tokenSource = tokenSource;
            _progressMessage = progressMessage;

            Message = message;
            Operations = new ObservableCollection<GcsFileOperation>(operations);
            foreach (var operation in Operations)
            {
                operation.Completed += OnOperationCompleted;
                operation.Started += OnOperationStarted;
            }

            ActionCommand = new ProtectedCommand(OnActionCommand);
            ExpandCollapseDetailsCommand = new ProtectedCommand(OnExpandCollapseDetailsCommand);
        }

        private void OnExpandCollapseDetailsCommand()
        {
            DetailsExpanded = !DetailsExpanded;
        }

        private void OnOperationCompleted(object sender, EventArgs e)
        {
            var operation = (GcsFileOperation)sender;

            _hasCancellation = _hasCancellation || operation.IsCancelled;
            Completed++;
            if (IsComplete)
            {
                Caption = Resources.UiCloseButtonCaption;
            }
        }

        private void OnOperationStarted(object sender, EventArgs e)
        {
            OperationsPending = false;
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
