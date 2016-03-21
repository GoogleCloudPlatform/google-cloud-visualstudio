// Copyright 2016 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using GoogleCloudExtension.GCloud.Models;
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
    public sealed class GCloudWrapper
    {
        // Environment variables to specify the credentials to the Gcloud CLI.
        private const string CloudSdkCoreAccountVar = "CLOUDSDK_CORE_ACCOUNT";
        private const string CloudSdkCoreProjectVar = "CLOUDSDK_CORE_PROJECT";

        /// <summary>
        /// Maintains the currently selected account and project for the instance.
        /// </summary>
        private Credentials _currentCredentials;

        /// <summary>
        /// Singleton for the class.
        /// </summary>
        public static GCloudWrapper Instance { get; } = new GCloudWrapper();

        /// <summary>
        /// This event is raised whenever the current account or project has changed;
        /// there's no notification of what the new values are so the caller has to call
        /// to find out what the new values are.
        /// </summary>
        public event EventHandler AccountOrProjectChanged;

        /// <summary>
        /// Private to enforce the singleton.
        /// </summary>
        private GCloudWrapper()
        { }

        private void RaiseAccountOrProjectChanged()
        {
            AccountOrProjectChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Calculates what the current project and account is, it might return what "gcloud"
        /// things is the current account and project or what this instance override is. This is
        /// hidden from the caller.
        /// </summary>
        /// <returns>The current AccountAndProjectId.</returns>
        public async Task<Credentials> GetCurrentCredentialsAsync()
        {
            if (_currentCredentials == null)
            {
                // Fetching the current account and project, for gcloud, does not need to use the current
                // account.
                var settings = await GetJsonOutputAsync<Settings>("config list", credentials: null);
                _currentCredentials = new Credentials(
                    account: settings.CoreSettings.Account,
                    projectId: settings.CoreSettings.Project);
            }
            return _currentCredentials;
        }

        /// <summary>
        /// Updates the local concet of "current account and project" in the instance *only* it does not
        /// affect what gcloud thinks is the current account and project.
        /// </summary>
        /// <param name="credentials">The new accountAndProject to use</param>
        public void UpdateCredentials(Credentials credentials)
        {
            _currentCredentials = credentials;
            RaiseAccountOrProjectChanged();
        }

        public void UpdateProject(string projectId)
        {
            var newCredentials = new Credentials(_currentCredentials.Account, projectId);
            UpdateCredentials(newCredentials);
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

        private IDictionary<string, string> GetEnvironmentForCredentials(Credentials credentials)
        {
            if (credentials != null)
            {
                return new Dictionary<string, string>
                {
                    { CloudSdkCoreAccountVar, credentials.Account },
                    { CloudSdkCoreProjectVar, credentials.ProjectId }
                };
            }
            return null;
        }

        private string FormatCommand(string command, bool useJson)
        {
            var jsonFormatParam = useJson ? "--format=json" : "";
            return $"/c gcloud {jsonFormatParam} {command}";
        }

        public async Task RunCommandAsync(string command, Action<string> callback, Credentials credentials)
        {
            var actualCommand = FormatCommand(command, useJson: false);
            var envVars = GetEnvironmentForCredentials(credentials);
            Debug.WriteLine($"Executing gcloud command: {actualCommand}");
            var result = await ProcessUtils.RunCommandAsync("cmd.exe", actualCommand, (s, e) => callback(e.Line), envVars);
            if (!result)
            {
                throw new GCloudException($"Failed to execute: {actualCommand}");
            }
        }

        public async Task RunCommandAsync(string command, Action<string> callback)
        {
            await RunCommandAsync(command, callback, await GetCurrentCredentialsAsync());
        }

        public async Task<string> GetCommandOutputAsync(string command, Credentials credentials)
        {
            var actualCommand = FormatCommand(command, useJson: false);
            var envVars = GetEnvironmentForCredentials(credentials);
            Debug.WriteLine($"Executing gcloud command: {actualCommand}");
            var output = await ProcessUtils.GetCommandOutputAsync("cmd.exe", actualCommand, envVars);
            if (!output.Succeeded)
            {
                throw new GCloudException($"Failed with message: {output.Error}");
            }
            return output.Output;
        }

        public async Task<string> GetCommandOutputAsync(string command)
        {
            return await GetCommandOutputAsync(command, await GetCurrentCredentialsAsync());
        }

        public Task<int> LaunchCommandAsync(string command)
        {
            var actualCommand = FormatCommand(command, useJson: false);
            return ProcessUtils.LaunchCommandAsync("cmd.exe", actualCommand, null);
        }

        public async Task<T> GetJsonOutputAsync<T>(string command, Credentials credentials)
        {
            var actualCommand = FormatCommand(command, useJson: true);
            var envVars = GetEnvironmentForCredentials(credentials);
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

        public async Task<T> GetJsonOutputAsync<T>(string command)
        {
            return await GetJsonOutputAsync<T>(command, await GetCurrentCredentialsAsync());
        }

        /// <summary>
        /// Returns the access token for this class' notion of current user.
        /// </summary>
        /// <returns>The string representation of the access token.</returns>
        public Task<string> GetAccessTokenAsync()
        {
            return GetCommandOutputAsync("auth print-access-token");
        }

        /// <summary>
        /// Invokes the _default_ browser in the system to add a new set of credentials into
        /// the credentials store. This will also invalidate the list of credentials to notify the various
        /// parts in the extension that depends on the list of current credentials
        /// </summary>
        /// <returns></returns>
        public async Task AddCredentialsAsync(Action<string> callback)
        {
            callback("Launching browser.");
            var result = await LaunchCommandAsync("auth login --launch-browser");
            if (result == 0)
            {
                // A new account was succesfully registered, invalidate the current credentials
                // so the interface is updated.
                _currentCredentials = null;
                RaiseAccountOrProjectChanged();
                var credentials = await GetCurrentCredentialsAsync();
                callback($"Succesfully added the account: {credentials.Account}");
            }
            else
            {
                callback($"Failed to add the account with result: {result}");
            }
        }

        /// <summary>
        /// Returns the accounts registered with gcloud.
        /// </summary>
        /// <returns>The accounts.</returns>
        public async Task<IEnumerable<string>> GetAccountsAsync()
        {
            // Getting the list of accounts needs to not filter down by the current account
            // being used or nothing will be shown, so we don't need to use the current account.
            var settings = await GetJsonOutputAsync<AccountSettings>("auth list", credentials: null);
            return settings.Accounts;
        }

        /// <summary>
        /// Returns the list of projects accessible by the current account.
        /// </summary>
        /// <returns>List of projects.</returns>
        public Task<IList<CloudProject>> GetProjectsAsync()
        {
            return GetJsonOutputAsync<IList<CloudProject>>("alpha projects list");
        }

        /// <summary>
        /// Fetches the list of projects for the given credentials from the
        /// gcloud store.
        /// </summary>
        /// <param name="credentials">The credentials to use.</param>
        /// <returns>The task with the list of projects.</returns>
        public async Task<IList<CloudProject>> GetProjectsAsync(Credentials credentials)
        {
            return await GetJsonOutputAsync<IList<CloudProject>>("alpha projects list", credentials);
        }

        public async Task<WindowsInstanceCredentials> ResetWindowsCredentials(string instance, string zone, string user)
        {
            return await GetJsonOutputAsync<WindowsInstanceCredentials>($"beta compute reset-windows-password {instance} --quiet --zone {zone} --user {user}");
        }

        /// <summary>
        /// Fetches the value of the property given its full name.
        /// </summary>
        /// <param name="group">The group that contains the property.</param>
        /// <param name="property">The name of the property to fetch.</param>
        /// <returns>The task with the result from reading the property.</returns>
        public async Task<string> GetPropertyAsync(string group, string property)
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
        public async Task<IList<CloudSdkComponent>> GetComponentsAsync()
        {
            Debug.WriteLine("Reading list of components.");
            return await GetJsonOutputAsync<IList<CloudSdkComponent>>("components list");
        }

        /// <summary>
        /// Detects if gcloud is present in the system.
        /// </summary>
        /// <returns></returns>
        public bool IsGCloudCliInstalled()
        {
            Debug.WriteLine("Validating GCloud installation.");
            var gcloudPath = GetGCloudPath();
            Debug.WriteLineIf(gcloudPath == null, "Cannot find gcloud.cmd in the system.");
            Debug.WriteLineIf(gcloudPath != null, $"Found gcloud.cmd at {gcloudPath}");
            return gcloudPath != null;
        }
    }
}
