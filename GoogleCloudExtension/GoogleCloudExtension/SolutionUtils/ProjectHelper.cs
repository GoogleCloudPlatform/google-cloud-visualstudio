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

using EnvDTE;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using IOPath = System.IO.Path;
using System.Runtime.InteropServices;

namespace GoogleCloudExtension.SolutionUtils
{
    internal class ProjectHelper
    {
        private const string CSharpProjectKind = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        private const string AssemblyVersionProperty = "AssemblyVersion";
        private const string AssemblyNameProperty = "AssemblyName";

        private readonly Project _project;

        /// <summary>
        /// Get a list of c# source files. 
        /// It refresh and enumerates the list of files each time it is called. 
        /// </summary>
        public List<ProjectSourceFile> SourceFiles => GetSourceFiles();

        /// <summary>
        /// Project build target version.
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// Project build target assembly name.
        /// </summary>
        public string AssemblyName { get; private set; }

        /// <summary>
        /// Project full name. Commonly, it is full path.
        /// </summary>
        public readonly string FullName;

        /// <summary>
        /// The unique name of the project. Commonly it is the project relative path from solution path.
        /// </summary>
        public readonly string UniqueName;

        /// <summary>
        /// The project root directory. 
        /// It can be null if <seealso cref="UniqueName"/> is not end of <seealso cref="FullName"/> directory name
        /// </summary>
        public readonly string ProjectRoot;

        /// <summary>
        /// A private constructor to disable direct creation of instances of <seealso cref="ProjectHelper"/> class.
        /// </summary>
        private ProjectHelper(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException($"Input parameter {nameof(project)} is null.");
            }

            _project = project;

            try
            {
                FullName = project.FullName.ToLowerInvariant();
                UniqueName = _project.UniqueName.ToLowerInvariant();
                int idx = FullName.LastIndexOf(UniqueName);
                if (FullName.Length - idx == UniqueName.Length)
                {
                    idx = FullName[idx] != IOPath.DirectorySeparatorChar ? idx - 1 : idx;
                    ProjectRoot = FullName.Substring(0, idx);
                }
                else
                {
                    // Fallback to project directory.
                    ProjectRoot = IOPath.GetDirectoryName(FullName);
                }

                ParseProperties();
            }
            catch (COMException ex)
            {
                Debug.WriteLine($"{ex}");
            }
        }

        /// <summary>
        /// Create a <seealso cref="ProjectHelper"/> object wrapping up a Project interface.
        /// Together with private constructor, this ensures object creation won't run into exception. 
        /// </summary>
        /// <param name="project">Project interface.</param>
        /// <returns>
        /// The created object.
        /// Or null if the project is null, or not supported project type.
        /// </returns>
        public static ProjectHelper Create(Project project)
        {
            if (!IsValidSupported(project))
            {
                return null;
            }

            return new ProjectHelper(project);
        }

        /// <summary>
        /// Find the project item that matches the <paramref name="sourceFilePath"/> in this Project.
        /// </summary>
        /// <param name="sourceFilePath">The source file path to be searched for.</param>
        /// <returns>
        /// <seealso cref=" ProjectSourceFile"/> object if the project item is found.
        /// null: if not found.
        /// </returns>
        public ProjectSourceFile FindSourceFile(string sourceFilePath)
        {
            foreach (var item in GetSourceFiles())
            {
                if (item.IsMatchingPath(sourceFilePath))
                {
                    return item;
                }
            }

            return null;
        }

        private List<ProjectSourceFile> GetSourceFiles()
        {
            var items = new List<ProjectSourceFile>();
            foreach (ProjectItem projectItem in _project.ProjectItems)
            {
                AddSourceFiles(projectItem, items);
            }

            return items;
        }

        private void AddSourceFiles(ProjectItem projectItem, List<ProjectSourceFile> items)
        {
            var sourceFile = ProjectSourceFile.Create(projectItem, this);
            if (sourceFile != null)
            {
                items.Add(sourceFile);
            }

            foreach (ProjectItem nestedItem in projectItem.ProjectItems)
            {
                AddSourceFiles(nestedItem, items);
            }
        }

        private static bool IsValidSupported(Project project)
        {
            try
            {
                return project != null && project.Kind == CSharpProjectKind && project.FullName != null && project.Properties != null;
            }
            catch (COMException ex)
            {
                return false;
            }
        }

        private void ParseProperties()
        {
            foreach (Property property in _project.Properties)
            {
                switch (property.Name)
                {
                    case AssemblyNameProperty:
                        AssemblyName = property.Value as string;
                        break;
                    case AssemblyVersionProperty:
                        Version = property.Value as string;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
