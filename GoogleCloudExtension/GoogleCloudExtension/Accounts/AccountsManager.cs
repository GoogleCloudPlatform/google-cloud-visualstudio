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
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.OAuth;
using GoogleCloudExtension.OauthLoginFlow;
using GoogleCloudExtension.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Accounts
{
    /// <summary>
    /// This class manages the OAUTH credentials created for the extension.
    /// </summary>
    public static class AccountsManager
    {
        private const string OAuthEventCategory = "OAUTH";

        /// <summary>
        /// The OAUTH credentials to use for the VS extension.
        /// </summary>
        private static readonly OAuthCredentials s_extensionCredentials =
            new OAuthCredentials(
                clientId: "1072225748908-vilq5kul2gkfu75ibst47grttjv8k5k6.apps.googleusercontent.com",
                clientSecret: "LmgDUh7hKoiEu0ZR8OOHmBOQ");

        /// <summary>
        /// The scopes that the VS extension needs.
        /// </summary>
        private static readonly IEnumerable<string> s_extensionScopes =
            new List<string>
            {
                "https://www.googleapis.com/auth/userinfo.email",
                "https://www.googleapis.com/auth/cloud-platform",
                "https://www.googleapis.com/auth/appengine.admin",
                "https://www.googleapis.com/auth/compute",
                "https://www.googleapis.com/auth/plus.me",
            };

        /// <summary>
        /// Starts the flow to add a new account to the credentials store.
        /// </summary>
        /// <returns>Will return true if the accound was added, false if the user cancelled.</returns>
        public static async Task<bool> StartAddAccountFlowAsync()
        {
            try
            {
                ExtensionAnalytics.ReportEvent(OAuthEventCategory, "FlowStarted");
                string refreshToken = OAuthLoginFlowWindow.PromptUser(s_extensionCredentials, s_extensionScopes);
                if (refreshToken == null)
                {
                    ExtensionAnalytics.ReportEvent(OAuthEventCategory, "FlowCancelled");
                    Debug.WriteLine("The user cancelled the OAUTH login flow.");
                    return false;
                }

                var credentials = await GetUserAccountForRefreshToken(refreshToken);
                ExtensionAnalytics.ReportEvent(OAuthEventCategory, "FlowFinished");

                var existingUserAccount = CredentialsStore.Default.GetAccount(credentials.AccountName);
                if (existingUserAccount != null)
                {
                    Debug.WriteLine($"Duplicated account {credentials.AccountName}");
                    UserPromptUtils.ErrorPrompt($"The user account {credentials.AccountName} already exists.", "Duplicate Account");
                    return false;
                }

                // Store the new account and set it as the current account. The project is not changed so if the
                // new account also have access to it, it remains as the current project.
                CredentialsStore.Default.AddAccount(credentials);
                CredentialsStore.Default.CurrentAccount = credentials;
                return true;
            }
            catch (OAuthException ex)
            {
                ExtensionAnalytics.ReportEvent(OAuthEventCategory, "FlowFailed");
                UserPromptUtils.ErrorPrompt($"Failed to perform OAUTH authentication. {ex.Message}", "OAUTH error");
                return false;
            }
        }

        /// <summary>
        /// Deletes the given <paramref name="userAccount"/> from the store.
        /// </summary>
        /// <param name="userAccount"></param>
        public static void DeleteAccount(UserAccount userAccount) => CredentialsStore.Default.DeleteAccount(userAccount);

        private static async Task<UserAccount> GetUserAccountForRefreshToken(string refreshToken)
        {
            var result = new UserAccount
            {
                RefreshToken = refreshToken,
                ClientId = s_extensionCredentials.ClientId,
                ClientSecret = s_extensionCredentials.ClientSecret
            };
            var plusDataSource = new GPlusDataSource(result.GetGoogleCredential());
            var person = await plusDataSource.GetProfileAsync();
            result.AccountName = person.Emails.FirstOrDefault()?.Value;
            return result;
        }
    }
}
