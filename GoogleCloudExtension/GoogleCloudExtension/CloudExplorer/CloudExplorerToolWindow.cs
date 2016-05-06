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
using GoogleCloudExtension.CloudExplorerSources.Gce;
using GoogleCloudExtension.CloudExplorerSources.Gcs;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace GoogleCloudExtension.CloudExplorer
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("fe34c2aa-59b3-40ad-a3b6-2743d072d2aa")]
    public class CloudExplorerToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloudExplorerToolWindow"/> class.
        /// </summary>
        public CloudExplorerToolWindow() : base(null)
        {
            SetCaption();

            // Contains the list of sources to display to the user, in the order they will
            // be displayed.
            var sources = new List<ICloudExplorerSource>
            {
                new GceSource(),
                new GcsSource(),
            };


            var model = new CloudExplorerViewModel(sources);
            var content = new CloudExplorerToolWindowControl(this) { DataContext = model };
            Content = content;

            CredentialsStore.Default.CurrentAccountChanged += OnCurrentAccountChanged;
            CredentialsStore.Default.Reset += OnCurrentAccountChanged;
        }

        private void OnCurrentAccountChanged(object sender, EventArgs e)
        {
            SetCaption();
        }

        private void SetCaption()
        {
            if (CredentialsStore.Default.CurrentAccount?.AccountName != null)
            {
                Caption = $"Google Cloud Explorer ({CredentialsStore.Default.CurrentAccount.AccountName})";
            }
            else
            {
                Caption = "Google Cloud Explorer (select credentials)";
            }
        }
    }
}
