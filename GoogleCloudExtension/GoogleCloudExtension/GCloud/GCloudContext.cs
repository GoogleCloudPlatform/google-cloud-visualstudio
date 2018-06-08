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

using GoogleCloudExtension.Accounts;

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class holds the credentials used to perform GCloud operations.
    /// </summary>
    public sealed class GCloudContext
    {
        /// <summary>
        /// Creates the default GCloud context from the current environment.
        /// </summary>
        public GCloudContext()
        {
            CredentialsPath = CredentialsStore.Default.CurrentAccountPath;
            ProjectId = CredentialsStore.Default.CurrentProjectId;
            AppName = GoogleCloudExtensionPackage.Instance.ApplicationName;
            AppVersion = GoogleCloudExtensionPackage.Instance.ApplicationVersion;
        }

        /// <summary>
        /// The path to the credentials .json file to use for the call. The .json file should be a
        /// format accetable by gcloud's --credential-file-override parameter. Typically an authorize_user kind.
        /// </summary>
        public string CredentialsPath { get; }

        /// <summary>
        /// The project id of the project to use for the invokation of gcloud.
        /// </summary>
        public string ProjectId { get; }

        /// <summary>
        /// The application name to use when reporting metrics to the server side.
        /// </summary>
        public string AppName { get; }

        /// <summary>
        /// The application version to use when reporting metrics to the server side.
        /// </summary>
        public string AppVersion { get; }
    }
}
