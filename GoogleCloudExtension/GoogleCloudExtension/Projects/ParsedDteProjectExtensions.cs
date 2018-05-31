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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleCloudExtension.Projects
{
    public static class ParsedDteProjectExtensions
    {
        public const uint UserFileFlag = (uint)_PersistStorageType.PST_USER_FILE;

        /// <summary>
        /// Reads a property from the project's .user file.
        /// </summary>
        /// <param name="parsedProject">The project to read the property from.</param>
        /// <param name="propertyName">The name of the property to read.</param>
        /// <returns>The value of the property.</returns>
        public static string GetUserProperty(this IParsedDteProject parsedProject, string propertyName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsBuildPropertyStorage propertyStore = GetProjectPropertyStore(parsedProject);
            ErrorHandler.ThrowOnFailure(
                propertyStore.GetPropertyValue(propertyName, null, UserFileFlag, out string value));
            return value;
        }

        /// <summary>
        /// Saves a property to the project's .user file.
        /// </summary>
        /// <param name="parsedProject">The project to save the property to.</param>
        /// <param name="propertyName">The name of the property to save.</param>
        /// <param name="value">The value of the property.</param>
        public static void SaveUserProperty(this IParsedDteProject parsedProject, string propertyName, string value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsBuildPropertyStorage propertyStore = GetProjectPropertyStore(parsedProject);
            ErrorHandler.ThrowOnFailure(
                propertyStore.SetPropertyValue(propertyName, null, UserFileFlag, value));
        }

        /// <summary>
        /// Deletes a property from the project's .user file.
        /// </summary>
        /// <param name="parsedProject">The project to delete the property from.</param>
        /// <param name="propertyName">The name of the property to delete.</param>
        public static void DeleteUserProperty(this IParsedDteProject parsedProject, string propertyName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsBuildPropertyStorage propertyStore = GetProjectPropertyStore(parsedProject);
            ErrorHandler.ThrowOnFailure(
                propertyStore.RemoveProperty(propertyName, null, UserFileFlag));
        }

        private static IVsBuildPropertyStorage GetProjectPropertyStore(IParsedDteProject parsedProject)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var solution = GoogleCloudExtensionPackage.Instance.GetService<IVsSolution>();
            ErrorHandler.ThrowOnFailure(
                solution.GetProjectOfUniqueName(parsedProject.Project.UniqueName, out IVsHierarchy vsProject));
            // ReSharper disable once SuspiciousTypeConversion.Global
            return (IVsBuildPropertyStorage)vsProject;
        }
    }
}
