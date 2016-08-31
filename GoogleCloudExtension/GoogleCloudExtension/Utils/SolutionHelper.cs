using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class wraps the Visual Studio solution.
    /// </summary>
    internal class SolutionHelper
    {
        private readonly Solution _solution;

        internal SolutionHelper(Solution solution)
        {
            _solution = solution;
        }

        public static SolutionHelper CurrentSolution
        {
            get
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                return new SolutionHelper(dte.Solution);
            }
        }

        private SolutionBuild2 SolutionBuild => _solution.SolutionBuild as SolutionBuild2;

        public string Root => _solution.FullName;

        public Project StartupProject => GetStartupProject();

        public Project SelectedProject => GetSelectedProject();

        public void PublishProject(Project project)
        {
            SolutionBuild.PublishProject("Release", project.UniqueName, WaitForPublishToFinish: false);
        }

        private Project GetSelectedProject()
        {
            var selectedProjectDirectory = GetSelectedProjectDirectory();
            if (selectedProjectDirectory == null)
            {
                return null;
            }

            foreach (Project p in _solution.Projects)
            {
                var projectDirectory = Path.GetDirectoryName(p.FullName);
                if (projectDirectory.Equals(selectedProjectDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
            return null;
        }

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

        private Project GetStartupProject()
        {
            var sb = SolutionBuild;
            if (sb == null)
            {
                ActivityLogUtils.LogInfo("No startup project, no solution loaded yet.");
                return null;
            }

            var startupProjects = sb.StartupProjects as Array;
            if (startupProjects == null)
            {
                ActivityLogUtils.LogInfo("No startup projects in solution.");
                return null;
            }

            string startupProjectFilePath = startupProjects.GetValue(0) as string;
            if (startupProjectFilePath == null)
            {
                ActivityLogUtils.LogInfo("No startup project name.");
                return null;
            }

            string projectName = Path.GetFileNameWithoutExtension(startupProjectFilePath);
            foreach (Project p in _solution.Projects)
            {
                if (p.Name == projectName)
                {
                    return p;
                }
            }
            return null;
        }
    }
}
