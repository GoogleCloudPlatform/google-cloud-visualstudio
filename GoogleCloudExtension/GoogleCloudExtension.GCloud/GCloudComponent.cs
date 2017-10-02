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
    /// This enum describe the well known components for gcloud.
    /// </summary>
    public enum GCloudComponent
    {
        /// <summary>
        /// Placeholder for no component.
        /// </summary>
        None = 0,

        /// <summary>
        /// The beta component, contains the beta features for gcloud, only depend on this if
        /// absolutely necessary as things change rapidly.
        /// </summary>
        Beta,

        /// <summary>
        /// The kubectl component, which installs the necessary tools to work with Kubernetes clusters.
        /// </summary>
        Kubectl,
    }
}
