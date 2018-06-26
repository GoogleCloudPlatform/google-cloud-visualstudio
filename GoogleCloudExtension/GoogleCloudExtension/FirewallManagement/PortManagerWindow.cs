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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Theming;
using System;

namespace GoogleCloudExtension.FirewallManagement
{
    /// <summary>
    /// This class is the dialog to use to prompt the user for firewall changes for the given instance.
    /// </summary>
    public class PortManagerWindow : CommonDialogWindowBase
    {
        internal PortManagerViewModel ViewModel => (PortManagerViewModel)((PortManagerWindowContent)Content).DataContext;

        /// <summary>
        /// Internal hook for unit testing.
        /// </summary>
        internal static event EventHandler WindowActivated;

        private PortManagerWindow(Instance instance) : base(GoogleCloudExtension.Resources.PortManagerWindowCaption)
        {
            var viewModel = new PortManagerViewModel(Close, instance);
            Content = new PortManagerWindowContent
            {
                DataContext = viewModel,
            };
        }

        /// <summary>
        /// Shows the dialog to the user and returns the changes requested.
        /// </summary>
        /// <param name="instance">The instance on which open/close ports.</param>
        public static PortChanges PromptUser(Instance instance)
        {
            var window = new PortManagerWindow(instance);
            window.Activated += OnWindowActiviated;
            window.ShowModal();
            window.Activated -= OnWindowActiviated;
            return window.ViewModel.Result;
        }

        private static void OnWindowActiviated(object sender, EventArgs e) => WindowActivated?.Invoke(sender, e);
    }
}
