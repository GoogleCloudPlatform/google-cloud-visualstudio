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
        private const string GCloudMetricsVariable = "CLOUDSDK_METRICS_ENVIRONMENT";
        private const string GCloudMetricsVersionVariable = "CLOUDSDK_METRICS_ENVIRONMENT_VERSION";

        /// <summary>
        /// Finds the location of gcloud.cmd by following all of the directories in the PATH environment
        /// variable until it finds it. With this we assume that in order to run the extension gcloud.cmd is
        /// in the PATH.
        /// </summary>
        /// <returns></returns>
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
        /// <returns></returns>
        public static Task<WindowsInstanceCredentials> ResetWindowsCredentialsAsync(
            string instanceName,
            string zoneName,
            string userName,
            Context context) =>
            GetJsonOutputAsync<WindowsInstanceCredentials>(
                $"beta compute reset-windows-password {instanceName} --zone={zoneName} --user={userName} --quiet ",
                context);

        /// <summary>
        /// Returns true if the <seealso cref="ResetWindowsCredentialsAsync(string, string, string, Context)"/> method can
        /// be used safely.
        /// </summary>
        /// <returns>A task that will be fulfilled to true if the method can be called, false otherwise.</returns>
        public static Task<bool> CanUseResetWindowsCredentialsAsync() => IsComponentInstalledAsync("beta");

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

        private static string FormatCommand(string command, Context context)
        {
            var projectId = context?.ProjectId != null ? $"--project={context.ProjectId}" : "";
            var credentialsPath = context?.CredentialsPath != null ? $"--credential-file-override=\"{context.CredentialsPath}\"" : "";
            return $"gcloud {command} {projectId} {credentialsPath} --format=json";
        }

        private static async Task<T> GetJsonOutputAsync<T>(string command, Context context = null)
        {
            var actualCommand = FormatCommand(command, context);
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
    }
}
