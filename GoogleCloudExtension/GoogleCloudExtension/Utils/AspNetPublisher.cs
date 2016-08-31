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
using System;
using System.IO;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    internal static class AspNetPublisher
    {
        private static readonly Lazy<string> s_msbuildPath = new Lazy<string>(GetMsbuildPath);
        private static readonly Lazy<string> s_msdeployPath = new Lazy<string>(GetMsdeployPath);

        public static async Task PublishAppAsync(
            EnvDTE.Project project,
            Instance targetInstance,
            WindowsInstanceCredentials credentials,
            Action<string> outputAction)
        {
            var stageDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(stageDirectory);

            var publishSettingsPath = Path.GetTempFileName();
            var publishSettingsContent = targetInstance.GeneratePublishSettings(credentials.User, credentials.Password);
            File.WriteAllText(publishSettingsPath, publishSettingsContent);

            if (!await CreateAppBundleAsync(project, stageDirectory, outputAction))
            {
                outputAction($"Failed to publish project {project.Name}");
                return;
            }

            if (!await DeployAppAsync(stageDirectory, publishSettingsPath, outputAction))
            {
                outputAction($"Failed to publish project {project.Name}");
            }

            File.Delete(publishSettingsPath);
            // TODO: Delete the temporary directory with the app bundle.

            outputAction($"Project {project.Name} succesfully published to Compute Engine instance {targetInstance.Name}");
        }

        private static async Task<bool> DeployAppAsync(string stageDirectory, string publishSettingsPath, Action<string> outputAction)
        {
            var arguments = "-verb:sync " +
                $@"-source:contentPath=""{stageDirectory}"" " +
                $@"-dest:contentPath=""Default Web Site"",publishSettings=""{publishSettingsPath}"" " +
                "-allowUntrusted";

            outputAction($"Publishing projet with command:");
            outputAction($"msdeploy.exe {arguments}");
            var result = await ProcessUtils.RunCommandAsync(s_msdeployPath.Value, arguments, (o, e) => outputAction(e.Line));
            if (result)
            {
                outputAction("Command succeeded.");
            }
            else
            {
                outputAction("Command failed.");
            }

            return result;
        }

        private static async Task<bool> CreateAppBundleAsync(EnvDTE.Project project, string stageDirectory, Action<string> outputAction)
        {
            var arguments = $@"""{project.FullName}""" + " " +
                "/p:Configuration=Release " +
                "/p:Platform=AnyCPU " +
                "/t:WebPublish " +
                "/p:WebPublishMethod=FileSystem " +
                "/p:DeleteExistingFiles=True " +
                $@"/p:publishUrl=""{stageDirectory}""";

            outputAction($"Execution command:");
            outputAction($"msbuild.exe {arguments}");
            var result = await ProcessUtils.RunCommandAsync(s_msbuildPath.Value, arguments, (o, e) => outputAction(e.Line));
            if (result)
            {
                outputAction("Coommand succeeded.");
            }
            else
            {
                outputAction("Command failed.");
            }

            return result;
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
