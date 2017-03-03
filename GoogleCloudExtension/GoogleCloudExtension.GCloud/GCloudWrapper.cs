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
        // The minimum version of the Google Cloud SDK that the extension can work with. Update this only when
        // a feature appears in the Cloud SDK that is absolutely required for the extension to work.
        public const string GCloudSdkMinimumVersion = "146.0.0";

        // These variables specify the environment to be reported by gcloud when reporting metrics. These variables
        // are only used with gcloud which is why they're private here.
        private const string GCloudMetricsVariable = "CLOUDSDK_METRICS_ENVIRONMENT";
        private const string GCloudMetricsVersionVariable = "CLOUDSDK_METRICS_ENVIRONMENT_VERSION";

        // Minimum version of Cloud SDK that is acceptable.
        private static readonly Version s_minimumVersion = new Version(GCloudSdkMinimumVersion);

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
        /// <param name="useRuntimeBuilder">Whether to enable runtime builders or not.</param>
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
            Dictionary<string, string> environment = null;

            return RunCommandAsync(
                $"beta app deploy \"{appYaml}\" {versionParameter} {promoteParameter} --skip-staging --quiet",
                outputAction,
                context,
                environment);
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
                    [CommonEnvironmentVariables.GCloudKubeConfigVariable] = path,
                    [CommonEnvironmentVariables.GCloudContainerUseApplicationDefaultCredentialsVariable] = CommonEnvironmentVariables.TrueValue,
                    [CommonEnvironmentVariables.GoogleApplicationCredentialsVariable] = context.CredentialsPath,
                });
        }

        /// <summary>
        /// Returns the <seealso cref="KubectlContext"/> instance to use for the given <paramref name="cluster"/> when
        /// performing Kubernetes operations.
        /// </summary>
        /// <param name="cluster">The name of the cluster for which to create credentials.</param>
        /// <param name="zone">The zone of the cluster.</param>
        /// <param name="context">The context under which the command is executed.</param>
        /// <returns>The <seealso cref="KubectlContext"/> for the given <paramref name="cluster"/>.</returns>
        public static async Task<KubectlContext> GetKubectlContextForClusterAsync(
            string cluster,
            string zone,
            GCloudContext context)
        {
            var tempPath = Path.GetTempFileName();
            if (!await CreateCredentialsForClusterAsync(
                    cluster: cluster,
                    zone: zone,
                    path: tempPath,
                    context: context))
            {
                throw new GCloudException($"Failed to get credentials for cluster {cluster}");
            }
            return new KubectlContext(
                configPath: tempPath,
                credentialsPath: context.CredentialsPath);
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
        /// Validates that gcloud is installed with the minimum version and that the given component
        /// for gcloud is installed.
        /// </summary>
        /// <param name="component">the component to check, optional. If no component is provided only gcloud is checked.</param>
        /// <returns></returns>
        public static async Task<GCloudValidationResult> ValidateGCloudAsync(string component = null)
        {
            if (!IsGCloudCliInstalled())
            {
                return new GCloudValidationResult(isCloudSdkInstalled: false);
            }

            var cloudSdkVersion = await GetInstalledCloudSdkVersionAsync();
            if (cloudSdkVersion < s_minimumVersion)
            {
                return new GCloudValidationResult(isCloudSdkInstalled: true, isCloudSdkUpdated: false, cloudSdkVersion: cloudSdkVersion);
            }

            if (component != null && !await IsComponentInstalledAsync(component))
            {
                return new GCloudValidationResult(
                    isCloudSdkInstalled: true,
                    isCloudSdkUpdated: true,
                    isRequiredComponentInstalled: false,
                    cloudSdkVersion: cloudSdkVersion);
            }

            return new GCloudValidationResult(
                isCloudSdkInstalled: true,
                isCloudSdkUpdated: true,
                isRequiredComponentInstalled: true,
                cloudSdkVersion: cloudSdkVersion);
        }

        private static async Task<IList<string>> GetInstalledComponentsAsync()
        {
            Debug.WriteLine("Reading list of components.");
            var components = await GetJsonOutputAsync<IList<CloudSdkComponent>>("components list");
            return components.Where(x => x.State.IsInstalled).Select(x => x.Id).ToList();
        }

        private static bool IsGCloudCliInstalled()
        {
            Debug.WriteLine("Validating GCloud installation.");
            var gcloudPath = GetGCloudPath();
            Debug.WriteLineIf(gcloudPath == null, "Cannot find gcloud.cmd in the system.");
            Debug.WriteLineIf(gcloudPath != null, $"Found gcloud.cmd at {gcloudPath}");
            return gcloudPath != null;
        }

        private static async Task<bool> IsComponentInstalledAsync(string component)
        {
            if (!IsGCloudCliInstalled())
            {
                return false;
            }
            var installedComponents = await GetInstalledComponentsAsync();
            return installedComponents.Contains(component);
        }

        private static async Task<Version> GetInstalledCloudSdkVersionAsync()
        {
            if (!IsGCloudCliInstalled())
            {
                return null;
            }

            var version = await GetJsonOutputAsync<CloudSdkVersions>("version");
            return new Version(version.SdkVersion);
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
                var environment = GetContextEnvironment(context);

                // This code depends on the fact that gcloud.cmd is a batch file.
                Debug.Write($"Executing gcloud command: {actualCommand}");
                return await ProcessUtils.GetJsonOutputAsync<T>(file: "cmd.exe", args: $"/c {actualCommand}", environment: environment);
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
            var environment = GetContextEnvironment(context, extraEnvironment);

            // This code depends on the fact that gcloud.cmd is a batch file.
            Debug.Write($"Executing gcloud command: {actualCommand}");
            return ProcessUtils.RunCommandAsync(
                file: "cmd.exe",
                args: $"/c {actualCommand}",
                handler: (o, e) => outputAction?.Invoke(e.Line),
                environment: environment);
        }

        private static Dictionary<string, string> GetContextEnvironment(
            GCloudContext context,
            Dictionary<string, string> extraEnvironment = null)
        {
            Dictionary<string, string> environment = new Dictionary<string, string>();

            if (context?.AppName != null)
            {
                environment[GCloudMetricsVariable] = context.AppName;
                if (context.AppVersion != null)
                {
                    environment[GCloudMetricsVersionVariable] = context.AppVersion;
                }
            }

            if (extraEnvironment != null)
            {
                foreach (var entry in extraEnvironment)
                {
                    environment.Add(entry.Key, entry.Value);
                }
            }

            return environment;
        }
    }
}
