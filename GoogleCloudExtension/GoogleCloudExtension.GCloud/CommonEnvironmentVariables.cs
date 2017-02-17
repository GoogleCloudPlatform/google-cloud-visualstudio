// Copyright 2017 Google Inc. All Rights Reserved.
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
    /// This class contains defintions for the environment variables used by the various wrappers.
    /// </summary>
    internal static class CommonEnvironmentVariables
    {
        // This variables is used to force gcloud to use application default credentials when generating
        // the cluster information for the cluster.
        public const string GCloudContainerUseApplicationDefaultCredentialsVariable = "CLOUDSDK_CONTAINER_USE_APPLICATION_DEFAULT_CREDENTIALS";
        public const string TrueValue = "true";

        // This variable is used to override the location of the application default credentials
        // with the current user's credentials.
        public const string GoogleApplicationCredentialsVariable = "GOOGLE_APPLICATION_CREDENTIALS";

        // This variable contains the path to the configuration to be used for kubernetes operations.
        public const string GCloudKubeConfigVariable = "KUBECONFIG";
    }
}
