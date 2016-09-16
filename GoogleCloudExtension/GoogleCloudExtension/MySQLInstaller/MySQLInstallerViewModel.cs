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

using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Utils;
using System.Diagnostics;
using System.Windows.Input;

namespace GoogleCloudExtension.MySQLInstaller
{
    /// <summary>
    /// This class is the view model for the <seealso cref="MySQLInstallerWindow"/> dialog.
    /// </summary>
    internal class MySQLInstallerViewModel : ViewModelBase
    {
        private readonly MySQLInstallerWindow _owner;

        /// <summary>
        /// The command to execute when the Download button is pressed.
        /// </summary>
        public ICommand DownloadCommand { get; }

        public MySQLInstallerViewModel(MySQLInstallerWindow owner)
        {
            _owner = owner;

            DownloadCommand = new WeakCommand(OpenDownload);
        }

        private void OpenDownload()
        {
            var url = $"https://dev.mysql.com/downloads/installer/";
            Debug.WriteLine($"Opening page to download MySQL Installer: {url}");
            Process.Start(url);
            _owner.Close();
        }
    }
}
