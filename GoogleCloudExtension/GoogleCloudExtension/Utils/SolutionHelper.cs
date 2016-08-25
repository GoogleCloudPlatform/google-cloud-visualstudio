﻿// Copyright 2015 Google Inc. All Rights Reserved.
// Licensed under the Apache License Version 2.0.

using EnvDTE;
using EnvDTE80;
using GoogleCloudExtension.DnxSupport;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;

namespace GoogleCloudExtension.Projects
{
    /// <summary>
    /// This class wraps the Visual Studio solution and provices methods to interact
    /// with the DNX project system underneath.
    /// </summary>
    internal class SolutionHelper
    {
        private readonly Solution _solution;
        private readonly DnxSolution _dnxSolution;

        internal SolutionHelper(Solution solution)
        {
            _solution = solution;
            _dnxSolution = new DnxSolution(solution.FullName);
        }

        public static SolutionHelper CurrentSolution
        {
            get
            {
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                return new SolutionHelper(dte.Solution);
            }
        }
        public DnxProject StartupProject => GetStartupProject();

        public IList<DnxProject> Projects => _dnxSolution.GetProjects();

        private SolutionBuild2 SolutionBuild => _solution.SolutionBuild as SolutionBuild2;

        public string Root => _solution.FullName;

        private DnxProject GetStartupProject()
        {
            var sb = this.SolutionBuild;
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

            string startupProjectName = startupProjects.GetValue(0) as string;
            if (startupProjectName == null)
            {
                ActivityLogUtils.LogInfo("No startup project name.");
                return null;
            }

            try
            {
                return _dnxSolution.GetProjectFromName(startupProjectName);
            }
            catch (Exception ex)
            {
                ActivityLogUtils.LogError($"Failed to get startup project: {ex.Message}");
                return null;
            }
        }
    }
}
