using EnvDTE;
using EnvDTE80;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;

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
