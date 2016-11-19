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

using GoogleCloudExtension.Accounts;
using Microsoft.VisualStudio.Shell;
using System;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// The class for changing Tool Window caption.
    /// </summary>
    /// <remarks>
    /// CloudExplorerToolWindow, LogsViewerToolWindow shares the code for updating the caption,
    /// especially when an Account is changed.
    /// </remarks>
    public sealed class ToolWindowCaptionHelper
    {
        private readonly string _noAccountCaption;
        private readonly string _caption;
        private readonly ToolWindowPane _toolWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="ToolWindowCaptionHelper"/> class.
        /// </summary>
        public ToolWindowCaptionHelper(ToolWindowPane toolWindow, string captionFormat, string noAccountCaption)
        {
            _caption = captionFormat;
            _noAccountCaption = noAccountCaption;
            _toolWindow = toolWindow;
            SetCaption();
            CredentialsStore.Default.CurrentAccountChanged += OnCurrentAccountChanged;
            CredentialsStore.Default.Reset += OnCurrentAccountChanged;
        }

        private void OnCurrentAccountChanged(object sender, EventArgs e)
        {
            ErrorHandlerUtils.HandleExceptions(() =>
            {
                SetCaption();
            });
        }

        private void SetCaption()
        {
            if (CredentialsStore.Default.CurrentAccount?.AccountName != null)
            {
                _toolWindow.Caption = String.Format(_caption, CredentialsStore.Default.CurrentAccount.AccountName);
            }
            else
            {
                _toolWindow.Caption = _noAccountCaption;
            }
        }
    }
}
