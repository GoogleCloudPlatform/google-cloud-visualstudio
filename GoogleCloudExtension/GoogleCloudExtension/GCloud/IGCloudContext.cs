// Copyright 2018 Google Inc. All Rights Reserved.
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
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    public interface IGCloudContext
    {
        /// <summary>
        /// Resets, or creates, the password for the given <paramref name="userName"/> in the given instance.
        /// </summary>
        Task<WindowsInstanceCredentials> ResetWindowsCredentialsAsync(
            string instanceName,
            string zoneName,
            string userName);

        /// <summary>
        /// Deploys an app to App Engine.
        /// </summary>
        /// <param name="appYaml">The path to the app.yaml file to deploy.</param>
        /// <param name="version">The version to use, if no version is used gcloud will decide the version name.</param>
        /// <param name="promote">Whether to promote the app or not.</param>
        /// <param name="outputAction">The action to call with output from the command.</param>
        Task<bool> DeployAppAsync(string appYaml, string version, bool promote, Func<string, Task> outputAction);

        /// <summary>
        /// Builds a container using the Container Builder service.
        /// </summary>
        /// <param name="imageTag">The name of the image to build.</param>
        /// <param name="contentsPath">The contents of the container, including the Dockerfile.</param>
        /// <param name="outputAction">The action to perform on each line of output.</param>
        Task<bool> BuildContainerAsync(string imageTag, string contentsPath, Func<string, Task> outputAction);

        /// <summary>
        /// The path to the credentials .json file to use for the call. The .json file should be a
        /// format accetable by gcloud's --credential-file-override parameter. Typically an authorize_user kind.
        /// </summary>
        string CredentialsPath { get; }

        /// <summary>
        /// The project id of the project to use for the invocation of gcloud.
        /// </summary>
        string ProjectId { get; }
    }
}