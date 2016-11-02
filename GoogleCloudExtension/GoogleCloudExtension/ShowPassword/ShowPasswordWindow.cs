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

using GoogleCloudExtension.Theming;
using System;

namespace GoogleCloudExtension.ShowPassword
{
    /// <summary>
    /// This class represents the dialog that will show the user the Windows credentials
    /// obtained after a password reset.
    /// </summary>
    public class ShowPasswordWindow : CommonDialogWindowBase
    {
        private ShowPasswordWindow(string userName, string password, string instanceName)
            : base(String.Format(GoogleCloudExtension.Resources.ShowPasswordWindowTitle, instanceName))
        {
            Content = new ShowPasswordWindowContent(new ShowPasswordViewModel(
                    this,
                    userName: userName,
                    password: password,
                    instanceName: instanceName));
        }

        /// <summary>
        /// Shows the given credentials to the user.
        /// </summary>
        public static void PromptUser(string userName, string password, string instanceName)
        {
            var dialog = new ShowPasswordWindow(
                userName: userName,
                password: password,
                instanceName: instanceName);
            dialog.ShowModal();
        }
    }
}
