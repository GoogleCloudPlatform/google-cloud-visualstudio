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

using System;
using System.Diagnostics;
using System.IO;

namespace GoogleCloudExtension.Deployment
{
    internal static class CloudBuilderUtils
    {
        private const string CloudBuildFileContent =
           "steps:\n" +
            "- name: gcr.io/cloud-builders/docker\n" +
            "  args: [ 'build', '-t', '{0}', '--no-cache', '--pull', '.' ]\n" +
            "images:\n" +
            "  ['{0}']\n";

        internal static string CreateBuildFile(string project, string imageName, string imageVersion, string buildFilePath)
        {
            var tag = GetDeploymentTag(
                project: project,
                imageName: imageName,
                imageVersion: imageVersion);
            Debug.WriteLine($"Creating build file for tag {tag} at {buildFilePath}");
            var content = String.Format(CloudBuildFileContent, tag);
            File.WriteAllText(buildFilePath, content);
            return tag;
        }

        private static void Cleanup(string buildFilePath)
        {
            try
            {
                if (File.Exists(buildFilePath))
                {
                    File.Delete(buildFilePath);
                }
            }
            catch (IOException)
            {
                Debug.WriteLine($"Failed to cleanup file {buildFilePath}");
            }
        }

        private static string GetDeploymentTag(string project, string imageName, string imageVersion)
            => $"gcr.io/{project}/{imageName}:{imageVersion}";
    }
}
