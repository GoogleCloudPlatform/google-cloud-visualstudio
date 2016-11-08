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

namespace GoogleCloudExtension.AddWindowsCredential
{
    /// <summary>
    /// This class represents the AddWindowsCredential window.
    /// </summary>
    public class AddWindowsCredentialWindow : CommonDialogWindowBase
    {
        public AddWindowsCredentialViewModel ViewModel { get; }

        private AddWindowsCredentialWindow(Instance instance)
            : base(GoogleCloudExtension.Resources.AddWindowsCredentialTitle)
        {
            ViewModel = new AddWindowsCredentialViewModel(this, instance);
            Content = new AddWindowsCredentialWindowContent
            {
                DataContext = ViewModel
            };
        }

        /// <summary>
        /// Prompt the user for the Windows credentials to use, returns the result of the operation.
        /// </summary>
        /// <param name="instance">The instance for which the credentials are added.</param>
        /// <returns>An instance of <seealso cref="AddWindowsCredentialResult"/> if the user accepeted, null if canceled.</returns>
        public static AddWindowsCredentialResult PromptUser(Instance instance)
        {
            var dialog = new AddWindowsCredentialWindow(instance);
            dialog.ShowModal();
            return dialog.ViewModel.Result;
        }
    }
}
