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

using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Utils;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class wraps the gcloud command and offers up some of its services in a
    /// as async methods. It also manages the its own notion of "current user" and 
    /// "current project".
    /// This class is a singleton.
    /// </summary>
    public static class GCloudWrapper
    {
        // Environment variables to specify the credentials to the Gcloud CLI.
        private const string CloudSdkCoreAccountVar = "CLOUDSDK_CORE_ACCOUNT";
        private const string CloudSdkCoreProjectVar = "CLOUDSDK_CORE_PROJECT";

        /// <summary>
        /// Calculates what the current project and account is, it might return what "gcloud"
        /// things is the current account and project or what this instance override is. This is
        /// hidden from the caller.
        /// </summary>
        /// <returns>The current AccountAndProjectId.</returns>
        public static async Task<Context> GetCurrentContextAsync()
        {
            // Fetching the current account and project, for gcloud, does not need to use the current
            // account.
            var settings = await GetJsonOutputAsync<Settings>("config list");
            return new Context(
                account: settings.CoreSettings.Account,
                projectId: settings.CoreSettings.Project);
        }

        /// <summary>
        /// Finds the location of gcloud.cmd by following all of the directories in the PATH environment
        /// variable until it finds it. With this we assume that in order to run the extension gcloud.cmd is
        /// in the PATH.
        /// </summary>
        /// <returns></returns>
        private static string GetGCloudPath()
        {
            return Environment.GetEnvironmentVariable("PATH")
                .Split(';')
                .Select(x => Path.Combine(x, "gcloud.cmd"))
                .Where(x => File.Exists(x))
                .FirstOrDefault();
        }

        private static IDictionary<string, string> GetEnvironmentForCredentials(string account, string projectId)
        {
            if (account == null && projectId == null)
            {
                return null;
            }

            var result = new Dictionary<string, string>();
            if (account != null)
            {
                result[CloudSdkCoreAccountVar] = account;
            }
            if (projectId != null)
            {
                result[CloudSdkCoreProjectVar] = projectId;
            }
            return result;
        }

        private static string FormatCommand(string command, bool useJson)
        {
            var jsonFormatParam = useJson ? "--format=json" : "";
            return $"/c gcloud {jsonFormatParam} {command}";
        }

        /// <summary>
        /// Runs a gcloud command, returns when the command is finished.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="callback">The output callback called for each line of output from the command.</param>
        /// <param name="credentials">The credentials to use.</param>
        /// <returns></returns>
        public static async Task RunCommandAsync(string command, Action<string> callback, string account = null, string projectId = null)
        {
            var actualCommand = FormatCommand(command, useJson: false);
            var envVars = GetEnvironmentForCredentials(account: account, projectId: projectId);
            Debug.WriteLine($"Executing gcloud command: {actualCommand}");
            var result = await ProcessUtils.RunCommandAsync("cmd.exe", actualCommand, (s, e) => callback(e.Line), envVars);
            if (!result)
            {
                throw new GCloudException($"Failed to execute: {actualCommand}");
            }
        }

        /// <summary>
        /// Returns the access token for this class' notion of current user.
        /// </summary>
        /// <returns>The string representation of the access token.</returns>
        public static Task<string> GetAccessTokenAsync(string account)
        {
            return GetCommandOutputAsync("auth print-access-token", account: account);
        }

        /// <summary>
        /// Returns the accounts registered with gcloud.
        /// </summary>
        /// <returns>The accounts.</returns>
        public static async Task<IEnumerable<string>> GetAccountsAsync()
        {
            // Getting the list of accounts needs to not filter down by the current account
            // being used or nothing will be shown, so we don't need to use the current account.
            var settings = await GetJsonOutputAsync<AccountSettings>("auth list");
            return settings.Accounts;
        }

        public static async Task<WindowsInstanceCredentials> ResetWindowsCredentials(string instance, string zone, string user, string account, string projectId)
        {
            return await GetJsonOutputAsync<WindowsInstanceCredentials>(
                $"beta compute reset-windows-password {instance} --quiet --zone {zone} --user {user}",
                account: account,
                projectId: projectId);
        }

        /// <summary>
        /// Fetches the value of the property given its full name.
        /// </summary>
        /// <param name="group">The group that contains the property.</param>
        /// <param name="property">The name of the property to fetch.</param>
        /// <returns>The task with the result from reading the property.</returns>
        public static async Task<string> GetPropertyAsync(string group, string property)
        {
            try
            {
                Debug.WriteLine($"Reading property gcloud {group}/{property}");
                var config = await GetJsonOutputAsync<JObject>($"config list {group}/{property}");
                var groupObject = config?[group];
                return (string)groupObject?[property];
            }
            catch (GCloudException)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the list of components that gcloud knows about.
        /// </summary>
        /// <returns></returns>
        public static async Task<IList<string>> GetInstalledComponentsAsync()
        {
            Debug.WriteLine("Reading list of components.");
            var components = await GetJsonOutputAsync<IList<CloudSdkComponent>>("components list");
            return components.Where(x => x.State.IsInstalled).Select(x => x.Id).ToList();
        }

        /// <summary>
        /// Detects if gcloud is present in the system.
        /// </summary>
        /// <returns></returns>
        public static bool IsGCloudCliInstalled()
        {
            Debug.WriteLine("Validating GCloud installation.");
            var gcloudPath = GetGCloudPath();
            Debug.WriteLineIf(gcloudPath == null, "Cannot find gcloud.cmd in the system.");
            Debug.WriteLineIf(gcloudPath != null, $"Found gcloud.cmd at {gcloudPath}");
            return gcloudPath != null;
        }

        private static async Task<string> GetCommandOutputAsync(string command, string account = null, string projectId = null)
        {
            var actualCommand = FormatCommand(command, useJson: false);
            var envVars = GetEnvironmentForCredentials(account: account, projectId: projectId);
            Debug.WriteLine($"Executing gcloud command: {actualCommand}");
            var output = await ProcessUtils.GetCommandOutputAsync("cmd.exe", actualCommand, envVars);
            if (!output.Succeeded)
            {
                throw new GCloudException($"Failed with message: {output.StandardError}");
            }
            return output.StandardOutput;
        }

        private static async Task<T> GetJsonOutputAsync<T>(string command, string account = null, string projectId = null)
        {
            var actualCommand = FormatCommand(command, useJson: true);
            var envVars = GetEnvironmentForCredentials(account: account, projectId: projectId);
            try
            {
                Debug.Write($"Executing gcloud command: {actualCommand}");
                return await ProcessUtils.GetJsonOutputAsync<T>("cmd.exe", actualCommand, envVars);
            }
            catch (JsonOutputException ex)
            {
                throw new GCloudException($"Failed to execute command {actualCommand}\nInner message:\n{ex.Message}", ex);
            }
        }
    }
}
