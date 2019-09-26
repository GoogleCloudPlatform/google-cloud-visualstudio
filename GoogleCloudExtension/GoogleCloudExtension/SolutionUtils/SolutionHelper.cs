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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Constants = EnvDTE.Constants;

namespace GoogleCloudExtension.SolutionUtils
{
    /// <summary>
    /// This class wraps the Visual Studio solution and simplifies its usage.
    /// </summary>
    internal class SolutionHelper
    {
        private readonly Solution _solution;

        /// <summary>
        /// Returns the <seealso cref="SolutionBuild2"/> associated with the current solution.
        /// </summary>
        private SolutionBuild2 SolutionBuild
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return _solution.SolutionBuild as SolutionBuild2;
            }
        }

        /// <summary>
        /// Returns the current solution open, null if no solution is present.
        /// </summary>
        public static SolutionHelper CurrentSolution
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var dte = (DTE)Package.GetGlobalService(typeof(DTE));
                var solution = dte.Solution;
                return solution != null ? new SolutionHelper(solution) : null;
            }
        }

        /// <summary>
        /// Returns the startup project for the current solution.
        /// </summary>
        public ProjectHelper StartupProject
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return GetStartupProject();
            }
        }

        /// <summary>
        /// Returns the selected project in the Solution Explorer.
        /// </summary>
        public ProjectHelper SelectedProject
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return GetSelectedProject();
            }
        }

        /// <summary>
        /// Get a list of <seealso cref="ProjectHelper"/> objects under current solution.
        /// </summary>
        public List<ProjectHelper> Projects
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return GetProjectList();
            }
        }

        private SolutionHelper(Solution solution)
        {
            _solution = solution;
        }

        /// <summary>
        /// Find project items that matches the path of <paramref name="sourceLocationFilePath"/>.
        /// </summary>
        /// <param name="sourceLocationFilePath">The source file path to be searched for.</param>
        /// <returns>A list of <seealso cref="ProjectSourceFile"/> objects that matches the searched file path.</returns>
        public List<ProjectSourceFile> FindMatchingSourceFile(string sourceLocationFilePath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var query = Projects.SelectMany(x => x.SourceFiles).Where(y => y.IsMatchingPath(sourceLocationFilePath));
            return query.ToList();
        }

        private ProjectHelper GetSelectedProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var selectedProjectDirectory = GetSelectedProjectDirectory();
            if (selectedProjectDirectory == null)
            {
                return null;
            }

            // Fetch the project that lives in that directory and parse it.
            var projects = GetSolutionProjects();
            foreach (Project p in projects)
            {
                var projectDirectory = Path.GetDirectoryName(p.FullName);
                Debug.Assert(projectDirectory != null, nameof(projectDirectory) + " != null");
                if (projectDirectory.Equals(selectedProjectDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    return new ProjectHelper(p);
                }
            }

            // Failed to determine the project.
            Debug.WriteLine($"Could not find a project in {selectedProjectDirectory}");
            return null;
        }

        /// <summary>
        /// Uses COM interop to find out what is the selected node in the Solution Explorer and from that what
        /// is the path to the directory that contains it.
        /// Note: This method does not guarantee that the selected node is a project, the caller should ensure that
        /// it is called in a context in which it will.
        /// </summary>
        /// <returns>The path to the directory that contains the project.</returns>
        private static string GetSelectedProjectDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return null;
            }

            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;

            try
            {
                var hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out uint itemId, out IVsMultiItemSelect _, out selectionContainerPtr);
                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemId == VSConstants.VSITEMID_NIL)
                {
                    return null;
                }

                if (!(Marshal.GetObjectForIUnknown(hierarchyPtr) is IVsHierarchy hierarchy))
                {
                    return null;
                }

                hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectDir, out object result);
                return (string)result;
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }

        private ProjectHelper GetStartupProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var sb = SolutionBuild;
            if (sb == null)
            {
                return null;
            }

            if (!(sb.StartupProjects is Array startupProjects))
            {
                return null;
            }

            if (!(startupProjects.GetValue(0) is string startupProjectFilePath))
            {
                return null;
            }

            string projectName = Path.GetFileNameWithoutExtension(startupProjectFilePath);
            var projects = GetSolutionProjects();
            foreach (Project p in projects)
            {
                if (p.Name == projectName)
                {
                    return new ProjectHelper(p);
                }
            }

            // The project could not be found.
            Debug.WriteLine($"Could not find project {startupProjectFilePath}");
            return null;
        }

        private List<ProjectHelper> GetProjectList()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            List<ProjectHelper> list = new List<ProjectHelper>();
            var projects = GetSolutionProjects();
            foreach (Project project in projects)
            {
                list.Add(new ProjectHelper(project));
            }

            return list;
        }

        /// <summary>
        /// Lists all of the projects included in the solution, recursing as necessary to extract the
        /// projects from the solution folders.
        /// Internal for testing.
        /// </summary>
        private IList<Project> GetSolutionProjects()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            List<Project> result = new List<Project>();

            var rootProjects = _solution.Projects;
            foreach (Project item in rootProjects)
            {
                // If it's a folder, search projects under it.
                if (item.Kind == Constants.vsProjectKindSolutionItems)
                {
                    result.AddRange(GetSolutionFolderProjects(item));
                }
                // Skipping unsupported projects.
                else if (ProjectHelper.IsValidSupported(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }

        /// <summary>
        /// Performs a deep search into the given solution folder, looking for all of the projects
        /// directly, or indirectly, under it.
        /// </summary>
        private IList<Project> GetSolutionFolderProjects(Project solutionDir)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            List<Project> result = new List<Project>();

            // Indexes in ProjectItems start at 1 and go through Count.
            for (int i = 1; i <= solutionDir.ProjectItems.Count; ++i)
            {
                var item = solutionDir.ProjectItems.Item(i).SubProject;
                if (item == null)
                {
                    continue;
                }
                // If it's a folder, search projects under it.
                if (item.Kind == Constants.vsProjectKindSolutionItems)
                {
                    result.AddRange(GetSolutionFolderProjects(item));
                }
                // Skipping unsupported projects.
                else if (ProjectHelper.IsValidSupported(item))
                {
                    result.Add(item);
                }
            }

            return result;
        }
    }
}
