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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace ProjectTemplate.Tests
{
    [TestClass]
    public class ProjectTemplatesTests
    {
        private const string SolutionName = "TestSolution";
        private const string SolutionFileName = SolutionName + ".sln";

        private static VisualStudioWrapper s_visualStudio;

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public TestContext TestContext { get; set; }
        private static DTE2 Dte => s_visualStudio.Dte;
        private static string SolutionFolderPath => Path.Combine(Path.GetTempPath(), SolutionName);
        private static string ProjectTemplateDirectoryPath => Path.GetFullPath(@"..\..\..");

        private string TemplateName { get; set; }
        private string ProjectName => $"Test{TemplateName}Project";
        private string ProjectPath => Path.Combine(SolutionFolderPath, ProjectName);

        private string TemplatePath =>
            Path.Combine(ProjectTemplateDirectoryPath, $@"{TemplateName}\{TemplateName}.vstemplate");

        [ClassInitialize]
        public static void InitClass(TestContext context)
        {
            s_visualStudio = VisualStudioWrapper.CreateExperimentalInstance();
            CreateSolutionDirectory();
            Dte.Solution.Create(SolutionFolderPath, SolutionFileName);
            Dte.Solution.SaveAs(Path.Combine(SolutionFolderPath, SolutionFileName));
        }

        private static void CreateSolutionDirectory()
        {
            if (Directory.Exists(SolutionFolderPath))
            {
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
            }
            while (Directory.Exists(SolutionFolderPath))
            {
                // Allow Directory.Delete to finish before creating.
                Thread.Sleep(200);
            }
            Directory.CreateDirectory(SolutionFolderPath);
        }

        private static void KillMsBuild()
        {
            Process[] msBuildProcesses = Process.GetProcessesByName("MSBuild");
            var killedProcesses = new List<Process>();
            foreach (var process in msBuildProcesses)
            {
                killedProcesses.AddRange(process.KillProcessTree());
            }
            foreach (var process in killedProcesses)
            {
                process.WaitForExit();
            }
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
            foreach (var project in Dte.Solution.Projects.OfType<Project>())
            {
                if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
                {
                    TestContext.WriteLine(project.FullName);
                }
                Dte.Solution.Remove(project);
            }
        }

        [TestMethod]
        public void TestGoogleAspnetWebApi()
        {
            TemplateName = "GoogleAspnetWebApi";

            CreateProjectFromTemplate();
            Dte.Solution.SolutionBuild.Build(true);

            Assert.AreEqual(vsBuildState.vsBuildStateDone, Dte.Solution.SolutionBuild.BuildState);
            string descriptions = GetErrorDescriptions();
            Assert.AreEqual(0, Dte.ToolWindows.ErrorList.ErrorItems.Count, $"Error descriptions:{descriptions}");
        }

        [TestMethod]
        public void TestGoogleAspnetCoreWebApi()
        {
            TemplateName = "GoogleAspNetCoreWebApi";

            CreateProjectFromTemplate();
            Dte.Solution.SolutionBuild.Build(true);

            Assert.AreEqual(vsBuildState.vsBuildStateDone, Dte.Solution.SolutionBuild.BuildState);
            string descriptions = GetErrorDescriptions();
            Assert.AreEqual(0, Dte.ToolWindows.ErrorList.ErrorItems.Count, $"Error descriptions:{descriptions}");
        }

        [TestMethod]
        public void TestGoogleAspnetMvc()
        {
            TemplateName = "GoogleAspnetMvc";

            CreateProjectFromTemplate();
            Dte.Solution.SolutionBuild.Build(true);

            Assert.AreEqual(vsBuildState.vsBuildStateDone, Dte.Solution.SolutionBuild.BuildState);
            string descriptions = GetErrorDescriptions();
            Assert.AreEqual(0, Dte.ToolWindows.ErrorList.ErrorItems.Count, $"Error descriptions:{descriptions}");
        }

        [TestMethod]
        public void TestGoogleAspnetCoreMvc()
        {
            TemplateName = "GoogleAspNetCoreMvc";

            CreateProjectFromTemplate();
            Dte.Solution.SolutionBuild.Build(true);

            Assert.AreEqual(vsBuildState.vsBuildStateDone, Dte.Solution.SolutionBuild.BuildState);
            string descriptions = GetErrorDescriptions();
            Assert.AreEqual(0, Dte.ToolWindows.ErrorList.ErrorItems.Count, $"Error descriptions:\n{descriptions}");
        }

        private string GetErrorDescriptions()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < Dte.ToolWindows.ErrorList.ErrorItems.Count; i++)
            {
                try
                {
                    builder.AppendLine(Dte.ToolWindows.ErrorList.ErrorItems.Item(i).Description);
                }
                // ErrorItems.Count sometimes is out of sync with the underlying collection.
                // Catching this exception seems to get it back in sync when the actual assert occurs.
                catch (IndexOutOfRangeException e)
                {
                    Debug.WriteLine(e);
                }
            }
            return builder.ToString();
        }

        private void CreateProjectFromTemplate()
        {
            Directory.CreateDirectory(ProjectPath);
            Dte.Solution.AddFromTemplate(TemplatePath, ProjectPath, ProjectName);
            Project project = Dte.Solution.Projects.OfType<Project>().First(p => p.Name == ProjectName);
            project.Save();
        }
    }
}
