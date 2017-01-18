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
using EnvDTE80;
using Microsoft.VisualStudio;
using VSOLEInterop = Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace GoogleCloudExtension.SolutionUtils
{
    /// <summary>
    /// This class wraps the Visual Studio solution and simplifies its usage.
    /// </summary>
    internal class SolutionHelper
    {
        private const string ProjectJsonName = "project.json";

        private readonly Solution _solution;

        /// <summary>
        /// Returns the current solution open, null if no solution is present.
        /// </summary>
        public static SolutionHelper CurrentSolution
        {
            get
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                var solution = dte.Solution;
                return solution != null ? new SolutionHelper(solution) : null;
            }
        }

        internal static ServiceProvider GetGloblalServiceProvider()
        {
            var dte2 = (DTE2)Package.GetGlobalService(typeof(SDTE));
            VSOLEInterop.IServiceProvider sp = (VSOLEInterop.IServiceProvider)dte2;
            return new ServiceProvider(sp);
        }

        public List<ProjectHelper> Projects { get; } 
        private List<ProjectHelper> GetProjectList()
        {
            List<ProjectHelper> list = new List<ProjectHelper>();
            foreach (Project project in _solution.Projects)
            {
                var helper = ProjectHelper.Create(project);
                if (helper != null)
                {
                    list.Add(helper);
                }
            }

            return list;
        }

        public Solution solution => _solution;

        /// <summary>
        /// Returns the path to the root of the solution.
        /// </summary>
        public string SolutionDirectory => Path.GetDirectoryName(_solution.FullName);

        /// <summary>
        /// Returns the startup project for the current solution.
        /// </summary>
        public ISolutionProject StartupProject => GetStartupProject();

        /// <summary>
        /// Returns the selected project in the Solution Explorer.
        /// </summary>
        public ISolutionProject SelectedProject => GetSelectedProject();

        public List<ProjectSourceFile> FindMatchingSourceFile(string sourceLocationFilePath)
        {
            List<ProjectSourceFile> items = new List<ProjectSourceFile>();
            foreach (Project project in _solution.Projects)
            {
                foreach (ProjectItem projectItem in project.ProjectItems)
                {
                    NavigateProjectItem(projectItem, sourceLocationFilePath, items);
                }
            }

            return items;
        }

        public void NavigateProjectItem(ProjectItem projectItem, string sourceLocationFilePath, List<ProjectSourceFile> items)
        {
            if (ProjectSourceFile.DoesPathMatch(projectItem, sourceLocationFilePath))
            {
                items.Add(ProjectSourceFile.Create(projectItem));
            }

            foreach (ProjectItem nestedItem in projectItem.ProjectItems)
            {
                NavigateProjectItem(nestedItem, sourceLocationFilePath, items);
            }            
        }

        /// <summary>
        /// Returns the <seealso cref="SolutionBuild2"/> associated with the current solution.
        /// </summary>
        private SolutionBuild2 SolutionBuild => _solution.SolutionBuild as SolutionBuild2;

        private SolutionHelper(Solution solution)
        {
            _solution = solution;
            Projects = GetProjectList();
        }

        private ISolutionProject GetSelectedProject()
        {
            var selectedProjectDirectory = GetSelectedProjectDirectory();
            if (selectedProjectDirectory == null)
            {
                return null;
            }

            // .NET Core projects are not fully supported, only exist as a project.json file in a directory, check
            // if the file exists.
            var possibleProjectJson = Path.Combine(selectedProjectDirectory, ProjectJsonName);
            if (File.Exists(possibleProjectJson))
            {
                return new NetCoreProject(possibleProjectJson);
            }

            // The project is probably a full VS project, look it up.
            foreach (Project p in _solution.Projects)
            {
                var projectDirectory = Path.GetDirectoryName(p.FullName);
                if (projectDirectory.Equals(selectedProjectDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    return new VsProject(p);
                }
            }

            // Failed to determine the project.
            return null;
        }

        /// <summary>
        /// Uses COM interop to find out what is the selected node in the Solution Explorer and from that what
        /// is the path to the directory that contains it.
        /// Note: This method does not guarantee that the selected node is a projet, the caller should ensure that
        /// it is called in a context in which it will.
        /// </summary>
        /// <returns>The path to the directory that contains the project.</returns>
        private static string GetSelectedProjectDirectory()
        {
            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return null;
            }

            IVsMultiItemSelect select = null;
            uint itemid = 0;
            IntPtr hierarchyPtr = IntPtr.Zero;
            IntPtr selectionContainerPtr = IntPtr.Zero;

            try
            {
                var hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out select, out selectionContainerPtr);
                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    return null;
                }

                var hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null)
                {
                    return null;
                }

                object result = null;
                hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectDir, out result);
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

        private ISolutionProject GetStartupProject()
        {
            var sb = SolutionBuild;
            if (sb == null)
            {
                return null;
            }

            var startupProjects = sb.StartupProjects as Array;
            if (startupProjects == null)
            {
                return null;
            }

            string startupProjectFilePath = startupProjects.GetValue(0) as string;
            if (startupProjectFilePath == null)
            {
                return null;
            }

            // For .NET Core the file will have an .xproj extension.
            if (Path.GetExtension(startupProjectFilePath) == ".xproj")
            {
                var projectDirectory = Path.Combine(SolutionDirectory, Path.GetDirectoryName(startupProjectFilePath));
                var projectJsonPath = Path.Combine(projectDirectory, ProjectJsonName);
                if (File.Exists(projectJsonPath))
                {
                    return new NetCoreProject(projectJsonPath);
                }
                else
                {
                    // We have an invalid project.
                    Debug.WriteLine($"Invalid project found, no project.json exists: {startupProjectFilePath}");
                    return null;
                }
            }

            // Other extensions are VS projects.
            string projectName = Path.GetFileNameWithoutExtension(startupProjectFilePath);
            foreach (Project p in _solution.Projects)
            {
                if (p.Name == projectName)
                {
                    return new VsProject(p);
                }
            }

            // The project could not be found.
            return null;
        }
    }
}
