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


namespace GoogleCloudExtension.Deployment
{
    /// <summary>
    /// This class contains helper functions to deal with the Google Cloud Container Builder functionality.
    /// </summary>
    public static class CloudBuilderUtils
    {
        /// <summary>
        /// Creates the Docker image tag with the given parameters.
        /// </summary>
        /// <param name="project">The project on which the image should be built.</param>
        /// <param name="imageName">The name of the image to build.</param>
        /// <param name="imageVersion">The version tag to use.</param>
        /// <returns>The tag to identify the image to be built.</returns>
        public static string GetImageTag(string project, string imageName, string imageVersion)
            => $"gcr.io/{project}/{imageName}:{imageVersion}";
    }
}
