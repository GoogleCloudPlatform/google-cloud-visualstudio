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
using System.IO;
using System.Runtime.InteropServices;

namespace GoogleCloudExtension.SolutionUtils
{
    /// <summary>
    /// An wrapper on top of Visual Studio extenstion API Project interface. 
    /// </summary>
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
        public string FullName { get; }

        /// <summary>
        /// The unique name of the project. Commonly it is the project relative path from solution path.
        /// </summary>
        public string UniqueName { get; }

        /// <summary>
        /// The project root directory. 
        /// </summary>
        public string ProjectRoot { get; }

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
                UniqueName = _project.UniqueName?.ToLowerInvariant();                
                if (FullName.EndsWith(UniqueName))
                {
                    int len = FullName.Length - UniqueName.Length; 
                    if (len > 0 && FullName[len] == Path.DirectorySeparatorChar)
                    {
                        ProjectRoot = FullName.Substring(0, len-1);
                    }
                    else if (UniqueName[0] == Path.DirectorySeparatorChar)
                    {
                        ProjectRoot = FullName.Substring(0, len);
                    }
                }
                
                // Fallback to project directory.
                ProjectRoot = ProjectRoot ?? Path.GetDirectoryName(FullName);

                GetAssembyInfoFromProperties();
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

        private void GetAssembyInfoFromProperties()
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
