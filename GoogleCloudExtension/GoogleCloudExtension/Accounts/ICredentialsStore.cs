// Copyright 2018 Google Inc. All Rights Reserved.
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

using System;
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;
using Google.Apis.CloudResourceManager.v1.Data;

namespace GoogleCloudExtension.Accounts
{
    public interface ICredentialsStore
    {
        /// <summary>
        /// The list of accounts known to the store.
        /// </summary>
        IEnumerable<IUserAccount> AccountsList { get; }

        /// <summary>
        /// The current <see cref="UserAccount"/> selected.
        /// </summary>
        IUserAccount CurrentAccount { get; }

        /// <summary>
        /// Returns the path for the current account.
        /// </summary>
        string CurrentAccountPath { get; }

        /// <summary>
        /// The GoogleCredential for the current <see cref="UserAccount"/>.
        /// </summary>
        GoogleCredential CurrentGoogleCredential { get; }

        /// <summary>
        /// The currently selected project ID.
        /// </summary>
        string CurrentProjectId { get; }

        /// <summary>
        /// The currently selected project numeric ID, might be null if no project is loaded.
        /// </summary>
        string CurrentProjectNumericId { get; }

        event EventHandler CurrentAccountChanged;
        event EventHandler CurrentProjectIdChanged;

        /// <summary>
        /// Stores a new set of user credentials in the credentials store.
        /// </summary>
        void AddAccount(IUserAccount userAccount);

        /// <summary>
        /// Deletes the <paramref name="account"/> from the store. The account must exist in the store
        /// or it will throw.
        /// </summary>
        /// <param name="account">The account to delete.</param>
        /// <returns>True if the current account was deleted, false otherwise.</returns>
        void DeleteAccount(IUserAccount account);

        /// <summary>
        /// Returns the account given the account name.
        /// </summary>
        /// <param name="accountName">The name to look.</param>
        /// <returns>The account if found, null otherwise.</returns>
        IUserAccount GetAccount(string accountName);

        /// <summary>
        /// Resets the credentials state to the account with the given <paramref name="accountName"/> and the
        /// given <paramref name="projectId"/>.
        /// If <paramref name="accountName"/> cannot be found in the store then the credentials will be reset
        /// to empty.
        /// </summary>
        /// <param name="accountName">The name of the account to make current.</param>
        /// <param name="projectId">The projectId to make current.</param>
        void ResetCredentials(string accountName, string projectId);

        /// <summary>
        /// Updates the current account for the extension.
        /// This method will also invalidate the project.
        /// It is up to the caller to select an appropriate one.
        /// </summary>
        void UpdateCurrentAccount(IUserAccount account);

        /// <summary>
        /// Updates the current project data from the given <paramref name="project"/>.
        /// </summary>
        void UpdateCurrentProject(Project project);
    }
}