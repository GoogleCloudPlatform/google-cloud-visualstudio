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

namespace GoogleCloudExtension.GCloud
{
    /// <summary>
    /// This class holds the credentials used to perform GCloud operations.
    /// </summary>
    public sealed class GCloudContext
    {
        /// <summary>
        /// The path to the credentials .json file to use for the call. The .json file should be a
        /// format accetable by gcloud's --credential-file-override parameter. Typically an authorize_user kind.
        /// </summary>
        public string CredentialsPath { get; set; }

        /// <summary>
        /// The project id of the project to use for the invokation of gcloud.
        /// </summary>
        public string ProjectId { get; set; }

        /// <summary>
        /// The application name to use when reporting metrics to the server side.
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// The application version to use when reporting metrics to the server side.
        /// </summary>
        public string AppVersion { get; set; }
    }
}
