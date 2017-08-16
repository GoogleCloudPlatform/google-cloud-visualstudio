using EnvDTE;
using EnvDTE80;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.TemplateWizards;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

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

        private const string GlobalJsonFileName = GoogleProjectTemplateWizard.GlobalJsonFileName;
        private const string TargetFrameworkMoniker = GoogleProjectTemplateWizard.TargetFrameworkMoniker;
        private const string SupportedTargetFrameworkItemName =
            GoogleProjectTemplateWizard.SupportedTargetFrameworkItemName;

        private const string MockedProjectPath = @"c:\ProjectFullName.csproj";
        private const string Net451FrameworkName = ".Net Framework,Version=v4.5.1";
        private const string Net452FrameworkName = ".Net Framework,Version=v4.5.2";
        private const string Net46FrameworkName = ".Net Framework,Version=v4.6.0";
        private const string NetCore10FrameworkName = ".Net Core,Version=v1.0.0";
        private const string NetCore11FrameworkName = ".Net Core,Version=v1.1.0";
        private const string NetCore20FrameworkName = ".Net Core,Version=v2.0.0";

        private static readonly string[] s_projectDirectoriesToTest =
            {ProjectDirectoryBackslash, ProjectDirectoryBackslashEnd, ProjectDirectorySlash, ProjectDirectorySlashEnd};

        private static readonly string[] s_solutionDirectoriesToTest =
            {SolutionDirectoryBackslash, SolutionDirectoryBackslashEnd, SolutionDirectorySlash, SolutionDirectorySlashEnd};

        private GoogleProjectTemplateWizard _objectUnderTest;
        private Mock<Action<string, bool>> _deleteDirectoryMock;
        private Mock<Func<string>> _pickProjectMock;
        private Dictionary<string, string> _replacementsDictionary;
        private Mock<DTE> _dteMock;
        private Mock<IVsFrameworkMultiTargeting> _frameworkServiceMock;
        private Mock<IProjectParser> _projectParserMock;
        private Microsoft.Build.Evaluation.Project _buildProject;
        private Mock<Project> _projectMock;

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void BeforeEachTest()
        {
            _dteMock = new Mock<DTE2>().As<DTE>();
            _dteMock.Setup(dte => dte.CommandLineArguments).Returns("");
            _frameworkServiceMock = new Mock<IVsFrameworkMultiTargeting>();
            GoogleCloudExtensionPackageTests.SetupService<SVsFrameworkMultiTargeting, IVsFrameworkMultiTargeting>(
                _dteMock.As<IServiceProvider>(), _frameworkServiceMock);

            _pickProjectMock = new Mock<Func<string>>();
            _deleteDirectoryMock = new Mock<Action<string, bool>>();
            _projectParserMock = new Mock<IProjectParser>();

            _buildProject = new Microsoft.Build.Evaluation.Project();

            _objectUnderTest =
                new GoogleProjectTemplateWizard
                {
                    PromptPickProjectId = _pickProjectMock.Object,
                    DeleteDirectory = _deleteDirectoryMock.Object,
                    Parser = _projectParserMock.Object,
                    GetMsBuildProject = s => _buildProject
                };
            _replacementsDictionary = new Dictionary<string, string>
            {
                {DestinationDirectoryKey, ProjectDirectoryBackslash},
                {SolutionDirectoryKey, SolutionDirectoryBackslash}
            };
            _projectMock = new Mock<Project>();
        }

        [TestMethod]
        [ExpectedException(typeof(WizardBackoutException))]
        public void TestRunStartedCanceledExclusive()
        {
            _pickProjectMock.Setup(x => x()).Returns(() => null);
            _replacementsDictionary.Add(ExclusiveProjectKey, bool.TrueString);
            Directory.SetCurrentDirectory(TestContext.TestDeploymentDir);

            try
            {
                _objectUnderTest.RunStarted(
                    _dteMock.Object,
                    _replacementsDictionary,
                    WizardRunKind.AsNewProject,
                    new object[0]);
                Assert.Fail();
            }
            catch (WizardBackoutException)
            {
                _deleteDirectoryMock.Verify(f => f(ProjectDirectoryBackslash, true), Times.Once);
                _deleteDirectoryMock.Verify(f => f(ProjectDirectoryBackslash, It.IsNotIn(true)), Times.Never);
                _deleteDirectoryMock.Verify(f => f(SolutionDirectoryBackslash, true), Times.Once);
                _deleteDirectoryMock.Verify(f => f(SolutionDirectoryBackslash, It.IsNotIn(true)), Times.Never);
                _deleteDirectoryMock.Verify(
                    f => f(It.IsNotIn(ProjectDirectoryBackslash, SolutionDirectoryBackslash), It.IsAny<bool>()),
                    Times.Never);
                Assert.AreEqual(TestContext.TestDeploymentDir, Directory.GetCurrentDirectory());
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardBackoutException))]
        public void TestRunStartedCanceledNonExclusive()
        {
            _pickProjectMock.Setup(x => x()).Returns(() => null);
            _replacementsDictionary.Add(ExclusiveProjectKey, bool.FalseString);
            Directory.SetCurrentDirectory(TestContext.TestDeploymentDir);

            try
            {
                _objectUnderTest.RunStarted(
                    _dteMock.Object,
                    _replacementsDictionary,
                    WizardRunKind.AsNewProject,
                    new object[0]);
                Assert.Fail();
            }
            catch (WizardBackoutException)
            {
                _deleteDirectoryMock.Verify(f => f(ProjectDirectoryBackslash, true), Times.Once);
                _deleteDirectoryMock.Verify(f => f(ProjectDirectoryBackslash, It.IsNotIn(true)), Times.Never);
                _deleteDirectoryMock.Verify(f => f(SolutionDirectoryBackslash, It.IsAny<bool>()), Times.Never);
                _deleteDirectoryMock.Verify(
                    f => f(It.IsNotIn(ProjectDirectoryBackslash, SolutionDirectoryBackslash), It.IsAny<bool>()),
                    Times.Never);
                Assert.AreEqual(TestContext.TestDeploymentDir, Directory.GetCurrentDirectory());
                throw;
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
                    Directory.SetCurrentDirectory(TestContext.TestDeploymentDir);
                    string message = $"For test case\nprojectDir: {projectDir}\nsolutionDir: {solutionDir}";
                    _deleteDirectoryMock.ResetCalls();
                    _replacementsDictionary = new Dictionary<string, string>
                    {
                        {DestinationDirectoryKey, projectDir},
                        {SolutionDirectoryKey, solutionDir}
                    };

                    _objectUnderTest.RunStarted(
                        _dteMock.Object,
                        _replacementsDictionary,
                        WizardRunKind.AsNewProject,
                        new object[0]);

                    _deleteDirectoryMock.Verify(f => f(It.IsAny<string>(), It.IsAny<bool>()), Times.Never, message);
                    Assert.IsTrue(_replacementsDictionary.ContainsKey(GcpProjectIdKey), message);
                    Assert.AreEqual(string.Empty, _replacementsDictionary[GcpProjectIdKey], message);
                    Assert.IsTrue(_replacementsDictionary.ContainsKey(PackagesPathKey), message);
                    Assert.AreEqual(PackagesPath, _replacementsDictionary[PackagesPathKey], message);
                    Assert.AreEqual(Path.GetTempPath(), Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);
                }
            }
        }

        [TestMethod]
        public void TestRunStartedSuccess()
        {
            Directory.SetCurrentDirectory(TestContext.TestDeploymentDir);
            _pickProjectMock.Setup(x => x()).Returns(() => MockProjectId);

            _objectUnderTest.RunStarted(
                _dteMock.Object,
                _replacementsDictionary,
                WizardRunKind.AsNewProject,
                new object[0]);

            _deleteDirectoryMock.Verify(f => f(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            Assert.IsTrue(_replacementsDictionary.ContainsKey(GcpProjectIdKey));
            Assert.AreEqual(MockProjectId, _replacementsDictionary[GcpProjectIdKey]);
            Assert.IsTrue(_replacementsDictionary.ContainsKey(PackagesPathKey));
            Assert.AreEqual(PackagesPath, _replacementsDictionary[PackagesPathKey]);
            Assert.AreEqual(Path.GetTempPath(), Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar);
        }

        [TestMethod]
        public void TestShouldAddProjectItemRandomFile()
        {
            bool result = _objectUnderTest.ShouldAddProjectItem(RandomFileName);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestShouldAddProjectItemGlobal2015()
        {
            _dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2015Version);

            GoogleCloudExtensionPackageTests.InitGlobalServiceProvider(_dteMock);
            bool result = _objectUnderTest.ShouldAddProjectItem(GlobalJsonFileName);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestShouldAddProjectItemGlobal2017()
        {
            _dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2017Version);

            GoogleCloudExtensionPackageTests.InitGlobalServiceProvider(_dteMock);
            bool result = _objectUnderTest.ShouldAddProjectItem(GlobalJsonFileName);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ProjectFinishedGeneratingVs2015DotNetCore()
        {
            _dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2015Version);
            _projectParserMock.Setup(p => p.ParseProject(It.IsAny<Project>())).Returns(
                Mock.Of<IParsedProject>(p => p.ProjectType == KnownProjectTypes.NetCoreWebApplication1_0));

            GoogleCloudExtensionPackageTests.InitGlobalServiceProvider(_dteMock);
            _objectUnderTest.ProjectFinishedGenerating(_projectMock.Object);

            _projectMock.VerifySet(
                p => p.Properties.Item(TargetFrameworkMoniker).Value = It.IsAny<object>(), Times.Never);
            _projectMock.Verify(p => p.Save(MockedProjectPath), Times.Never);
        }

        [TestMethod]
        public void ProjectFinishedGeneratingDotNetFrameworkUpdate()
        {
            _dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2015Version);
            _pickProjectMock.Setup(x => x()).Returns(() => MockProjectId);
            _projectParserMock.Setup(p => p.ParseProject(It.IsAny<Project>())).Returns(
                Mock.Of<IParsedProject>(p => p.ProjectType == KnownProjectTypes.WebApplication));
            // ReSharper disable once RedundantAssignment
            Array supportedFrameworks = new[]
            {
                Net451FrameworkName, Net452FrameworkName, Net46FrameworkName
            };
            _frameworkServiceMock.Setup(s => s.GetSupportedFrameworks(out supportedFrameworks)).Returns(VSConstants.S_OK);
            _projectMock.Setup(p => p.Properties.Item(TargetFrameworkMoniker).Value).Returns(Net452FrameworkName);
            _projectMock.Setup(p => p.FullName).Returns(MockedProjectPath);

            GoogleCloudExtensionPackageTests.InitGlobalServiceProvider(_dteMock);
            _objectUnderTest.RunStarted(
                _dteMock.Object, _replacementsDictionary, WizardRunKind.AsNewProject, new object[0]);
            _objectUnderTest.ProjectFinishedGenerating(_projectMock.Object);

            _projectMock.VerifySet(p => p.Properties.Item(TargetFrameworkMoniker).Value = Net46FrameworkName, Times.Once);
            _projectMock.Verify(p => p.Save(MockedProjectPath), Times.Once);
        }

        [TestMethod]
        public void ProjectFinishedGeneratingDotNetFrameworkKeep()
        {
            _dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2015Version);
            _pickProjectMock.Setup(x => x()).Returns(() => MockProjectId);
            _projectParserMock.Setup(p => p.ParseProject(It.IsAny<Project>())).Returns(
                Mock.Of<IParsedProject>(p => p.ProjectType == KnownProjectTypes.WebApplication));
            // ReSharper disable once RedundantAssignment
            Array supportedFrameworks = new string[0];
            _frameworkServiceMock.Setup(s => s.GetSupportedFrameworks(out supportedFrameworks)).Returns(VSConstants.S_OK);
            _projectMock.Setup(p => p.Properties.Item(TargetFrameworkMoniker).Value).Returns(Net452FrameworkName);
            _projectMock.Setup(p => p.FullName).Returns(MockedProjectPath);

            GoogleCloudExtensionPackageTests.InitGlobalServiceProvider(_dteMock);
            _objectUnderTest.RunStarted(
                _dteMock.Object, _replacementsDictionary, WizardRunKind.AsNewProject, new object[0]);
            _objectUnderTest.ProjectFinishedGenerating(_projectMock.Object);

            _projectMock.VerifySet(
                p => p.Properties.Item(TargetFrameworkMoniker).Value = It.IsAny<object>(), Times.Never);
            _projectMock.Verify(p => p.Save(MockedProjectPath), Times.Never);
        }

        [TestMethod]
        public void ProjectFinishedGeneratingVs2017DotNetCoreKeep()
        {
            _dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2017Version);
            _pickProjectMock.Setup(x => x()).Returns(() => MockProjectId);
            _projectParserMock.Setup(p => p.ParseProject(It.IsAny<Project>())).Returns(
                Mock.Of<IParsedProject>(p => p.ProjectType == KnownProjectTypes.NetCoreWebApplication1_0));
            _buildProject.AddItem(SupportedTargetFrameworkItemName, NetCore10FrameworkName);
            // ReSharper disable once RedundantAssignment
            Array supportedFrameworks = new[]
            {
                Net451FrameworkName, Net452FrameworkName, Net46FrameworkName
            };
            _frameworkServiceMock.Setup(s => s.GetSupportedFrameworks(out supportedFrameworks)).Returns(VSConstants.S_OK);
            _projectMock.Setup(p => p.Properties.Item(TargetFrameworkMoniker).Value).Returns(NetCore10FrameworkName);
            _projectMock.Setup(p => p.FullName).Returns(MockedProjectPath);

            GoogleCloudExtensionPackageTests.InitGlobalServiceProvider(_dteMock);
            _objectUnderTest.RunStarted(
                _dteMock.Object, _replacementsDictionary, WizardRunKind.AsNewProject, new object[0]);
            _objectUnderTest.ProjectFinishedGenerating(_projectMock.Object);

            _projectMock.VerifySet(
                p => p.Properties.Item(TargetFrameworkMoniker).Value = It.IsAny<string>(), Times.Never);
            _projectMock.Verify(p => p.Save(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void ProjectFinishedGeneratingVs2017DotNetCoreUpdate11()
        {
            _dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2017Version);
            _pickProjectMock.Setup(x => x()).Returns(() => MockProjectId);
            _projectParserMock.Setup(p => p.ParseProject(It.IsAny<Project>())).Returns(
                Mock.Of<IParsedProject>(p => p.ProjectType == KnownProjectTypes.NetCoreWebApplication1_0));
            _buildProject.AddItem(SupportedTargetFrameworkItemName, NetCore10FrameworkName);
            _buildProject.AddItem(SupportedTargetFrameworkItemName, NetCore11FrameworkName);
            // ReSharper disable once RedundantAssignment
            Array supportedFrameworks = new[]
            {
                Net451FrameworkName, Net452FrameworkName, Net46FrameworkName
            };
            _frameworkServiceMock.Setup(s => s.GetSupportedFrameworks(out supportedFrameworks)).Returns(VSConstants.S_OK);
            _projectMock.Setup(p => p.Properties.Item(TargetFrameworkMoniker).Value).Returns(NetCore10FrameworkName);
            _projectMock.Setup(p => p.FullName).Returns(MockedProjectPath);

            GoogleCloudExtensionPackageTests.InitGlobalServiceProvider(_dteMock);
            _objectUnderTest.RunStarted(
                _dteMock.Object, _replacementsDictionary, WizardRunKind.AsNewProject, new object[0]);
            _objectUnderTest.ProjectFinishedGenerating(_projectMock.Object);

            _projectMock.VerifySet(
                p => p.Properties.Item(TargetFrameworkMoniker).Value = NetCore11FrameworkName, Times.Once);
            _projectMock.Verify(p => p.Save(MockedProjectPath), Times.Once);
        }

        [TestMethod]
        public void ProjectFinishedGeneratingVs2017DotNetCoreUpdate20()
        {
            _dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2017Version);
            _pickProjectMock.Setup(x => x()).Returns(() => MockProjectId);
            _projectParserMock.Setup(p => p.ParseProject(It.IsAny<Project>())).Returns(
                Mock.Of<IParsedProject>(p => p.ProjectType == KnownProjectTypes.NetCoreWebApplication1_0));
            _buildProject.AddItem(SupportedTargetFrameworkItemName, NetCore10FrameworkName);
            _buildProject.AddItem(SupportedTargetFrameworkItemName, NetCore11FrameworkName);
            _buildProject.AddItem(SupportedTargetFrameworkItemName, NetCore20FrameworkName);
            // ReSharper disable once RedundantAssignment
            Array supportedFrameworks = new[]
            {
                Net451FrameworkName, Net452FrameworkName, Net46FrameworkName
            };
            _frameworkServiceMock.Setup(s => s.GetSupportedFrameworks(out supportedFrameworks)).Returns(VSConstants.S_OK);
            _projectMock.Setup(p => p.Properties.Item(TargetFrameworkMoniker).Value).Returns(NetCore10FrameworkName);
            _projectMock.Setup(p => p.FullName).Returns(MockedProjectPath);

            GoogleCloudExtensionPackageTests.InitGlobalServiceProvider(_dteMock);
            _objectUnderTest.RunStarted(
                _dteMock.Object, _replacementsDictionary, WizardRunKind.AsNewProject, new object[0]);
            _objectUnderTest.ProjectFinishedGenerating(_projectMock.Object);

            _projectMock.VerifySet(
                p => p.Properties.Item(TargetFrameworkMoniker).Value = NetCore20FrameworkName, Times.Once);
            _projectMock.Verify(p => p.Save(MockedProjectPath), Times.Once);
        }

        [TestMethod]
        public void RunFinished()
        {
            Directory.SetCurrentDirectory(TestContext.TestDeploymentDir);
            _pickProjectMock.Setup(x => x()).Returns(() => MockProjectId);

            _objectUnderTest.RunStarted(
                _dteMock.Object,
                _replacementsDictionary,
                WizardRunKind.AsNewProject,
                new object[0]);
            _objectUnderTest.RunFinished();

            Assert.AreEqual(TestContext.TestDeploymentDir, Directory.GetCurrentDirectory());
        }
    }
}
