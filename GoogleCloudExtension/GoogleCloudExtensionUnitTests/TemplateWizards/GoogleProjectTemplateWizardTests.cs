using EnvDTE;
using GoogleCloudExtension.TemplateWizards;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace GoogleCloudExtensionUnitTests.TemplateWizards
{
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
        private const string GlobalJsonFileName = "global.json";

        private static readonly string[] s_projectDirectoriesToTest =
            {ProjectDirectoryBackslash, ProjectDirectoryBackslashEnd, ProjectDirectorySlash, ProjectDirectorySlashEnd};

        private static readonly string[] s_solutionDirectoriesToTest =
            {SolutionDirectoryBackslash, SolutionDirectoryBackslashEnd, SolutionDirectorySlash, SolutionDirectorySlashEnd};

        private GoogleProjectTemplateWizard _objectUnderTest;
        private Mock<Action<string, bool>> _deleteDirectoryMock;
        private Mock<Func<string>> _pickProjectMock;
        private Dictionary<string, string> _replacementsDictionary;
        private DTE _mockedDte;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _mockedDte = Mock.Of<DTE>(dte => dte.CommandLineArguments == "");
            _pickProjectMock = new Mock<Func<string>>();
            _deleteDirectoryMock = new Mock<Action<string, bool>>();
            _objectUnderTest =
                new GoogleProjectTemplateWizard
                {
                    PromptPickProjectId = _pickProjectMock.Object,
                    DeleteDirectory = _deleteDirectoryMock.Object
                };
            _replacementsDictionary = new Dictionary<string, string>
            {
                {DestinationDirectoryKey, ProjectDirectoryBackslash},
                {SolutionDirectoryKey, SolutionDirectoryBackslash}
            };
        }

        [TestMethod]
        [ExpectedException(typeof(WizardBackoutException))]
        public void TestRunStartedCanceledExclusive()
        {
            _pickProjectMock.Setup(x => x()).Returns(() => null);
            _replacementsDictionary.Add(ExclusiveProjectKey, bool.TrueString);

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
                _deleteDirectoryMock.Verify(f => f(ProjectDirectoryBackslash, true), Times.Once);
                _deleteDirectoryMock.Verify(f => f(ProjectDirectoryBackslash, It.IsNotIn(true)), Times.Never);
                _deleteDirectoryMock.Verify(f => f(SolutionDirectoryBackslash, true), Times.Once);
                _deleteDirectoryMock.Verify(f => f(SolutionDirectoryBackslash, It.IsNotIn(true)), Times.Never);
                _deleteDirectoryMock.Verify(
                    f => f(It.IsNotIn(ProjectDirectoryBackslash, SolutionDirectoryBackslash), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardBackoutException))]
        public void TestRunStartedCanceledNonExclusive()
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
                _deleteDirectoryMock.Verify(f => f(ProjectDirectoryBackslash, true), Times.Once);
                _deleteDirectoryMock.Verify(f => f(ProjectDirectoryBackslash, It.IsNotIn(true)), Times.Never);
                _deleteDirectoryMock.Verify(f => f(SolutionDirectoryBackslash, It.IsAny<bool>()), Times.Never);
                _deleteDirectoryMock.Verify(
                    f => f(It.IsNotIn(ProjectDirectoryBackslash, SolutionDirectoryBackslash), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        [TestMethod]
        public void TestRunStartedPickProjectSkipped()
        {
            _pickProjectMock.Setup(x => x()).Returns(() => string.Empty);
            foreach (var projectDir in s_projectDirectoriesToTest)
            {
                foreach (var solutionDir in s_solutionDirectoriesToTest)
                {
                    var message = $"For test case\nprojectDir: {projectDir}\nsolutionDir: {solutionDir}";
                    _deleteDirectoryMock.ResetCalls();
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

                    _deleteDirectoryMock.Verify(f => f(It.IsAny<string>(), It.IsAny<bool>()), Times.Never, message);
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

            _deleteDirectoryMock.Verify(f => f(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            Assert.IsTrue(_replacementsDictionary.ContainsKey(GcpProjectIdKey));
            Assert.AreEqual(MockProjectId, _replacementsDictionary[GcpProjectIdKey]);
            Assert.IsTrue(_replacementsDictionary.ContainsKey(PackagesPathKey));
            Assert.AreEqual(PackagesPath, _replacementsDictionary[PackagesPathKey]);
        }

        [TestMethod]
        public void TestShouldAddProjectItemRandomFile()
        {
            var result = _objectUnderTest.ShouldAddProjectItem(RandomFileName);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestShouldAddProjectItemGlobal2015()
        {
            GoogleCloudExtensionPackageTests.InitPackageMock(
                dteMock => dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2015Version));

            var result = _objectUnderTest.ShouldAddProjectItem(GlobalJsonFileName);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestShouldAddProjectItemGlobal2017()
        {
            GoogleCloudExtensionPackageTests.InitPackageMock(
                dteMock => dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2017Version));

            var result = _objectUnderTest.ShouldAddProjectItem(GlobalJsonFileName);
            Assert.IsFalse(result);
        }
    }
}
