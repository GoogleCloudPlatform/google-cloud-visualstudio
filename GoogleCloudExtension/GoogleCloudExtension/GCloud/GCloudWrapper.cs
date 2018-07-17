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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class wraps the gcloud command and offers up some of its services in a
    /// as async methods.
    /// </summary>
    [Export(typeof(IGCloudWrapper))]
    public class GCloudWrapper : IGCloudWrapper
    {
        // The minimum version of the Google Cloud SDK that the extension can work with. Update this only when
        // a feature appears in the Cloud SDK that is absolutely required for the extension to work.
        public const string GCloudSdkMinimumVersion = "174.0.0";

        // These variables specify the environment to be reported by gcloud when reporting metrics. These variables
        // are only used with gcloud which is why they're private here.

        // Minimum version of Cloud SDK that is acceptable.
        private static readonly Version s_minimumVersion = new Version(GCloudSdkMinimumVersion);

        // Mapping between the enum and the actual gcloud component.
        private static readonly Dictionary<GCloudComponent, string> s_componentNames =
            new Dictionary<GCloudComponent, string>
            {
                [GCloudComponent.Beta] = "beta",
                [GCloudComponent.Kubectl] = "kubectl",
            };

        private readonly Lazy<IProcessService> _processService;
        private IProcessService ProcessService => _processService.Value;

        public GCloudWrapper(Lazy<IProcessService> processService)
        {
            _processService = processService;
        }

        /// <summary>
        /// Validates that gcloud is installed with the minimum version and that the given component
        /// for gcloud is installed.
        /// </summary>
        /// <param name="component">the component to check, optional. If no component is provided only gcloud is checked.</param>
        /// <returns></returns>
        public async Task<GCloudValidationResult> ValidateGCloudAsync(GCloudComponent component = GCloudComponent.None)
        {
            if (!IsGCloudCliInstalled())
            {
                return GCloudValidationResult.NotInstalled;
            }

            Version cloudSdkVersion = await GetInstalledCloudSdkVersionAsync();
            if (cloudSdkVersion < s_minimumVersion)
            {
                return GCloudValidationResult.GetObsoleteVersion(cloudSdkVersion);
            }

            if (component != GCloudComponent.None && !await IsComponentInstalledAsync(component))
            {
                return GCloudValidationResult.MissingComponent;
            }

            return GCloudValidationResult.Valid;
        }

        /// <summary>
        /// Generates the source context information for the repo stored in <paramref name="sourcePath"/> and stores it
        /// in <paramref name="outputPath"/>. If the <paramref name="sourcePath"/> does not refer to a supported CVS (currently git) then
        /// nothing will be done.
        /// </summary>
        /// <param name="sourcePath">The directory for which to generate the source contenxt.</param>
        /// <param name="outputPath">Where to store the source context files.</param>
        /// <returns>The task to be completed when the operation finishes.</returns>
        public async Task GenerateSourceContextAsync(string sourcePath, string outputPath)
        {
            bool result = await RunCommandAsync(
                $"debug source gen-repo-info-file --output-directory=\"{outputPath}\" --source-directory=\"{sourcePath}\"");
            if (!result)
            {
                Debug.WriteLine($"Could not find git repo at {sourcePath}");
            }
        }

        private async Task<IList<string>> GetInstalledComponentsAsync()
        {
            Debug.WriteLine("Reading list of components.");
            IList<CloudSdkComponent> components = await GetJsonOutputAsync<IList<CloudSdkComponent>>("components list");
            return components.Where(x => x.State.IsInstalled).Select(x => x.Id).ToList();
        }

        private bool IsGCloudCliInstalled()
        {
            Debug.WriteLine("Validating GCloud installation.");
            string gcloudPath = PathUtils.GetCommandPathFromPATH("gcloud.cmd");
            Debug.WriteLineIf(gcloudPath == null, "Cannot find gcloud.cmd in the system.");
            Debug.WriteLineIf(gcloudPath != null, $"Found gcloud.cmd at {gcloudPath}");
            return gcloudPath != null;
        }

        private async Task<bool> IsComponentInstalledAsync(GCloudComponent component)
        {
            if (!IsGCloudCliInstalled())
            {
                return false;
            }
            IList<string> installedComponents = await GetInstalledComponentsAsync();
            return installedComponents.Contains(s_componentNames[component]);
        }

        private async Task<Version> GetInstalledCloudSdkVersionAsync()
        {
            if (!IsGCloudCliInstalled())
            {
                return null;
            }

            CloudSdkVersions version = await GetJsonOutputAsync<CloudSdkVersions>("version");
            return new Version(version.SdkVersion);
        }

        private async Task<T> GetJsonOutputAsync<T>(string command)
        {
            string actualCommand = $"gcloud {command} --format=json";
            try
            {

                // This code depends on the fact that gcloud.cmd is a batch file.
                Debug.Write($"Executing gcloud command: {actualCommand}");
                return await ProcessService.GetJsonOutputAsync<T>(
                    file: "cmd.exe",
                    args: $"/c {actualCommand}");
            }
            catch (JsonOutputException ex)
            {
                throw new GCloudException($"Failed to execute command {actualCommand}", ex);
            }
        }

        private Task<bool> RunCommandAsync(string command)
        {
            void DebugOutputHandler(object o, OutputHandlerEventArgs e) => Debug.WriteLine(e.Line);

            // This code depends on the fact that gcloud.cmd is a batch file.
            return ProcessService.RunCommandAsync("cmd.exe", $"/c gcloud {command}", DebugOutputHandler);
        }
    }
}
