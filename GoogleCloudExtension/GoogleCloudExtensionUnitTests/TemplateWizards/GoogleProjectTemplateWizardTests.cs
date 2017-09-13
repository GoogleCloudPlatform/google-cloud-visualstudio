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
        private const string DestinationDirectoryKey = "$destinationdirectory$";
        private const string ExclusiveProjectKey = "$exclusiveproject$";
        private const string SolutionDirectoryKey = "$solutiondirectory$";
        private const string MockProjectId = "mock-project-id";
        private const string GcpProjectIdKey = "$gcpprojectid$";
        private const string PackagesPathKey = "$packagespath$";
        private const string PackagesPath = @"..\..\packages\";
        private const string RandomFileName = "random.file.name";

        private static readonly string[] s_projectDirectoriesToTest =
            {ProjectDirectoryBackslash, ProjectDirectoryBackslashEnd, ProjectDirectorySlash, ProjectDirectorySlashEnd};

        private static readonly string[] s_solutionDirectoriesToTest =
            {SolutionDirectoryBackslash, SolutionDirectoryBackslashEnd, SolutionDirectorySlash, SolutionDirectorySlashEnd};

        private GoogleProjectTemplateWizard _objectUnderTest;
        private Mock<Action<Dictionary<string, string>>> _cleanupDirectoriesMock;
        private Mock<Func<string>> _pickProjectMock;
        private Dictionary<string, string> _replacementsDictionary;
        private DTE _mockedDte;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _mockedDte = Mock.Of<DTE>(dte => dte.CommandLineArguments == "");
            _pickProjectMock = new Mock<Func<string>>();
            _cleanupDirectoriesMock = new Mock<Action<Dictionary<string, string>>>();
            _objectUnderTest =
                new GoogleProjectTemplateWizard
                {
                    PromptPickProjectId = _pickProjectMock.Object,
                    CleanupDirectories = _cleanupDirectoriesMock.Object
                };
            _replacementsDictionary = new Dictionary<string, string>
            {
                {DestinationDirectoryKey, ProjectDirectoryBackslash},
                {SolutionDirectoryKey, SolutionDirectoryBackslash}
            };
        }

        [TestMethod]
        [ExpectedException(typeof(WizardBackoutException))]
        public void TestRunStartedCanceled()
        {
            _pickProjectMock.Setup(x => x()).Returns(() => null);
            _replacementsDictionary.Add(ExclusiveProjectKey, bool.FalseString);

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
            _pickProjectMock.Setup(x => x()).Returns(() => string.Empty);
            foreach (string projectDir in s_projectDirectoriesToTest)
            {
                foreach (string solutionDir in s_solutionDirectoriesToTest)
                {
                    string message = $"For test case\nprojectDir: {projectDir}\nsolutionDir: {solutionDir}";
                    _cleanupDirectoriesMock.ResetCalls();
                    _replacementsDictionary = new Dictionary<string, string>
                    {
                        {DestinationDirectoryKey, projectDir},
                        {SolutionDirectoryKey, solutionDir}
                    };

                    _objectUnderTest.RunStarted(
                        _mockedDte,
                        _replacementsDictionary,
                        WizardRunKind.AsNewProject,
                        new object[0]);

                    _cleanupDirectoriesMock.Verify(f => f(It.IsAny<Dictionary<string, string>>()), Times.Never, message);
                    Assert.IsTrue(_replacementsDictionary.ContainsKey(GcpProjectIdKey), message);
                    Assert.AreEqual(string.Empty, _replacementsDictionary[GcpProjectIdKey], message);
                    Assert.IsTrue(_replacementsDictionary.ContainsKey(PackagesPathKey), message);
                    Assert.AreEqual(PackagesPath, _replacementsDictionary[PackagesPathKey], message);
                }
            }
        }

        [TestMethod]
        public void TestRunStartedSuccess()
        {
            _pickProjectMock.Setup(x => x()).Returns(() => MockProjectId);

            _objectUnderTest.RunStarted(
                _mockedDte,
                _replacementsDictionary,
                WizardRunKind.AsNewProject,
                new object[0]);

            _cleanupDirectoriesMock.Verify(f => f(It.IsAny<Dictionary<string, string>>()), Times.Never);
            Assert.IsTrue(_replacementsDictionary.ContainsKey(GcpProjectIdKey));
            Assert.AreEqual(MockProjectId, _replacementsDictionary[GcpProjectIdKey]);
            Assert.IsTrue(_replacementsDictionary.ContainsKey(PackagesPathKey));
            Assert.AreEqual(PackagesPath, _replacementsDictionary[PackagesPathKey]);
        }

        [TestMethod]
        public void TestShouldAddProjectItem()
        {
            bool result = _objectUnderTest.ShouldAddProjectItem(RandomFileName);
            Assert.IsTrue(result);
        }
    }
}
