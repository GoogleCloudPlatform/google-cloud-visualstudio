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
using GoogleCloudExtension.TemplateWizards;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace GoogleCloudExtensionUnitTests.TemplateWizards
{
    /// <summary>
    /// Class for testing <see cref="GoogleProjectTemplateWizard"/>
    /// </summary>
    [TestClass]
    public class GoogleProjectTemplateWizardTests
    {
        private const string ProjectDirectoryBackslash = @"root:\solution\dir\project\dir";
        private const string ProjectDirectoryBackslashEnd = ProjectDirectoryBackslash + @"\";
        private const string SolutionDirectoryBackslash = @"root:\solution\dir";
        private const string SolutionDirectoryBackslashEnd = SolutionDirectoryBackslash + @"\";
        private const string ProjectDirectorySlash = "root:/solution/dir/project/dir";
        private const string ProjectDirectorySlashEnd = ProjectDirectorySlash + "/";
        private const string SolutionDirectorySlash = "root:/solution/dir";
        private const string SolutionDirectorySlashEnd = SolutionDirectorySlash + "/";
        private const string MockProjectId = "mock-project-id";
        private const string PackagesPath = @"..\..\packages\";
        private const string RandomFileName = "random.file.name";
        private const string ProjectName = "ProjectName";

        private static readonly string[] s_projectDirectoriesToTest =
            {ProjectDirectoryBackslash, ProjectDirectoryBackslashEnd, ProjectDirectorySlash, ProjectDirectorySlashEnd};

        private static readonly string[] s_solutionDirectoriesToTest =
        {
            SolutionDirectoryBackslash,
            SolutionDirectoryBackslashEnd,
            SolutionDirectorySlash,
            SolutionDirectorySlashEnd
        };

        private GoogleProjectTemplateWizard _objectUnderTest;
        private Mock<Action<Dictionary<string, string>>> _cleanupDirectoriesMock;
        private Mock<Func<string, string>> _pickProjectMock;
        private Dictionary<string, string> _replacementsDictionary;
        private DTE _mockedDte;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _mockedDte = Mock.Of<DTE>(dte => dte.CommandLineArguments == "");
            _pickProjectMock = new Mock<Func<string, string>>();
            _cleanupDirectoriesMock = new Mock<Action<Dictionary<string, string>>>();
            _objectUnderTest =
                new GoogleProjectTemplateWizard
                {
                    PromptPickProjectId = _pickProjectMock.Object,
                    CleanupDirectories = _cleanupDirectoriesMock.Object
                };
            _replacementsDictionary = new Dictionary<string, string>
            {
                {ReplacementsKeys.DestinationDirectoryKey, ProjectDirectoryBackslash},
                {ReplacementsKeys.SolutionDirectoryKey, SolutionDirectoryBackslash},
                {ReplacementsKeys.ProjectNameKey, ProjectName}
            };
        }

        [TestMethod]
        [ExpectedException(typeof(WizardBackoutException))]
        public void TestRunStartedCanceled()
        {
            _pickProjectMock.Setup(x => x(It.IsAny<string>())).Returns(() => null);
            _replacementsDictionary.Add(ReplacementsKeys.ExclusiveProjectKey, bool.FalseString);

            try
            {
                _objectUnderTest.RunStarted(
                    _mockedDte,
                    _replacementsDictionary,
                    WizardRunKind.AsNewProject,
                    new object[0]);
                Assert.Fail();
            }
            finally
            {
                _cleanupDirectoriesMock.Verify(f => f(_replacementsDictionary), Times.Once);
                _cleanupDirectoriesMock.Verify(f => f(It.IsNotIn(_replacementsDictionary)), Times.Never);
            }
        }

        [TestMethod]
        public void TestRunStartedPickProjectSkipped()
        {
            _pickProjectMock.Setup(x => x(It.IsAny<string>())).Returns(() => string.Empty);
            foreach (string projectDir in s_projectDirectoriesToTest)
            {
                foreach (string solutionDir in s_solutionDirectoriesToTest)
                {
                    string message = $"For test case\nprojectDir: {projectDir}\nsolutionDir: {solutionDir}";
                    _cleanupDirectoriesMock.ResetCalls();
                    _replacementsDictionary = new Dictionary<string, string>
                    {
                        {ReplacementsKeys.DestinationDirectoryKey, projectDir},
                        {ReplacementsKeys.SolutionDirectoryKey, solutionDir},
                        {ReplacementsKeys.ProjectNameKey, ProjectName},
                        {ReplacementsKeys.SafeProjectNameKey, ProjectName}
                    };

                    _objectUnderTest.RunStarted(
                        _mockedDte,
                        _replacementsDictionary,
                        WizardRunKind.AsNewProject,
                        new object[0]);

                    _cleanupDirectoriesMock.Verify(
                        f => f(It.IsAny<Dictionary<string, string>>()), Times.Never, message);
                    Assert.IsTrue(_replacementsDictionary.ContainsKey(ReplacementsKeys.GcpProjectIdKey), message);
                    Assert.AreEqual(string.Empty, _replacementsDictionary[ReplacementsKeys.GcpProjectIdKey], message);
                    Assert.IsTrue(_replacementsDictionary.ContainsKey(ReplacementsKeys.PackagesPathKey), message);
                    Assert.AreEqual(PackagesPath, _replacementsDictionary[ReplacementsKeys.PackagesPathKey], message);
                }
            }
        }

        [TestMethod]
        public void TestRunStartedSuccess()
        {
            _pickProjectMock.Setup(x => x(It.IsAny<string>())).Returns(() => MockProjectId);
            _replacementsDictionary.Add(ReplacementsKeys.SafeProjectNameKey, ProjectName);

            _objectUnderTest.RunStarted(
                _mockedDte,
                _replacementsDictionary,
                WizardRunKind.AsNewProject,
                new object[0]);

            _cleanupDirectoriesMock.Verify(f => f(It.IsAny<Dictionary<string, string>>()), Times.Never);
            Assert.IsTrue(_replacementsDictionary.ContainsKey(ReplacementsKeys.GcpProjectIdKey));
            Assert.AreEqual(MockProjectId, _replacementsDictionary[ReplacementsKeys.GcpProjectIdKey]);
            Assert.IsTrue(_replacementsDictionary.ContainsKey(ReplacementsKeys.PackagesPathKey));
            Assert.AreEqual(PackagesPath, _replacementsDictionary[ReplacementsKeys.PackagesPathKey]);
            Assert.AreEqual(ProjectName, _replacementsDictionary[ReplacementsKeys.EmbeddableSafeProjectNameKey]);
        }

        [TestMethod]
        public void TestShouldAddProjectItem()
        {
            bool result = _objectUnderTest.ShouldAddProjectItem(RandomFileName);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void RunFinished()
        {
            _objectUnderTest.RunFinished();
        }

        [TestMethod]
        public void TestBeforeOpeningFile()
        {
            _objectUnderTest.BeforeOpeningFile(new Mock<ProjectItem>(MockBehavior.Strict).Object);
        }

        [TestMethod]
        public void TestProjectItemFinishedGenerating()
        {
            _objectUnderTest.ProjectItemFinishedGenerating(new Mock<ProjectItem>(MockBehavior.Strict).Object);
        }

        [TestMethod]
        public void TestProjectFinishedGenerating()
        {
            _objectUnderTest.ProjectFinishedGenerating(new Mock<Project>(MockBehavior.Strict).Object);
        }
    }
}
