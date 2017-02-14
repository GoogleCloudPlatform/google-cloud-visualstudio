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
using System.IO;
using System.Linq;

namespace GoogleCloudExtension.SolutionUtils
{
    /// <summary>
    /// This class represents a project source file. 
    /// Typlically .cs file.
    /// </summary>
    internal class ProjectSourceFile
    {
        private static readonly string[] s_supportedFileExtension = { ".cs" };

        private readonly ProjectHelper _owningProject;
        private readonly Lazy<string> _relativePath;

        /// <summary>
        /// The <seealso cref="ProjectItem"/> object.
        /// </summary>
        public ProjectItem ProjectItem { get; }

        /// <summary>
        /// The file path.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Initializes an instance of <seealso cref="ProjectSourceFile"/> class.
        /// </summary>
        /// <param name="projectItem">A project item that is physical file.</param>
        /// <param name="project">The container project of type <seealso cref="ProjectHelper"/></param>
        private ProjectSourceFile(ProjectItem projectItem, ProjectHelper project)
        {
            ProjectItem = projectItem;
            FullName = ProjectItem.FileNames[0].ToLowerInvariant();
            _owningProject = project;
            _relativePath = new Lazy<string>(GetRelativePath);
        }

        /// <summary>
        /// Create a <seealso cref="ProjectSourceFile"/> object wrapping up a ProjectItem interface.
        /// Together with private constructor, this ensures object creation won't run into exception. 
        /// </summary>
        /// <param name="projectItem">A project item.</param>
        /// <param name="project">The container project of type <seealso cref="ProjectHelper"/></param>
        /// <returns>
        /// The created object.
        /// null: if the projectItem is null or the item is not physical source sfile.
        /// </returns>
        public static ProjectSourceFile Create(ProjectItem projectItem, ProjectHelper project)
        {
            if (!IsValidSupportedItem(projectItem) || project == null)
            {
                return null;
            }

            return new ProjectSourceFile(projectItem, project);
        }

        /// <summary>
        /// Verifies if a giving path match the source file item path.
        /// </summary>
        public bool IsMatchingPath(string filePath)
        {
            var path = filePath.ToLowerInvariant();
            return path.EndsWith(_relativePath.Value);
        }

        /// <summary>
        /// Get the project item path relative to the project root. 
        /// The relative path starts with '\' character.
        /// (1) If the file path of the project item starts with the project root path. 
        ///     The part after the root path is the relative file path.
        ///     Example:  project root is c:\aa\bb,  file item path is c:\aa\bb\cce.cs.  
        ///               relative path is \cce.cs
        /// (2) Fallback is to compare every subpath of the project full path and full path of this project item,
        ///     from the start of the path.  Starting from the part that differs are the relative path.
        ///     Example:  c:\aa\bb\cc.csproj   c:\aa\bb\mm\ff.cs  --> the common parts are "c:" "aa", 
        ///               relative path is \mm\ff.cs
        /// </summary>
        private string GetRelativePath()
        {
            if (_owningProject.ProjectRoot != null && FullName.StartsWith(_owningProject.ProjectRoot))
            {
                return FullName.Substring(_owningProject.ProjectRoot.Length);
            }
            else
            {
                // Fallback to compare the root of both paths.
                int baseIndex = 0;
                for (int i = 0; i < FullName.Length && i < _owningProject.FullName.Length && FullName[i] == _owningProject.FullName[i]; ++i)
                {
                    if (FullName[i] == Path.DirectorySeparatorChar)
                    {
                        baseIndex = i;
                    }
                }
                return FullName.Substring(baseIndex);
            }
        }

        private static bool IsValidSupportedItem(ProjectItem projectItem)
        {
            if (EnvDTE.Constants.vsProjectItemKindPhysicalFile != projectItem?.Kind ||
                !s_supportedFileExtension.Contains(Path.GetExtension(projectItem.Name).ToLower()))
            {
                return false;
            }

            if (projectItem.FileCount != 1)
            {
                Debug.WriteLine($"project item file count is {projectItem.FileCount}. Expects 1");
                return false;
            }

            return true;
        }
    }
}
