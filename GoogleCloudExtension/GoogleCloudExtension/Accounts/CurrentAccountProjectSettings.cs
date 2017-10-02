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

using GoogleCloudExtension.SolutionUtils;
using System;
using System.Diagnostics;

namespace GoogleCloudExtension.Accounts
{
    /// <summary>
    /// This class get/set current account, project from/to solution .suo file.
    /// </summary>
    public class CurrentAccountProjectSettings
    {
        private const string CurrentGcpProjectKey = "google_current_gcp_project";
        private const string CurrentGcpAccountKey = "google_current_gcp_credentials";
        private static readonly Lazy<CurrentAccountProjectSettings> s_instance = new Lazy<CurrentAccountProjectSettings>();

        private string _tmpProject;
        private string _tmpAccount;
        private ushort _commitFlag = 0;

        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static CurrentAccountProjectSettings Current => s_instance.Value;

        /// <summary>
        /// Gets/sets current GCP project id
        /// </summary>
        [SolutionSettingKey(CurrentGcpProjectKey)]
        public string CurrentProject
        {
            get { return CredentialsStore.Default.CurrentProjectId; }
            set
            {
                _tmpProject = value;
                _commitFlag |= 0x01;
                CheckIfCommit();
            }
        }

        /// <summary>
        /// Gets/Sets current GCP account name
        /// </summary>
        [SolutionSettingKey(CurrentGcpAccountKey)]
        public string CurrentAccount
        {
            get { return CredentialsStore.Default.CurrentAccount?.AccountName; }
            set
            {
                _tmpAccount = value;
                _commitFlag |= 0x02;
                CheckIfCommit();
            }
        }

        /// <summary>
        /// We need to make final account, project changes in one batch.
        /// </summary>
        private void CheckIfCommit()
        {
            if (_commitFlag == 0x03)
            {
                Debug.WriteLine("Setting the user and project.");
                CredentialsStore.Default.ResetCredentials(
                    accountName: _tmpAccount,
                    projectId: _tmpProject);
                _commitFlag = 0;
            }
        }
    }
}
