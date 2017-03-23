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

using Google.Apis.SQLAdmin.v1beta4.Data;
using GoogleCloudExtension.Theming;

namespace GoogleCloudExtension.AuthorizedNetworkManagement
{
    /// <summary>
    /// This class is the dialog to manage authorized networks for Cloud SQL instances.
    /// </summary>
    public class AuthorizedNetworksWindow : CommonDialogWindowBase
    {
        private AuthorizedNetworksViewModel ViewModel =>
            (AuthorizedNetworksViewModel)((AuthorizedNetworksWindowContent)Content).DataContext;

        private AuthorizedNetworksWindow(DatabaseInstance instance) :
            base(GoogleCloudExtension.Resources.AuthorizedNetworksWindowCaption)
        {
            Content = new AuthorizedNetworksWindowContent
            {
                DataContext = new AuthorizedNetworksViewModel(this, instance)
            };
        }

        /// <summary>
        /// Shows the dialog to the user.
        /// </summary>
        /// <param name="instance">The instance on which to managed authorized networks.</param>
        /// <returns>The authorized network changes or null if the user canceled the dialog.</returns>
        public static AuthorizedNetworkChange PromptUser(DatabaseInstance instance)
        {
            AuthorizedNetworksWindow dialog = new AuthorizedNetworksWindow(instance);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}

