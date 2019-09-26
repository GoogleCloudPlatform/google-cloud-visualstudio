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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using GoogleCloudExtension.TemplateWizards;
using GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using stdole;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace GoogleCloudExtensionUnitTests.TemplateWizards
{
    /// <summary>
    /// Class for testing <see cref="GoogleProjectTemplateSelectorWizard"/>.
    /// </summary>
    [TestClass]
    [SuppressMessage("ReSharper", "RedundantAssignment")]
    public class GoogleProjectTemplateSelectorWizardTests
    {
        private const string DefaultSolutionDirectory = @"c:\DefaultSolutionDirectory";

        private const string DefaultDestinationDirectory =
                @"c:\DefaultSolutionDirectory\DefaultDestinationDirectory";

        private const string DefaultAspDotNetTemplatePath =
                @"c:\ProjectTemplates\CSharp\Google Cloud Platform\1033\Gcp\Gcp.AspNet.vstemplate";

        private const string DefaultProjectName = "DefaultProjectName";
        private const string DefaultProjectId = "default-project-id";
        private const FrameworkType DefaultFrameworkType = FrameworkType.NetCore;
        private const string AspNetWizardData = "<TemplateType>AspNet</TemplateType>";
        private const string AspNetCoreWizardData = "<TemplateType>AspNetCore</TemplateType>";

        private GoogleProjectTemplateSelectorWizard _objectUnderTest;
        private Dictionary<string, string> _replacements;
        private Mock<DTE> _dteMock;
        private Mock<IVsSolution6> _solutionMock;
        private Mock<Action<Dictionary<string, string>>> _cleanupDirectoriesMock;
        private Mock<Func<string, TemplateType, TemplateChooserViewModelResult>> _promptUserMock;
        private IVsHierarchy _newHierarchy;
        private TemplateChooserViewModelResult _promptResult;
        private object[] _customParams;
        private Dictionary<string, string> _expectedCustomParams;

        [TestInitialize]
        public void BeforeEach()
        {
            _replacements = new Dictionary<string, string>
            {
                [ReplacementsKeys.ProjectNameKey] = DefaultProjectName,
                [ReplacementsKeys.DestinationDirectoryKey] = DefaultDestinationDirectory,
                [ReplacementsKeys.SolutionDirectoryKey] = DefaultSolutionDirectory,
                [ReplacementsKeys.WizardDataKey] = AspNetWizardData
            };
            _customParams = new object[]
            {
                DefaultAspDotNetTemplatePath
            };
            _promptResult = new TemplateChooserViewModelResult(
                DefaultProjectId, DefaultFrameworkType, AspNetVersion.AspNetCore10, AppType.Mvc);

            _promptUserMock = new Mock<Func<string, TemplateType, TemplateChooserViewModelResult>>();
            _promptUserMock.Setup(p => p(It.IsAny<string>(), It.IsAny<TemplateType>()))
                    .Returns(() => _promptResult);
            _cleanupDirectoriesMock = new Mock<Action<Dictionary<string, string>>>();

            _objectUnderTest = new GoogleProjectTemplateSelectorWizard
            {
                CleanupDirectories = _cleanupDirectoriesMock.Object,
                PromptUser = _promptUserMock.Object
            };

            _dteMock = new Mock<DTE>();
            Guid guidService = typeof(SVsSolution).GUID;
            Guid uuid = typeof(IUnknown).GUID;
            _newHierarchy = Mock.Of<IVsHierarchy>();
            _solutionMock = new Mock<IVsSolution6>();
            _solutionMock.Setup(
                    s => s.AddNewProjectFromTemplate(
                        It.IsAny<string>(), It.IsAny<Array>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<string>(), It.IsAny<IVsHierarchy>(), out _newHierarchy))
                .Returns(VSConstants.S_OK);
            IntPtr mockedSolutionPtr = Marshal.GetIUnknownForObject(_solutionMock.Object);
            _dteMock.As<IServiceProvider>().Setup(
                sp => sp.QueryService(ref guidService, ref uuid, out mockedSolutionPtr));

            _expectedCustomParams = new Dictionary<string, string>();
        }

        private bool AreExpectedCustomParams(IEnumerable customParams)
        {
            return _expectedCustomParams.All(pair => customParams.OfType<object>().Contains($"{pair.Key}={pair.Value}"));
        }

        [TestMethod]
        public void TestWizardData_AspNetPromptsForAspDotNet()
        {
            const string newProjectName = "AspNetProjectName";
            _replacements[ReplacementsKeys.ProjectNameKey] = newProjectName;
            _replacements[ReplacementsKeys.WizardDataKey] = AspNetWizardData;

            Assert.ThrowsException<WizardCancelledException>(
                () => _objectUnderTest.RunStarted(
                    _dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams));

            _promptUserMock.Verify(p => p(newProjectName, TemplateType.AspNet), Times.Once);
        }

        [TestMethod]
        public void TestWizardData_AspNetCorePromptsForAspDotNetCore()
        {
            const string newProjectName = "AspNetCoreProjectName";
            _replacements[ReplacementsKeys.ProjectNameKey] = newProjectName;
            _replacements[ReplacementsKeys.WizardDataKey] = AspNetCoreWizardData;

            Assert.ThrowsException<WizardCancelledException>(
                () => _objectUnderTest.RunStarted(
                    _dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams));

            _promptUserMock.Verify(p => p(newProjectName, TemplateType.AspNetCore), Times.Once);
        }

        [TestMethod]
        public void TestWizardData_MissingPromptsForAspDotNetCore()
        {
            const string newProjectName = "AspNetCoreProjectName";
            _replacements[ReplacementsKeys.ProjectNameKey] = newProjectName;
            _replacements.Remove(ReplacementsKeys.WizardDataKey);

            Assert.ThrowsException<WizardCancelledException>(
                () => _objectUnderTest.RunStarted(
                    _dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams));

            _promptUserMock.Verify(p => p(newProjectName, TemplateType.AspNetCore), Times.Once);
        }

        [TestMethod]
        public void TestAddTemplateCallWithSameDestinationDirectory()
        {
            const string newProjectName = "SameDirectoryProject";
            const string targetDirectory = @"c:\TargetDirectory";
            const string packagesPath = @"packages\";

            _replacements[ReplacementsKeys.ProjectNameKey] = newProjectName;
            _replacements[ReplacementsKeys.SolutionDirectoryKey] = targetDirectory;
            _replacements[ReplacementsKeys.DestinationDirectoryKey] = targetDirectory;

            Assert.ThrowsException<WizardCancelledException>(
                () => _objectUnderTest.RunStarted(
                    _dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams));

            _expectedCustomParams[ReplacementsKeys.SolutionDirectoryKey] = targetDirectory;
            _expectedCustomParams[ReplacementsKeys.PackagesPathKey] = packagesPath;
            _solutionMock.Verify(
                s => s.AddNewProjectFromTemplate(
                    It.IsAny<string>(), It.Is<Array>(a => AreExpectedCustomParams(a)), It.IsAny<string>(),
                    targetDirectory, newProjectName, null, out _newHierarchy),
                Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(WizardCancelledException))]
        public void TestAddTemplateCallWithDifferentDestinationDirectory()
        {
            const string newProjectName = "DifferentDirectoryProject";
            const string destinationDirectory = @"c:\DestinationDirectory";
            const string packagesPath = @"..\SolutionDirectory\packages\";
            const string solutionDirectory = @"c:\SolutionDirectory";

            _replacements[ReplacementsKeys.ProjectNameKey] = newProjectName;
            _replacements[ReplacementsKeys.DestinationDirectoryKey] = destinationDirectory;
            _replacements[ReplacementsKeys.SolutionDirectoryKey] = solutionDirectory;

            try
            {
                _objectUnderTest.RunStarted(_dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (WizardCancelledException)
            {
                _expectedCustomParams[ReplacementsKeys.PackagesPathKey] = packagesPath;
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                            It.IsAny<string>(), It.Is<Array>(a => AreExpectedCustomParams(a)), It.IsAny<string>(),
                        destinationDirectory, newProjectName, null, out _newHierarchy),
                    Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardCancelledException))]
        public void TestDifferentProjectNameParameter()
        {
            const string newProjectName = "NewProjectName";
            _replacements[ReplacementsKeys.ProjectNameKey] = newProjectName;

            try
            {
                _objectUnderTest.RunStarted(_dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (WizardCancelledException)
            {
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                        It.IsAny<string>(), It.IsAny<Array>(), It.IsAny<string>(),
                        It.IsAny<string>(), newProjectName, It.IsAny<IVsHierarchy>(), out _newHierarchy),
                    Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardCancelledException))]
        public void TestRunStartedOverridePrompt()
        {
            const string targetDirectory = @"c:\TargetDirectory";
            const string packagesPath = @"packages\";
            const string thisTemplatePath =
                    @"c:\ProjectTemplates\CSharp\Google Cloud Platform\1033\Gcp\Gcp.AspNet.vstemplate";
            var result = new TemplateChooserViewModelResult(
                    "overrideProjectId", FrameworkType.NetCore, AspNetVersion.AspNetCore11, AppType.WebApi);
            const string expectedTargetTemplatePath = @"c:\ProjectTemplates\CSharp\WebApi\1033\1.1\1.1.vstemplate";

            _replacements[ReplacementsKeys.SolutionDirectoryKey] = targetDirectory;
            _replacements[ReplacementsKeys.DestinationDirectoryKey] = targetDirectory;
            _replacements.Add(ReplacementsKeys.TemplateChooserResultKey, JsonConvert.SerializeObject(result));
            try
            {
                _objectUnderTest.RunStarted(
                        _dteMock.Object, _replacements, WizardRunKind.AsNewProject, new object[] { thisTemplatePath });
            }
            catch (WizardCancelledException)
            {
                _promptUserMock.Verify(p => p(It.IsAny<string>(), It.IsAny<TemplateType>()), Times.Never);
                _expectedCustomParams[ReplacementsKeys.PackagesPathKey] = packagesPath;
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                       expectedTargetTemplatePath, It.Is<Array>(a => AreExpectedCustomParams(a)), null,
                       targetDirectory, It.IsAny<string>(), null, out _newHierarchy),
                    Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardBackoutException))]
        public void TestBackOut()
        {
            _promptResult = null;
            try
            {
                _objectUnderTest.RunStarted(
                    _dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (WizardBackoutException)
            {
                _cleanupDirectoriesMock.Verify(d => d(_replacements), Times.Once);
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                        It.IsAny<string>(), It.IsAny<Array>(), It.IsAny<string>(),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IVsHierarchy>(), out _newHierarchy),
                    Times.Never);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardCancelledException))]
        public void TestNetFrameworkTemplatePath()
        {
            _promptResult = new TemplateChooserViewModelResult(
                    DefaultProjectId, FrameworkType.NetFramework, AspNetVersion.AspNet4, AppType.Mvc);
            const string sourceTemplatePath =
                    @"c:\ProjectTemplates\CSharp\Google Cloud Platform\1033\Gcp\Gcp.AspNet.vstemplate";
            const string expectedTargetTemplatePath = @"c:\ProjectTemplates\CSharp\Mvc\1033\4\4.vstemplate";
            try
            {
                _objectUnderTest.RunStarted(
                        _dteMock.Object, _replacements, WizardRunKind.AsNewProject,
                        new object[] { sourceTemplatePath });
            }
            catch (WizardCancelledException)
            {
                _solutionMock.Verify(
                        s => s.AddNewProjectFromTemplate(
                                expectedTargetTemplatePath, It.IsAny<Array>(), It.IsAny<string>(), It.IsAny<string>(),
                                It.IsAny<string>(), It.IsAny<IVsHierarchy>(), out _newHierarchy), Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardCancelledException))]
        public void TestAspCoreNetFrameworkTemplatePath()
        {
            _promptResult = new TemplateChooserViewModelResult(
                    DefaultProjectId, FrameworkType.NetFramework, AspNetVersion.AspNetCore10, AppType.Mvc);
            const string sourceTemplatePath =
                    @"c:\ProjectTemplates\CSharp\Google Cloud Platform\1033\Gcp\Gcp.AspNetCore.vstemplate";
            const string expectedTargetTemplatePath = @"c:\ProjectTemplates\CSharp\Mvc\1033\1.0\1.0.vstemplate";
            try
            {
                _objectUnderTest.RunStarted(
                        _dteMock.Object, _replacements, WizardRunKind.AsNewProject,
                        new object[] { sourceTemplatePath });
            }
            catch (WizardCancelledException)
            {
                _solutionMock.Verify(
                        s => s.AddNewProjectFromTemplate(
                                expectedTargetTemplatePath, It.IsAny<Array>(), It.IsAny<string>(), It.IsAny<string>(),
                                It.IsAny<string>(), It.IsAny<IVsHierarchy>(), out _newHierarchy), Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestRunStartedInvalidFrameworkType()
        {
            _promptResult = new TemplateChooserViewModelResult(
                DefaultProjectId, FrameworkType.None, AspNetVersion.AspNetCore10, AppType.Mvc);
            try
            {
                _objectUnderTest.RunStarted(_dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (InvalidOperationException)
            {
                _cleanupDirectoriesMock.Verify(d => d(It.IsAny<Dictionary<string, string>>()), Times.Once);
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                        It.IsAny<string>(), It.IsAny<Array>(), It.IsAny<string>(),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IVsHierarchy>(), out _newHierarchy),
                    Times.Never);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestRunStartedUnknownFrameworkType()
        {
            _promptResult = new TemplateChooserViewModelResult(
                DefaultProjectId, (FrameworkType)(-1), AspNetVersion.AspNetCore10, AppType.Mvc);
            try
            {
                _objectUnderTest.RunStarted(_dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (InvalidOperationException)
            {
                _cleanupDirectoriesMock.Verify(d => d(It.IsAny<Dictionary<string, string>>()), Times.Once);
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                        It.IsAny<string>(), It.IsAny<Array>(), It.IsAny<string>(),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IVsHierarchy>(), out _newHierarchy),
                    Times.Never);
                throw;
            }
        }

        [TestMethod]
        public void TestProjectFinishedGenerating() =>
            Assert.ThrowsException<NotSupportedException>(
                () => _objectUnderTest.ProjectFinishedGenerating(Mock.Of<Project>()));

        [TestMethod]
        public void TestProjectItemFinishedGenerating() => Assert.ThrowsException<NotSupportedException>(
            () => _objectUnderTest.ProjectItemFinishedGenerating(Mock.Of<ProjectItem>()));

        [TestMethod]
        public void TestShouldAddProjectItem() =>
            Assert.ThrowsException<NotSupportedException>(() => _objectUnderTest.ShouldAddProjectItem(""));

        [TestMethod]
        public void TestBeforeOpeningFile() =>
            Assert.ThrowsException<NotSupportedException>(
                () => _objectUnderTest.BeforeOpeningFile(Mock.Of<ProjectItem>()));

        [TestMethod]
        public void TestRunFinished() =>
            Assert.ThrowsException<NotSupportedException>(() => _objectUnderTest.RunFinished());
    }
}
