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

using EnvDTE;

namespace GoogleCloudExtension.Services.VsProject
{
    public interface IVsProjectPropertyService
    {
        /// <summary>
        /// Reads a property from the project's .user file.
        /// </summary>
        /// <param name="project">The project to read the property from.</param>
        /// <param name="propertyName">The name of the property to read.</param>
        /// <returns>The value of the property.</returns>
        string GetUserProperty(Project project, string propertyName);

        /// <summary>
        /// Saves a property to the project's .user file.
        /// </summary>
        /// <param name="project">The project to save the property to.</param>
        /// <param name="propertyName">The name of the property to save.</param>
        /// <param name="value">The value of the property.</param>
        void SaveUserProperty(Project project, string propertyName, string value);

        /// <summary>
        /// Deletes a property from the project's .user file.
        /// </summary>
        /// <param name="project">The project to delete the property from.</param>
        /// <param name="propertyName">The name of the property to delete.</param>
        void DeleteUserProperty(Project project, string propertyName);
    }
}
