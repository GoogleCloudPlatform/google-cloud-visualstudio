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
        private const string ProjectDirectory = "root:\\solution\\dir\\project\\dir";
        private const string SolutionDirectory = "root:\\solution\\dir";
        private const string ProjectDirectoryUnix = "root:/solution/dir/project/dir";
        private const string SolutionDirectoryUnix = "root:/solution/dir";
        private const string DestinationDirectoryKey = "$destinationdirectory$";
        private const string ExclusiveProjectKey = "$exclusiveproject$";
        private const string SolutionDirectoryKey = "$solutiondirectory$";
        private const string MockProjectId = "mock-project-id";
        private const string GcpProjectIdKey = "$gcpprojectid$";
        private const string PackagesPathKey = "$packagespath$";
        private const string PackagesPath = "..\\..\\packages\\";
        private const string RandomFileName = "random.file.name";
        private const string GlobalJsonFileName = "global.json";
        private GoogleProjectTemplateWizard _objectUnderTest;
        private Mock<Action<string, bool>> _deleteDirectoryMock;
        private Mock<Func<string>> _pickProjectMock;
        private Dictionary<string, string> _replacementsDictionary;

        [TestInitialize]
        public void BeforeEachTest()
        {
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
                {DestinationDirectoryKey, ProjectDirectory},
                {SolutionDirectoryKey, SolutionDirectory}
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
                    Mock.Of<DTE>(),
                    _replacementsDictionary,
                    WizardRunKind.AsNewProject,
                    new object[0]);
                Assert.Fail();
            }
            finally
            {
                _deleteDirectoryMock.Verify(f => f(ProjectDirectory, true), Times.Once);
                _deleteDirectoryMock.Verify(f => f(ProjectDirectory, It.IsNotIn(true)), Times.Never);
                _deleteDirectoryMock.Verify(f => f(SolutionDirectory, true), Times.Once);
                _deleteDirectoryMock.Verify(f => f(SolutionDirectory, It.IsNotIn(true)), Times.Never);
                _deleteDirectoryMock.Verify(
                    f => f(It.IsNotIn(ProjectDirectory, SolutionDirectory), It.IsAny<bool>()),
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
                    Mock.Of<DTE>(),
                    _replacementsDictionary,
                    WizardRunKind.AsNewProject,
                    new object[0]);
                Assert.Fail();
            }
            finally
            {
                _deleteDirectoryMock.Verify(f => f(ProjectDirectory, true), Times.Once);
                _deleteDirectoryMock.Verify(f => f(ProjectDirectory, It.IsNotIn(true)), Times.Never);
                _deleteDirectoryMock.Verify(f => f(SolutionDirectory, It.IsAny<bool>()), Times.Never);
                _deleteDirectoryMock.Verify(
                    f => f(It.IsNotIn(ProjectDirectory, SolutionDirectory), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        [DataTestMethod]
        [DataRow(ProjectDirectory, SolutionDirectory)]
        [DataRow(ProjectDirectory, SolutionDirectoryUnix)]
        [DataRow(ProjectDirectoryUnix, SolutionDirectory)]
        [DataRow(ProjectDirectoryUnix, SolutionDirectoryUnix)]
        public void TestRunStartedPickProjectSkipped(string projectDir, string solutionDir)
        {
            _replacementsDictionary[DestinationDirectoryKey] = projectDir;
            _replacementsDictionary[SolutionDirectoryKey] = solutionDir;
            _pickProjectMock.Setup(x => x()).Returns(() => string.Empty);

            _objectUnderTest.RunStarted(
                Mock.Of<DTE>(),
                _replacementsDictionary,
                WizardRunKind.AsNewProject,
                new object[0]);

            _deleteDirectoryMock.Verify(f => f(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            Assert.IsTrue(_replacementsDictionary.ContainsKey(GcpProjectIdKey));
            Assert.AreEqual(string.Empty, _replacementsDictionary[GcpProjectIdKey]);
            Assert.IsTrue(_replacementsDictionary.ContainsKey(PackagesPathKey));
            Assert.AreEqual(PackagesPath, _replacementsDictionary[PackagesPathKey]);
        }

        [TestMethod]
        public void TestRunStartedSuccess()
        {
            _pickProjectMock.Setup(x => x()).Returns(() => MockProjectId);

            _objectUnderTest.RunStarted(
                Mock.Of<DTE>(),
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
