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
using GoogleCloudExtension.DataSources;
using Microsoft.VisualStudio.PlatformUI;

namespace GoogleCloudExtension.FirewallManagement
{
    /// <summary>
    /// This class is the dialog to use to prompt the user for firewall changes for the given instance.
    /// </summary>
    public class PortManagerWindow : DialogWindow
    {
        private PortManagerViewModel ViewModel => (PortManagerViewModel)((PortManagerWindowContent)Content).DataContext;

        private PortManagerWindow(Instance instance)
        {
            Title = "Manage Open Ports";
            Width = 320;
            Height = 300;
            ResizeMode = System.Windows.ResizeMode.NoResize;
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

            var viewModel = new PortManagerViewModel(this, instance);
            Content = new PortManagerWindowContent
            {
                DataContext = viewModel,
            };
        }

        /// <summary>
        /// Shows the dialog to the user and returns the changes requested.
        /// </summary>
        /// <param name="instance">The instance on which open/close ports.</param>
        /// <returns></returns>
        public static PortChanges PromptUser(Instance instance)
        {
            var window = new PortManagerWindow(instance);
            window.ShowModal();
            return window.ViewModel.Result;
        }
    }
}
