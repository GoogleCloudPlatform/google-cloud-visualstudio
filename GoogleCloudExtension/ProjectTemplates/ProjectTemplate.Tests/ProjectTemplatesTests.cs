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
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Process = System.Diagnostics.Process;

namespace ProjectTemplate.Tests
{
    /// <summary>
    /// This class tests project templates by creating a new visual studio experimental instance,
    /// creating new projects from the templates, and compiling them.
    /// </summary>
    [TestClass]
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    [DeploymentItem(TemplatesCsvFileName + ".csv")]
    public class ProjectTemplatesTests
    {
        private const string SolutionName = "TestSolution";
        private const string SolutionFileName = SolutionName + ".sln";
#if VS2015
        private const string TemplatesCsvFileName = "Templates2015";
#elif VS2017
        private const string TemplatesCsvFileName = "Templates2017";
#endif

        private static VisualStudioWrapper s_visualStudio;

        private static IEnumerable<ErrorItem> ErrorItems =>
            ((IEnumerable)Dte.ToolWindows.ErrorList.ErrorItems).Cast<ErrorItem>();
        private static DTE2 Dte => s_visualStudio.Dte;
        private static Solution2 Solution => (Solution2)Dte.Solution;
        private static string SolutionFolderPath => Path.Combine(Path.GetTempPath(), SolutionName);

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public TestContext TestContext { get; set; }

        private string TemplateName { get; set; }
        private string Framework { get; set; }
        private bool IsCoreTemplate { get; set; }
        private string Version { get; set; }
        private string AppType { get; set; }
        private string ProjectName => $"Test{TemplateName}Project";
        private string ProjectPath => Path.Combine(SolutionFolderPath, ProjectName);

        [ClassInitialize]
        public static void InitClass(TestContext context)
        {
            CreateSolutionDirectory();
            s_visualStudio = VisualStudioWrapper.CreateExperimentalInstance();
            Solution.Create(SolutionFolderPath, SolutionFileName);
            Solution.SaveAs(Path.Combine(SolutionFolderPath, SolutionFileName));
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            s_visualStudio.Dispose();
        }

        [TestInitialize]
        public void BeforeEach()
        {
        }

        [TestCleanup]
        public void AfterEach()
        {
            foreach (Project project in Solution.Projects.OfType<Project>())
            {
                if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
                {
                    TestContext.WriteLine(project.FullName);
                }
                Solution.Remove(project);
            }
        }

        [TestMethod]
        [DataSource(
            "Microsoft.VisualStudio.TestTools.DataSource.CSV",
            @"|DataDirectory|\" + TemplatesCsvFileName + ".csv",
            TemplatesCsvFileName + "#csv",
            DataAccessMethod.Sequential)]
        public void TestCompile()
        {
            TemplateName = Convert.ToString(TestContext.DataRow[0]);
            Framework = Convert.ToString(TestContext.DataRow[1]);
            IsCoreTemplate = Convert.ToBoolean(TestContext.DataRow[2]);
            Version = Convert.ToString(TestContext.DataRow[3]).Substring(1);
            AppType = Convert.ToString(TestContext.DataRow[4]);

            CreateProjectFromTemplate();
            if (IsCoreTemplate)
            {
                RestorePackages();
            }
            Solution.SolutionBuild.Build(true);

            Assert.AreEqual(vsBuildState.vsBuildStateDone, Solution.SolutionBuild.BuildState, TemplateName);
            Assert.AreEqual(0, Solution.SolutionBuild.LastBuildInfo,
                $"{TemplateName} build output:{Environment.NewLine}{GetBuildOutput()}");
            string descriptions = string.Join(
                Environment.NewLine, ErrorItems.Select(e => e.Project + ":" + e.Description));
            Assert.AreEqual(0, ErrorItems.Count(e => e.Project == ProjectName),
                $"{TemplateName} error descriptions:{Environment.NewLine}{descriptions}");
        }

        private void RestorePackages()
        {
            var dotNetRestoreInfo = new ProcessStartInfo("dotnet", "restore") { WorkingDirectory = ProjectPath };
            Process.Start(dotNetRestoreInfo)?.WaitForExit();
        }

        private static void CreateSolutionDirectory()
        {
            if (Directory.Exists(SolutionFolderPath))
            {
                var watcher = new FileSystemWatcher(SolutionFolderPath);
                try
                {
                    Directory.Delete(SolutionFolderPath, true);
                }
                catch (UnauthorizedAccessException)
                {
                    // MSBuild sometimes persists and holds a handle on a file in the solution package directory.
                    KillMsBuild();
                    Directory.Delete(SolutionFolderPath, true);
                }
                if (Directory.Exists(SolutionFolderPath))
                {
                    // Allow Directory.Delete to finish before creating.
                    try
                    {
                        WaitForChangedResult result = watcher.WaitForChanged(WatcherChangeTypes.Deleted, 1000);
                        if (result.TimedOut && Directory.Exists(SolutionFolderPath))
                        {
                            throw new TimeoutException($"Time out waiting for deletion of {SolutionFolderPath}");
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        // Directory was deleted between the "if(exists)" and the "watcher.WaitForChanged".
                    }
                }
            }
            Directory.CreateDirectory(SolutionFolderPath);
        }

        private static void KillMsBuild()
        {
            Process[] msBuildProcesses = Process.GetProcessesByName("MSBuild");
            foreach (Process msBuildProcess in msBuildProcesses)
            {
                msBuildProcess.KillProcessTreeAndWait(TimeSpan.FromSeconds(30));
            }
        }

        private static string GetBuildOutput()
        {
            OutputWindowPane buildWindow = Dte.ToolWindows.OutputWindow.OutputWindowPanes.Item("Build");
            TextSelection buildSelection = buildWindow.TextDocument.Selection;
            buildSelection.SelectAll();
            return buildSelection.Text;
        }

        private void CreateProjectFromTemplate()
        {
            Directory.CreateDirectory(ProjectPath);
            string templatePath = Solution.GetProjectTemplate("Gcp", "CSharp");
            var isp = Dte as IServiceProvider;
            var sp = new ServiceProvider(isp);
            var vsSolution = (IVsSolution6)sp.GetService(typeof(SVsSolution));
            var resultObject = new JObject(
                new JProperty("GcpProjectId", "test-project-id"),
                new JProperty("SelectedFramework", Framework),
                new JProperty("AppType", AppType),
                new JProperty("SelectedVersion", new JObject(
                    new JProperty("Version", Version),
                    new JProperty("IsCore", IsCoreTemplate)))
                );
            Array customParams = new object[]
            {
                $"$templateChooserResult$={resultObject}"
            };
            IVsHierarchy newProject;
            int hResult = vsSolution.AddNewProjectFromTemplate(
                templatePath,
                customParams,
                "",
                ProjectPath,
                ProjectName,
                null,
                out newProject);
            ErrorHandler.ThrowOnFailure(hResult, -2147221492);
            Project project = Solution.Projects.OfType<Project>().First(p => p.Name == ProjectName);
            project.Save();
        }
    }
}
