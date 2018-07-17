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

using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud
{
    public interface IGCloudWrapper
    {
        /// <summary>
        /// Validates that gcloud is installed with the minimum version and that the given component
        /// for gcloud is installed.
        /// </summary>
        /// <param name="component">the component to check, optional. If no component is provided only gcloud is checked.</param>
        /// <returns></returns>
        Task<GCloudValidationResult> ValidateGCloudAsync(GCloudComponent component = GCloudComponent.None);

        /// <summary>
        /// Generates the source context information for the repo stored in <paramref name="sourcePath"/> and stores it
        /// in <paramref name="outputPath"/>. If the <paramref name="sourcePath"/> does not refer to a supported CVS (currently git) then
        /// nothing will be done.
        /// </summary>
        /// <param name="sourcePath">The directory for which to generate the source contenxt.</param>
        /// <param name="outputPath">Where to store the source context files.</param>
        /// <returns>The task to be completed when the operation finishes.</returns>
        Task GenerateSourceContextAsync(string sourcePath, string outputPath);
    }
}