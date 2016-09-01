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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class offers services to perform deployments for ASP.NET 4.x applications to a GCE VM.
    /// </summary>
    public static class AspnetDeployment
    {
        private static readonly Lazy<string> s_msbuildPath = new Lazy<string>(GetMsbuildPath);
        private static readonly Lazy<string> s_msdeployPath = new Lazy<string>(GetMsdeployPath);

        /// <summary>
        /// Publishes an ASP.NET 4.x project to the given GCE <seealso cref="Instance"/>.
        /// </summary>
        /// <param name="projectPath">The full path to the project file.</param>
        /// <param name="targetInstance">The instance to which deploy.</param>
        /// <param name="credentials">The Windows credentials to use to deploy to the <paramref name="targetInstance"/>.</param>
        /// <param name="outputAction">The action to call with lines of output.</param>
        /// <returns></returns>
        public static async Task<bool> PublishProjectAsync(
            string projectPath,
            Instance targetInstance,
            WindowsInstanceCredentials credentials,
            Action<string> outputAction)
        {
            var stageDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(stageDirectory);

            var publishSettingsPath = Path.GetTempFileName();
            var publishSettingsContent = targetInstance.GeneratePublishSettings(credentials.User, credentials.Password);
            File.WriteAllText(publishSettingsPath, publishSettingsContent);

            if (!await CreateAppBundleAsync(projectPath, stageDirectory, outputAction))
            {
                return false;
            }

            if (!await DeployAppAsync(stageDirectory, publishSettingsPath, outputAction))
            {
                return false;
            }

            File.Delete(publishSettingsPath);
            // TODO: Delete the temporary directory with the app bundle.

            return true;
        }

        private static Task<bool> DeployAppAsync(string stageDirectory, string publishSettingsPath, Action<string> outputAction)
        {
            var arguments = "-verb:sync " +
                $@"-source:contentPath=""{stageDirectory}"" " +
                $@"-dest:contentPath=""Default Web Site"",publishSettings=""{publishSettingsPath}"" " +
                "-allowUntrusted";

            outputAction($"msdeploy.exe {arguments}");
            return ProcessUtils.RunCommandAsync(s_msdeployPath.Value, arguments, (o, e) => outputAction(e.Line));
        }

        private static Task<bool> CreateAppBundleAsync(string projectPath, string stageDirectory, Action<string> outputAction)
        {
            var arguments = $@"""{projectPath}""" + " " +
                "/p:Configuration=Release " +
                "/p:Platform=AnyCPU " +
                "/t:WebPublish " +
                "/p:WebPublishMethod=FileSystem " +
                "/p:DeleteExistingFiles=True " +
                $@"/p:publishUrl=""{stageDirectory}""";

            outputAction($"msbuild.exe {arguments}");
            return ProcessUtils.RunCommandAsync(s_msbuildPath.Value, arguments, (o, e) => outputAction(e.Line));
        }

        private static string GetMsbuildPath()
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            return Path.Combine(programFilesPath, @"MSBuild\14.0\Bin\MSBuild.exe");
        }

        private static string GetMsdeployPath()
        {
            var programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            return Path.Combine(programFilesPath, @"IIS\Microsoft Web Deploy V3\msdeploy.exe");
        }
    }
}
