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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace ProjectTemplate.Tests
{
    [TestClass]
    [DeploymentItem("Templates.csv")]
    public class ProjectTemplatesTests
    {
        private const string SolutionName = "TestSolution";
        private const string SolutionFileName = SolutionName + ".sln";

        private static VisualStudioWrapper s_visualStudio;

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public TestContext TestContext { get; set; }
        private static DTE2 Dte => s_visualStudio.Dte;
        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        private static Solution2 Solution => (Solution2)Dte.Solution;
        private static string SolutionFolderPath => Path.Combine(Path.GetTempPath(), SolutionName);

        private string TemplateName { get; set; }
        private string ProjectName => $"Test{TemplateName}Project";
        private string ProjectPath => Path.Combine(SolutionFolderPath, ProjectName);

        [ClassInitialize]
        public static void InitClass(TestContext context)
        {
            s_visualStudio = VisualStudioWrapper.CreateExperimentalInstance();
            CreateSolutionDirectory();
            Solution.Create(SolutionFolderPath, SolutionFileName);
            Solution.SaveAs(Path.Combine(SolutionFolderPath, SolutionFileName));
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
            foreach (Process killedProcess in msBuildProcesses.SelectMany(p => p.KillProcessTree()).ToList())
            {
                killedProcess.WaitForExit();
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
            foreach (var project in Solution.Projects.OfType<Project>())
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
            @"|DataDirectory|\Templates.csv",
            "Templates#csv",
            DataAccessMethod.Random)]
        public void TestCompile()
        {
            TemplateName = Convert.ToString(TestContext.DataRow[0]);
            TestContext.WriteLine($"Testing template {TemplateName}");

            CreateProjectFromTemplate();
            Solution.SolutionBuild.Build(true);

            Assert.AreEqual(vsBuildState.vsBuildStateDone, Solution.SolutionBuild.BuildState, TemplateName);
            IList<ErrorItem> errors = GetErrors();
            string descriptions = string.Join("\n", errors.Select(e => e.Description));
            Assert.AreEqual(0, errors.Count, $"{TemplateName} error descriptions: {descriptions}");
        }

        private IList<ErrorItem> GetErrors()
        {
            var list = new List<ErrorItem>();
            for (int i = 0; i < Dte.ToolWindows.ErrorList.ErrorItems.Count; i++)
            {
                try
                {
                    list.Add(Dte.ToolWindows.ErrorList.ErrorItems.Item(i));
                }
                // ErrorItems.Count sometimes is out of sync with the underlying collection.
                catch (IndexOutOfRangeException e)
                {
                    TestContext.WriteLine("In template {0}: {1}", TemplateName, e);
                }
            }
            return list;
        }

        private void CreateProjectFromTemplate()
        {
            Directory.CreateDirectory(ProjectPath);
            string templatePath = Solution.GetProjectTemplate(TemplateName, "CSharp");
            Solution.AddFromTemplate(templatePath, ProjectPath, ProjectName);
            Project project = Solution.Projects.OfType<Project>().First(p => p.Name == ProjectName);
            project.Save();
        }
    }
}
