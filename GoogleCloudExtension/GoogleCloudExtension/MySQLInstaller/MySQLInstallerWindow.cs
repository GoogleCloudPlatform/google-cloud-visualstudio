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

namespace GoogleCloudExtension.MySQLInstaller
{
    /// <summary>
    /// This class is the dialog to use to prompt the user to install MySQL for Visual Studio and other
    /// needed extensions.
    /// </summary>
    internal class MySQLInstallerWindow : CommonDialogWindowBase
    {
        private MySQLInstallerWindow() : base(GoogleCloudExtension.Resources.MySqlInstallerWindowTitle)
        {
            Content = new MySQLInstallerWindowContent
            {
                DataContext = new MySQLInstallerViewModel(this)
            };
        }

        /// <summary>
        /// Shows the dialog to the user.
        /// </summary>
        public static void PromptUser()
        {
            var dialog = new MySQLInstallerWindow();
            dialog.ShowModal();
        }
    }
}
