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

using GoogleCloudExtension.Services.VsProject;

namespace GoogleCloudExtension.Projects
{
    public static class ParsedDteProjectExtensions
    {
        private static IVsProjectPropertyService ProjectPropertyService =>
            GoogleCloudExtensionPackage.Instance.GetMefService<IVsProjectPropertyService>();

        /// <summary>
        /// Reads a property from the project's .user file.
        /// </summary>
        /// <param name="parsedProject">The project to read the property from.</param>
        /// <param name="propertyName">The name of the property to read.</param>
        /// <returns>The value of the property.</returns>
        public static string GetUserProperty(this IParsedDteProject parsedProject, string propertyName) =>
            ProjectPropertyService.GetUserProperty(parsedProject.Project, propertyName);

        /// <summary>
        /// Saves a property to the project's .user file.
        /// </summary>
        /// <param name="parsedProject">The project to save the property to.</param>
        /// <param name="propertyName">The name of the property to save.</param>
        /// <param name="value">The value of the property.</param>
        public static void SaveUserProperty(this IParsedDteProject parsedProject, string propertyName, string value) =>
            ProjectPropertyService.SaveUserProperty(parsedProject.Project, propertyName, value);

        /// <summary>
        /// Deletes a property from the project's .user file.
        /// </summary>
        /// <param name="parsedProject">The project to delete the property from.</param>
        /// <param name="propertyName">The name of the property to delete.</param>
        public static void DeleteUserProperty(this IParsedDteProject parsedProject, string propertyName) =>
            ProjectPropertyService.DeleteUserProperty(parsedProject.Project, propertyName);
    }
}
