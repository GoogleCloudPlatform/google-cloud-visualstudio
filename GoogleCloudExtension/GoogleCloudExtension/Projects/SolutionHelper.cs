// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using EnvDTE;
using EnvDTE80;
using GoogleCloudExtension.GCloud.Dnx.Models;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace GoogleCloudExtension.Projects
{
    /// <summary>
    /// This class wraps the Visual Studio solution and provices methods to interact
    /// with the DNX project system underneath.
    /// </summary>
    internal class SolutionHelper
    {
        private readonly Solution _solution;
        private readonly GCloud.Dnx.Solution _dnxSolution;

        internal SolutionHelper(Solution solution)
        {
            _solution = solution;
            _dnxSolution = new GCloud.Dnx.Solution(solution.FullName);
        }

        public static SolutionHelper CurrentSolution
        {
            get
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                return new SolutionHelper(dte.Solution);
            }
        }
        public GCloud.Dnx.Project StartupProject => GetStartupProject();

        public IList<GCloud.Dnx.Project> Projects => _dnxSolution.GetProjects();

        private SolutionBuild2 SolutionBuild => _solution.SolutionBuild as SolutionBuild2;

        private GCloud.Dnx.Project GetStartupProject()
        {
            var sb = this.SolutionBuild;
            if (sb == null)
            {
                Debug.WriteLine("No startup project, no solution loaded yet.");
                return null;
            }

            var startupProjects = sb.StartupProjects as Array;
            if (startupProjects == null)
            {
                Debug.WriteLine("No startup projects in solution.");
                return null;
            }

            string startupProjectName = startupProjects.GetValue(0) as string;
            if (startupProjectName == null)
            {
                Debug.WriteLine("No startup project name.");
                return null;
            }

            try
            {
                return _dnxSolution.GetProjectFromName(startupProjectName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get startup project {ex.Message}");
                return null;
            }
        }
    }
}
