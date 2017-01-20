﻿// Copyright 2017 Google Inc. All Rights Reserved.
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
    /// <summary>
    /// This class contains helper functions to deal with the Google Cloud Container Builder functionality.
    /// </summary>
    internal static class CloudBuilderUtils
    {
        /// <summary>
        /// The template to produce a cloudbuild.yaml file to build an image using the the container builder.
        /// </summary>
        private const string CloudBuildFileContent =
           "steps:\n" +
            "- name: gcr.io/cloud-builders/docker\n" +
            "  args: [ 'build', '-t', '{0}', '--no-cache', '--pull', '.' ]\n" +
            "images:\n" +
            "  ['{0}']\n";

        /// <summary>
        /// Creates a cloudbuild.yaml for the given <paramref name="imageName"/> in the given <paramref name="project"/>.
        /// </summary>
        /// <param name="project">The project on which the image should be built.</param>
        /// <param name="imageName">The name of the image to build.</param>
        /// <param name="imageVersion">The version tag to use.</param>
        /// <param name="buildFilePath">Where to store the produced build file.</param>
        /// <returns>The tag to identify the image to be built.</returns>
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

        private static string GetDeploymentTag(string project, string imageName, string imageVersion)
            => $"gcr.io/{project}/{imageName}:{imageVersion}";
    }
}
