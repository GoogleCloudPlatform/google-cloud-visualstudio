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
    public class ProjectTemplatesTests
    {
        private const string SolutionName = "TestSolution";
        private const string SolutionFileName = SolutionName + ".sln";
#if VS2017
        private const string VsVersion = "15.0";
#elif VS2019
        private const string VsVersion = "16.0";
#else
        private const string VsVersion = "15.0";
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


        [ClassInitialize]
        public static void InitClass(TestContext context)
        {
            CreateSolutionDirectory();
            s_visualStudio = VisualStudioWrapper.CreateExperimentalInstance(VsVersion);
            Solution.Create(SolutionFolderPath, SolutionFileName);
            Solution.SaveAs(Path.Combine(SolutionFolderPath, SolutionFileName));
        }

        [ClassCleanup]
        public static void CleanupClass() => s_visualStudio.Dispose();

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
        [DataRow("4", "Mvc")]
        [DataRow("4", "WebApi")]
        public void TestCompileAspNet(string version, string appType)
        {
            string projectName = $"TestGcpAspNet{appType}";
            CreateProjectFromTemplate(projectName, "NetFramework", "Gcp.AspNet.vstemplate", version, appType);
            RestorePackages(Path.Combine(SolutionFolderPath, projectName));
            Solution.SolutionBuild.Build(true);

            Assert.AreEqual(vsBuildState.vsBuildStateDone, Solution.SolutionBuild.BuildState, projectName);
            Assert.AreEqual(0, Solution.SolutionBuild.LastBuildInfo,
                $"{projectName} build output:{Environment.NewLine}{GetBuildOutput()}");
            string descriptions = string.Join(
                Environment.NewLine, ErrorItems.Select(e => e.Project + ":" + e.Description));
            Assert.AreEqual(0, ErrorItems.Count(e => e.Project == projectName),
                $"{projectName} error descriptions:{Environment.NewLine}{descriptions}");
        }

        [TestMethod]
        [DataRow("1.0", "Mvc")]
        [DataRow("1.0", "WebApi")]
        [DataRow("1.1", "Mvc")]
        [DataRow("1.1", "WebApi")]
        [DataRow("2.0", "Mvc")]
        [DataRow("2.0", "WebApi")]
        [DataRow("2.1", "Mvc")]
        [DataRow("2.1", "WebApi")]
        public void TestCompileAspNetCore(string version, string appType)
        {
            string projectName = $"TestGcpAspNetCore{appType}{version.Replace(".", "").Replace("-", "")}";
            CreateProjectFromTemplate(projectName, "NetCore", "Gcp.AspNetCore.vstemplate", version, appType);
            RestorePackages(Path.Combine(SolutionFolderPath, projectName));
            Solution.SolutionBuild.Build(true);

            Assert.AreEqual(vsBuildState.vsBuildStateDone, Solution.SolutionBuild.BuildState, projectName);
            Assert.AreEqual(
                0, Solution.SolutionBuild.LastBuildInfo,
                $"{projectName} build output:{Environment.NewLine}{GetBuildOutput()}");
            string descriptions = string.Join(
                Environment.NewLine, ErrorItems.Select(e => e.Project + ":" + e.Description));
            Assert.AreEqual(
                0, ErrorItems.Count(e => e.Project == $"Test{projectName}Project"),
                $"{projectName} error descriptions:{Environment.NewLine}{descriptions}");
        }

        private static void RestorePackages(string projectDirectory)
        {
            var dotNetRestoreInfo = new ProcessStartInfo("dotnet", "restore")
            {
                WorkingDirectory = projectDirectory,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            Process.Start(dotNetRestoreInfo)?.WaitForExit();
        }

        private static void CreateSolutionDirectory()
        {
            if (Directory.Exists(SolutionFolderPath))
            {
                while (Directory.EnumerateFileSystemEntries(SolutionFolderPath).Any())
                {
                    foreach (string directory in Directory.EnumerateDirectories(SolutionFolderPath))
                    {
                        Directory.Delete(directory, true);
                    }

                    foreach (string file in Directory.EnumerateFiles(SolutionFolderPath))
                    {
                        File.Delete(file);
                    }
                }
            }
            else
            {
                Directory.CreateDirectory(SolutionFolderPath);
            }
        }

        private static string GetBuildOutput()
        {
            OutputWindowPane buildWindow = Dte.ToolWindows.OutputWindow.OutputWindowPanes.Item("Build");
            TextSelection buildSelection = buildWindow.TextDocument.Selection;
            buildSelection.SelectAll();
            return buildSelection.Text;
        }

        private void CreateProjectFromTemplate(
            string projectName,
            string framework,
            string chooserTemplateName,
            string version,
            string appType)
        {
            string projectPath = Path.Combine(SolutionFolderPath, projectName);
            Directory.CreateDirectory(projectPath);
            string templatePath = Solution.GetProjectTemplate(chooserTemplateName, "CSharp");
            var serviceProvider = Dte as IServiceProvider;
            var vsSolution6 = (IVsSolution6)serviceProvider.QueryService<SVsSolution>();
            var resultObject = new JObject
            {
                ["GcpProjectId"] = "fake-gcp-project-id",
                ["SelectedFramework"] = framework,
                ["AppType"] = appType,
                ["SelectedVersion"] = new JObject { ["Version"] = version }
            };
            Array customParams = new object[]
            {
                $"$templateChooserResult$={resultObject}"
            };
            int hResult = vsSolution6.AddNewProjectFromTemplate(
                templatePath,
                customParams,
                "",
                projectPath,
                projectName,
                null,
                out _);
            ErrorHandler.ThrowOnFailure(hResult, -2147221492);
            Project project = Solution.Projects.OfType<Project>().First(p => p.Name == projectName);
            project.Save();
        }
    }
}
