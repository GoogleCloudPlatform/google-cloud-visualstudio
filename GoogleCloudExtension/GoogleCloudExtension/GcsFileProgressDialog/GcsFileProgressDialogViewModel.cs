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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;

namespace GoogleCloudExtension.GcsFileProgressDialog
{
    class GcsFileProgressDialogViewModel : ViewModelBase
    {
        private readonly GcsFileProgressDialogWindow _owner;
        private readonly CancellationTokenSource _tokenSource;
        private int _completed = 0;
        private string _caption = Resources.UiCancelButtonCaption;

        public string Message { get; }

        public ObservableCollection<GcsFileOperation> Operations { get; }

        public string Caption
        {
            get { return _caption; }
            set { SetValueAndRaise(ref _caption, value); }
        }

        public ICommand ActionCommand { get; }

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
