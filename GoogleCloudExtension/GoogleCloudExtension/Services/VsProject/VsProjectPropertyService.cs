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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;

namespace GoogleCloudExtension.Services.VsProject
{
    [Export(typeof(IVsProjectPropertyService))]
    public class VsProjectPropertyService : IVsProjectPropertyService
    {
        public const uint UserFileFlag = (uint)_PersistStorageType.PST_USER_FILE;
        public const int HrPropertyNotFound = unchecked((int)0x8004C738);

        private readonly Lazy<IVsSolution> _solutionServiceLazy;

        private IVsSolution SolutionService => _solutionServiceLazy.Value;

        [ImportingConstructor]
        public VsProjectPropertyService(Lazy<SVsServiceProvider> serviceProvider)
        {
            IVsSolution SolutionFactory()
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return (IVsSolution)serviceProvider.Value.GetService(typeof(SVsSolution));
            }

            _solutionServiceLazy = new Lazy<IVsSolution>(SolutionFactory);
        }

        /// <summary>
        /// Reads a property from the project's .user file.
        /// </summary>
        /// <param name="project">The project to read the property from.</param>
        /// <param name="propertyName">The name of the property to read.</param>
        /// <returns>The value of the property.</returns>
        public string GetUserProperty(Project project, string propertyName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsBuildPropertyStorage propertyStore = GetProjectPropertyStore(project);

            ErrorHandler.ThrowOnFailure(
                propertyStore.GetPropertyValue(propertyName, null, UserFileFlag, out string value), HrPropertyNotFound);
            return value;

        }

        /// <summary>
        /// Saves a property to the project's .user file.
        /// </summary>
        /// <param name="project">The project to save the property to.</param>
        /// <param name="propertyName">The name of the property to save.</param>
        /// <param name="value">The value of the property.</param>
        public void SaveUserProperty(Project project, string propertyName, string value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsBuildPropertyStorage propertyStore = GetProjectPropertyStore(project);

            int hr;
            if (value == null)
            {
                hr = propertyStore.RemoveProperty(propertyName, null, UserFileFlag);
            }
            else
            {
                hr = propertyStore.SetPropertyValue(propertyName, null, UserFileFlag, value);
            }

            ErrorHandler.ThrowOnFailure(hr);
        }

        /// <summary>
        /// Deletes a property from the project's .user file.
        /// </summary>
        /// <param name="project">The project to delete the property from.</param>
        /// <param name="propertyName">The name of the property to delete.</param>
        public void DeleteUserProperty(Project project, string propertyName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsBuildPropertyStorage propertyStore = GetProjectPropertyStore(project);
            ErrorHandler.ThrowOnFailure(
                propertyStore.RemoveProperty(propertyName, null, UserFileFlag));
        }

        private IVsBuildPropertyStorage GetProjectPropertyStore(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ErrorHandler.ThrowOnFailure(
                SolutionService.GetProjectOfUniqueName(project.UniqueName, out IVsHierarchy vsProject));
            // ReSharper disable once SuspiciousTypeConversion.Global
            return (IVsBuildPropertyStorage)vsProject;
        }
    }
}
