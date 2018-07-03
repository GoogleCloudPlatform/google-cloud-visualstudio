﻿// Copyright 2016 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class holds the credentials used to perform GCloud operations.
    /// </summary>
    public class GCloudContext : IGCloudContext
    {
        private const string GCloudMetricsVariable = "CLOUDSDK_METRICS_ENVIRONMENT";
        private const string GCloudMetricsVersionVariable = "CLOUDSDK_METRICS_ENVIRONMENT_VERSION";

        /// <summary>
        /// The path to the credentials .json file to use for the call. The .json file should be a
        /// format accetable by gcloud's --credential-file-override parameter. Typically an authorize_user kind.
        /// </summary>
        public string CredentialsPath { get; }

        /// <summary>
        /// The project id of the project to use for the invokation of gcloud.
        /// </summary>
        public string ProjectId { get; }

        protected readonly Dictionary<string, string> Environment = new Dictionary<string, string>
        {
            [GCloudMetricsVariable] = GoogleCloudExtensionPackage.Instance.ApplicationName,
            [GCloudMetricsVersionVariable] =
                GoogleCloudExtensionPackage.Instance.ApplicationVersion
        };

        /// <summary>
        /// Creates the default GCloud context from the current environment.
        /// </summary>
        public GCloudContext()
        {
            CredentialsPath = CredentialsStore.Default.CurrentAccountPath;
            ProjectId = CredentialsStore.Default.CurrentProjectId;
        }

        /// <summary>
        /// Resets, or creates, the password for the given <paramref name="userName"/> in the given instance.
        /// </summary>
        public Task<WindowsInstanceCredentials> ResetWindowsCredentialsAsync(
            string instanceName,
            string zoneName,
            string userName)
        {
            string command =
                $"compute reset-windows-password {instanceName} --zone={zoneName} --user=\"{userName}\" --quiet ";
            return GetGcloudOutputAsync<WindowsInstanceCredentials>(command);
        }

        /// <summary>
        /// Deploys an app to App Engine.
        /// </summary>
        /// <param name="appYaml">The path to the app.yaml file to deploy.</param>
        /// <param name="version">The version to use, if no version is used gcloud will decide the version name.</param>
        /// <param name="promote">Whether to promote the app or not.</param>
        /// <param name="outputAction">The action to call with output from the command.</param>
        public Task<bool> DeployAppAsync(string appYaml, string version, bool promote, Action<string> outputAction)
        {
            string versionParameter = version != null ? $"--version={version}" : "";
            string promoteParameter = promote ? "--promote" : "--no-promote";

            return RunGcloudCommandAsync(
                $"app deploy \"{appYaml}\" {versionParameter} {promoteParameter} --skip-staging --quiet",
                outputAction);
        }

        /// <summary>
        /// Builds a container using the Container Builder service.
        /// </summary>
        /// <param name="imageTag">The name of the image to build.</param>
        /// <param name="contentsPath">The contents of the container, including the Dockerfile.</param>
        /// <param name="outputAction">The action to perform on each line of output.</param>
        public Task<bool> BuildContainerAsync(string imageTag, string contentsPath, Action<string> outputAction)
        {
            string command = $"container builds submit --tag=\"{imageTag}\" \"{contentsPath}\"";
            return RunGcloudCommandAsync(command, outputAction);
        }

        /// <summary>
        /// Runs the given gcloud command.
        /// </summary>
        /// <param name="command">The subcommand and arguments to run.</param>
        /// <param name="outputAction">The action for outputting lines.</param>
        /// <returns>True if the command succeeds, false otherwise.</returns>
        protected Task<bool> RunGcloudCommandAsync(
            string command,
            Action<string> outputAction = null)
        {
            string actualCommand = FormatGcloudCommand(command);

            // This code depends on the fact that gcloud.cmd is a batch file.
            Debug.Write($"Executing gcloud command: {actualCommand}");
            return ProcessUtils.Default.RunCommandAsync(
                file: "cmd.exe",
                args: $"/c {actualCommand}",
                handler: (o, e) => outputAction?.Invoke(e.Line),
                environment: Environment);
        }

        private async Task<T> GetGcloudOutputAsync<T>(string command)
        {
            string actualCommand = FormatGcloudOutputCommand(command);
            try
            {
                // This code depends on the fact that gcloud.cmd is a batch file.
                Debug.Write($"Executing gcloud command: {actualCommand}");
                return await ProcessUtils.Default.GetJsonOutputAsync<T>(
                    file: "cmd.exe",
                    args: $"/c {actualCommand}",
                    environment: Environment);
            }
            catch (JsonOutputException ex)
            {
                throw new GCloudException(
                    $"Failed to execute command {actualCommand}\nInner message:\n{ex.Message}",
                    ex);
            }
        }

        private string FormatGcloudCommand(string command)
        {
            string projectId = $"--project={ProjectId}";
            string credentialsPath = $"--credential-file-override=\"{CredentialsPath}\"";

            return $"gcloud {command} {projectId} {credentialsPath}";
        }

        private string FormatGcloudOutputCommand(string command)
        {
            string formattedCommand = FormatGcloudCommand(command);
            return $"{formattedCommand} --format=json";
        }
    }
}
