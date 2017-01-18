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
    /// as async methods. 
    /// </summary>
    public static class GCloudWrapper
    {
        // These variables specify the environment to be reported by gcloud when reporting metrics.
        private const string GCloudMetricsVariable = "CLOUDSDK_METRICS_ENVIRONMENT";
        private const string GCloudMetricsVersionVariable = "CLOUDSDK_METRICS_ENVIRONMENT_VERSION";

        // This variable contains the path to the configuration to be used for kubernetes operations.
        private const string GCloudKubeConfigVariable = "KUBECONFIG";

        /// <summary>
        /// Finds the location of gcloud.cmd by following all of the directories in the PATH environment
        /// variable until it finds it. With this we assume that in order to run the extension gcloud.cmd is
        /// in the PATH.
        /// </summary>
        public static string GetGCloudPath() =>
            Environment.GetEnvironmentVariable("PATH")
                .Split(';')
                .FirstOrDefault(x => File.Exists(Path.Combine(x, "gcloud.cmd")));

        /// <summary>
        /// Resets, or creates, the password for the given <paramref name="userName"/> in the given instance.
        /// </summary>
        /// <param name="instanceName"></param>
        /// <param name="zoneName"></param>
        /// <param name="userName"></param>
        public static Task<WindowsInstanceCredentials> ResetWindowsCredentialsAsync(
            string instanceName,
            string zoneName,
            string userName,
            GCloudContext context) =>
            GetJsonOutputAsync<WindowsInstanceCredentials>(
                $"beta compute reset-windows-password {instanceName} --zone={zoneName} --user=\"{userName}\" --quiet ",
                context);

        /// <summary>
        /// Deploys an app to App Engine.
        /// </summary>
        /// <param name="appYaml">The path to the app.yaml file to deploy.</param>
        /// <param name="version">The version to use, if no version is used gcloud will decide the version name.</param>
        /// <param name="promote">Whether to promote the app or not.</param>
        /// <param name="outputAction">The action to call with output from the command.</param>
        /// <param name="context">The context under which the command is executed.</param>
        public static Task<bool> DeployAppAsync(
            string appYaml,
            string version,
            bool promote,
            Action<string> outputAction,
            GCloudContext context)
        {
            var versionParameter = version != null ? $"--version={version}" : "";
            var promoteParameter = promote ? "--promote" : "--no-promote";
            return RunCommandAsync($"app deploy \"{appYaml}\" {versionParameter} {promoteParameter} --quiet", outputAction, context);
        }

        /// <summary>
        /// Creates a file with the cluster credentials at the given <paramref name="path"/>
        /// </summary>
        /// <param name="cluster">The name of the cluster for which to create credentials.</param>
        /// <param name="zone">The zone of the cluster.</param>
        /// <param name="path">The path where to store the credentials.</param>
        /// <param name="context">The context under which the command is executed.</param>
        public static Task<bool> CreateCredentialsForClusterAsync(string cluster, string zone, string path, GCloudContext context)
        {
            return RunCommandAsync(
                $"container clusters get-credentials {cluster} --zone={zone}",
                context: context,
                extraEnvironment: new Dictionary<string, string>
                {
                    [GCloudKubeConfigVariable] = path
                });
        }

        /// <summary>
        /// Builds a container using the Container Builder service.
        /// </summary>
        /// <param name="buildFilePath">The path to the cloudbuild.yaml file.</param>
        /// <param name="contentsPath">The contents of the container.</param>
        /// <param name="context">The context under which the command is executed.</param>
        public static Task<bool> BuildContainerAsync(string buildFilePath, string contentsPath, Action<string> outputAction, GCloudContext context)
        {
            return RunCommandAsync(
                $"container builds submit --config=\"{buildFilePath}\" \"{contentsPath}\"",
                outputAction,
                context);
        }

        /// <summary>
        /// Returns true if the <seealso cref="ResetWindowsCredentialsAsync(string, string, string, GCloudContext)"/> method can
        /// be used safely.
        /// </summary>
        /// <returns>A task that will be fulfilled to true if the method can be called, false otherwise.</returns>
        public static Task<bool> CanUseResetWindowsCredentialsAsync() => IsComponentInstalledAsync("beta");

        /// <summary>
        /// Returns true if the methods concerning kubectl and GKE can be used safely.
        /// </summary>
        /// <returns>A task that will be fullfilled to true if the GKE methods can be used.</returns>
        public static Task<bool> CanUseGKEAsync() => IsComponentInstalledAsync("kubectl");

        /// <summary>
        /// Returns the list of components that gcloud knows about.
        /// </summary>
        public static async Task<IList<string>> GetInstalledComponentsAsync()
        {
            Debug.WriteLine("Reading list of components.");
            var components = await GetJsonOutputAsync<IList<CloudSdkComponent>>("components list");
            return components.Where(x => x.State.IsInstalled).Select(x => x.Id).ToList();
        }

        /// <summary>
        /// Detects if gcloud is present in the system.
        /// </summary>
        public static bool IsGCloudCliInstalled()
        {
            Debug.WriteLine("Validating GCloud installation.");
            var gcloudPath = GetGCloudPath();
            Debug.WriteLineIf(gcloudPath == null, "Cannot find gcloud.cmd in the system.");
            Debug.WriteLineIf(gcloudPath != null, $"Found gcloud.cmd at {gcloudPath}");
            return gcloudPath != null;
        }

        /// <summary>
        /// Determines if the given gcloud component is installed.
        /// </summary>
        /// <param name="component">The component to check.</param>
        /// <returns>A task that will be fullfilled to true if the component is installed, false otherwise.</returns>
        public static async Task<bool> IsComponentInstalledAsync(string component)
        {
            if (!IsGCloudCliInstalled())
            {
                return false;
            }
            var installedComponents = await GetInstalledComponentsAsync();
            return installedComponents.Contains(component);
        }

        private static string FormatCommand(string command, GCloudContext context, bool jsonFormat)
        {
            var projectId = context?.ProjectId != null ? $"--project={context.ProjectId}" : "";
            var credentialsPath = context?.CredentialsPath != null ? $"--credential-file-override=\"{context.CredentialsPath}\"" : "";

            var format = jsonFormat ? "--format=json" : "";
            return $"gcloud {command} {projectId} {credentialsPath} {format}";
        }

        private static async Task<T> GetJsonOutputAsync<T>(string command, GCloudContext context = null)
        {
            var actualCommand = FormatCommand(command, context, jsonFormat: true);
            try
            {
                Dictionary<string, string> environment = null;
                if (context?.AppName != null)
                {
                    environment = new Dictionary<string, string> { { GCloudMetricsVariable, context?.AppName } };
                    if (context?.AppVersion != null)
                    {
                        environment[GCloudMetricsVersionVariable] = context?.AppVersion;
                    }
                }

                // This code depends on the fact that gcloud.cmd is a batch file.
                Debug.Write($"Executing gcloud command: {actualCommand}");
                return await ProcessUtils.GetJsonOutputAsync<T>("cmd.exe", $"/c {actualCommand}", environment);
            }
            catch (JsonOutputException ex)
            {
                throw new GCloudException($"Failed to execute command {actualCommand}\nInner message:\n{ex.Message}", ex);
            }
        }

        private static Task<bool> RunCommandAsync(
            string command,
            Action<string> outputAction = null,
            GCloudContext context = null,
            Dictionary<string, string> extraEnvironment = null)
        {
            var actualCommand = FormatCommand(command, context, jsonFormat: false);
            Dictionary<string, string> environment = null;
            if (context?.AppName != null)
            {
                environment = new Dictionary<string, string> { { GCloudMetricsVariable, context?.AppName } };
                if (context?.AppVersion != null)
                {
                    environment[GCloudMetricsVersionVariable] = context?.AppVersion;
                }

                if (extraEnvironment != null)
                {
                    foreach (var entry in extraEnvironment)
                    {
                        environment.Add(entry.Key, entry.Value);
                    }
                }
            }

            // This code depends on the fact that gcloud.cmd is a batch file.
            Debug.Write($"Executing gcloud command: {actualCommand}");
            return ProcessUtils.RunCommandAsync("cmd.exe", $"/c {actualCommand}", (o, e) => outputAction?.Invoke(e.Line), environment);
        }
    }
}
