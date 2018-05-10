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

using GoogleCloudExtension.Utils;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// Base class for attaching remote debugger step view models.
    /// </summary>
    public abstract class AttachDebuggerStepBase : ViewModelBase, IAttachDebuggerStep
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isCancelButtonEnabled;
        private bool _isOKButtonEnabled;
        private bool _isCancelButtonVisible = true;

        protected CancellationToken CancelToken { get; }

        protected AttachDebuggerContext Context { get; }

        public AttachDebuggerStepBase(AttachDebuggerContext context)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CancelToken = _cancellationTokenSource.Token;
            Context = context;
        }

        #region implement interface IAttachDebuggerStep

        public virtual bool IsCancelButtonVisible
        {
            get { return _isCancelButtonVisible; }
            protected set { SetValueAndRaise(ref _isCancelButtonVisible, value); }
        }

        public virtual bool IsCancelButtonEnabled
        {
            get { return _isCancelButtonEnabled; }
            protected set { SetValueAndRaise(ref _isCancelButtonEnabled, value); }
        }

        public virtual ContentControl Content => null;

        public virtual bool IsOKButtonEnabled
        {
            get { return _isOKButtonEnabled; }
            protected set { SetValueAndRaise(ref _isOKButtonEnabled, value); }
        }

        public virtual void OnCancelCommand()
        {
            _cancellationTokenSource.Cancel();
            Context.DialogWindow.Close();
        }

        public abstract Task<IAttachDebuggerStep> OnStartAsync();

        public abstract Task<IAttachDebuggerStep> OnOkCommandAsync();

        #endregion
    }
}
