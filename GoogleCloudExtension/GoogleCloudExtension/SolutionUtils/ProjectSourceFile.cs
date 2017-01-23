// Copyright 2016 Google Inc. All Rights Reserved.
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
using System.Collections.Generic;
using System.Diagnostics;
using IOPath = System.IO.Path;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GoogleCloudExtension.SolutionUtils
{
    /// <summary>
    /// This class represents a project source file. 
    /// Typlically .cs file.
    /// </summary>
    internal class ProjectSourceFile
    {
        private static readonly string[] s_supportedFileExtension = { ".cs" };

        private readonly ProjectItem _projectItem;
        private Window _window;

        /// <summary>
        /// The file path.
        /// </summary>
        public string Path => _projectItem.FileNames[0];

        /// <summary>
        /// Initializes an instance of <seealso cref="ProjectSourceFile"/> class.
        /// </summary>
        /// <param name="projectItem">A project item that is physical file.</param>
        private ProjectSourceFile(ProjectItem projectItem)
        {
            _projectItem = projectItem;
        }

        public bool IsMatchingPath(string sourceLocationFilePath)
        {
            // TODO: Advanced matching algorithm.
            // so as to rule out the root directory differ cases.
            return NormalizePath(Path) == NormalizePath(sourceLocationFilePath);
        }

        public static string NormalizePath(string path)
        {
            return IOPath.GetFileName(path).ToLower(System.Globalization.CultureInfo.InvariantCulture);
            //return Path.GetFullPath(new Uri(path).LocalPath)
            //           .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            //           .ToUpperInvariant();
        }

        /// <summary>
        /// Source location information includes file name and line number etc when it is compiled.
        /// Later on, the solution/project can be opened in different directory.
        /// This method test if the target source location file name is a possible match to the project item.
        /// </summary>
        /// <param name="projectItem">A project item.</param>
        /// <param name="sourceLocationFilePath">The source location file path.</param>
        /// <returns></returns>
        public static bool DoesPathMatch(ProjectItem projectItem, string sourceLocationFilePath)
        {
            if (!IsValidSupportedItem(projectItem))
            {
                return false;
            }

            // TODO: Advanced matching algorithm.
            return NormalizePath(sourceLocationFilePath) == NormalizePath(projectItem.FileNames[0]);
        }

        /// <summary>
        /// Create a <seealso cref="ProjectSourceFile"/> object wrapping up a ProjectItem interface.
        /// Together with private constructor, this ensures object creation won't run into exception. 
        /// </summary>
        /// <param name="projectItem">A project item.</param>
        /// <returns>
        /// The created object.
        /// Or null if the projectItem is null,  the item is not physical file.
        /// </returns>
        public static ProjectSourceFile Create(ProjectItem projectItem)
        {
            if (!IsValidSupportedItem(projectItem))
            {
                return null;
            }

            return new ProjectSourceFile(projectItem);
        }

        public Window GotoLine(int line)
        {
            Open();
            TextSelection selection = _window.Document.Selection as TextSelection;
            TextPoint tp = selection.TopPoint;
            selection.GotoLine(line, Select: false);
            return _window;
        }

        private void Open()
        {
            _window = _projectItem.Open(EnvDTE.Constants.vsViewKindPrimary);  // TODO: should it be Constants.vsViewKindCode ?
            Debug.Assert(_window != null, "If the _window is null, there is a code bug");
            _window.Visible = true;
        }

        private static bool IsValidSupportedItem(ProjectItem projectItem)
        {
            if (EnvDTE.Constants.vsProjectItemKindPhysicalFile != projectItem?.Kind ||
                !s_supportedFileExtension.Contains(IOPath.GetExtension(projectItem.Name).ToLower()))
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
