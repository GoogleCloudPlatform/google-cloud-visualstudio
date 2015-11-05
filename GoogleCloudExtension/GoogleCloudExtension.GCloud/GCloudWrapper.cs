// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        private AccountAndProjectId _currentAccountAndProject;

        private GCloudWrapper()
        { }

        /// <summary>
        /// Lazily creates the singleton instace for the class.
        /// </summary>
        private static GCloudWrapper s_Instance = new GCloudWrapper();
        public static GCloudWrapper Instance
        {
            get { return s_Instance; }
        }

        /// <summary>
        /// This event is raised whenever the current account or project has changed;
        /// there's no notification of what the new values are so the caller has to call
        /// to find out what the new values are.
        /// </summary>
        public event EventHandler AccountOrProjectChanged;

        private void RaiseAccountOrProjectChanged()
        {
            AccountOrProjectChanged?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Calculates what the current project and account is, it might return what "gcloud"
        /// things is the current account and project or what this instance override is. This is
        /// hidden from the caller.
        /// </summary>
        /// <returns>The current AccountAndProjectId.</returns>
        public async Task<AccountAndProjectId> GetCurrentAccountAndProjectAsync()
        {
            if (_currentAccountAndProject == null)
            {
                // Fetching the current account and project, for gcloud, does not need to use the current
                // account.
                var settings = await GetJsonOutputAsync<Settings>("config list", accountAndProject: null);
                _currentAccountAndProject = new AccountAndProjectId(
                    account: settings.CoreSettings.Account,
                    projectId: settings.CoreSettings.Project);
            }
            return _currentAccountAndProject;
        }

        /// <summary>
        /// Updates the local concet of "current account and project" in the instance *only* it does not
        /// affect what gcloud thinks is the current account and project.
        /// </summary>
        /// <param name="accountAndProject">The new accountAndProject to use</param>
        public void UpdateUserAndProject(AccountAndProjectId accountAndProject)
        {
            _currentAccountAndProject = accountAndProject;
            RaiseAccountOrProjectChanged();
        }

        public void UpdateProject(string projectId)
        {
            var newAccountAndProject = new AccountAndProjectId(_currentAccountAndProject.Account, projectId);
            UpdateUserAndProject(newAccountAndProject);
        }

        private static Dictionary<string, string> s_GCloudEnvironment;

        private static Dictionary<string, string> GetGCloudEnvironment()
        {
            if (s_GCloudEnvironment == null)
            {
                s_GCloudEnvironment = new Dictionary<string, string>();
                var gcloudPath = GetGCloudPath();
                var newPath = Environment.ExpandEnvironmentVariables($"{gcloudPath};%PATH%");
                s_GCloudEnvironment["PATH"] = newPath;
            }
            return s_GCloudEnvironment;
        }

        // TODO(ivann): Possibly use MSI APIs to find the location where gcloud was installed instead
        // of hardcoding it to program files.
        // If the user installs it by hand then we need to have a registry key, or environment variable, where
        // the user can override this location.
        private static string GetGCloudPath()
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            // This is where the binaries are supposed to be stored.
            string result = Path.Combine(programFiles, @"Google\Cloud SDK\google-cloud-sdk\bin");
            if (Directory.Exists(result))
            {
                return result;
            }

            // There's a bug in gcloud setup and it might be stored in this directory.
            return Path.Combine(programFiles, @"Google\Cloud SDK\GOOGLE~1\bin");
        }

        private string FormatAccountAndProjectParameters(AccountAndProjectId accountAndProject)
        {
            if (accountAndProject == null)
            {
                return "";
            }
            var projectParam = accountAndProject?.ProjectId == null ? "" : $"--project={accountAndProject.ProjectId}";
            var accountParam = accountAndProject?.Account == null ? "" : $"--account={accountAndProject.Account}";
            return $"{projectParam} {accountParam}";
        }

        private string FormatCommand(string command, AccountAndProjectId accountAndProject, bool useJson)
        {
            var accountAndProjectParams = FormatAccountAndProjectParameters(accountAndProject);
            var jsonFormatParam = useJson ? "--format=json" : "";
            return $"/c gcloud {jsonFormatParam} {accountAndProjectParams} {command}";
        }

        public async Task RunCommandAsync(string command, Action<string> callback, AccountAndProjectId accountAndProject)
        {
            var actualCommand = FormatCommand(command, accountAndProject, useJson: false);
            Debug.WriteLine($"Executing gcloud command: {actualCommand}");
            var result = await ProcessUtils.RunCommandAsync("cmd.exe", actualCommand, (s, e) => callback(e.Line), GetGCloudEnvironment());
            if (!result)
            {
                throw new GCloudException($"Failed to execute: {actualCommand}");
            }
        }

        public async Task RunCommandAsync(string command, Action<string> callback)
        {
            await RunCommandAsync(command, callback, await GetCurrentAccountAndProjectAsync());
        }

        public async Task<string> GetCommandOutputAsync(string command, AccountAndProjectId accountAndProject)
        {
            var actualCommand = FormatCommand(command, accountAndProject, useJson: false);
            Debug.WriteLine($"Executing gcloud command: {actualCommand}");
            var output = await ProcessUtils.GetCommandOutputAsync("cmd.exe", actualCommand, GetGCloudEnvironment());
            if (!output.Succeeded)
            {
                throw new GCloudException($"Failed with message: {output.Error}");
            }
            return output.Output;
        }

        public async Task<string> GetCommandOutputAsync(string command)
        {
            return await GetCommandOutputAsync(command, await GetCurrentAccountAndProjectAsync());
        }

        public async Task<T> GetJsonOutputAsync<T>(string command, AccountAndProjectId accountAndProject)
        {
            var actualCommand = FormatCommand(command, accountAndProject, useJson: true);
            try
            {
                Debug.Write($"Executing gcloud command: {actualCommand}");
                return await ProcessUtils.GetJsonOutputAsync<T>("cmd.exe", actualCommand, GetGCloudEnvironment());
            }
            catch (JsonOutputException ex)
            {
                throw new GCloudException($"Failed to execute command {actualCommand}\nInner message:\n{ex.Message}", ex);
            }
        }

        public async Task<T> GetJsonOutputAsync<T>(string command)
        {
            return await GetJsonOutputAsync<T>(command, await GetCurrentAccountAndProjectAsync());
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
        /// Returns the list of compute instances for this class' notion of current
        /// user and project.
        /// </summary>
        /// <returns>The list of compute instances.</returns>
        public Task<IList<ComputeInstance>> GetComputeInstanceListAsync()
        {
            return GetJsonOutputAsync<IList<ComputeInstance>>("compute instances list");
        }

        /// <summary>
        /// Returns the list of accounts registered with gcloud.
        /// </summary>
        /// <returns>List of accounts.</returns>
        public async Task<IList<string>> GetAccountListAsync()
        {
            // Getting the list of accounts needs to not filter down by the current account
            // being used or nothing will be shown, so we don't need to use the current account.
            var settings = await GetJsonOutputAsync<AccountSettings>("auth list", accountAndProject: null);
            return settings.Accounts;
        }

        /// <summary>
        /// Returns the list of projects accessible by the current account.
        /// </summary>
        /// <returns>List of projects.</returns>
        public Task<IList<GcpProject>> GetProjectsAsync()
        {
            return GetJsonOutputAsync<IList<GcpProject>>("alpha projects list");
        }

        public async Task<IList<GcpProject>> GetProjectsAsync(AccountAndProjectId accountAndProject)
        {
            return await GetJsonOutputAsync<IList<GcpProject>>("alpha projects list", accountAndProject);
        }

        public bool ValidateGCloudInstallation()
        {
            Debug.WriteLine("Validating GCloud installation.");
            var gcloudDirectory = GetGCloudPath();
            var gcloudPath = Path.Combine(gcloudDirectory, "gcloud.cmd");

            var result = File.Exists(gcloudPath);
            Debug.WriteLineIf(!result, $"GCloud cannot be found, can't find {gcloudPath}");
            return result;
        }
    }
}
