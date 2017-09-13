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
using GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TemplateWizard;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using stdole;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
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
        private const string DefaultSolutionDirectoryName = @"DefaultSolutionDirectory";
        private const string DefaultSolutionDirectory = @"root:\" + DefaultSolutionDirectoryName;
        private const string DefaultDestinationDirectoryName = @"DefaultDestinationDirectory";
        private const string DefaultDestinationDirectory =
            DefaultSolutionDirectory + @"\" + DefaultDestinationDirectoryName;

        private const string SelectorTemplateSubPath = @"CSharp\Google Cloud Platform\1033\Gcp\Gcp.vstemplate";
        private const string TargetTemplateSubPathFormat = @"CSharp\{0}\1033\{1}\{1}.vstemplate";
        private const string DefaultTemplatesBasePath = @"c:\ProjectTemplates\";
        private const string DefaultSelectorTemplatePath = DefaultTemplatesBasePath + SelectorTemplateSubPath;
        private const string TargetTemplatePathFromat = DefaultTemplatesBasePath + TargetTemplateSubPathFormat;

        private const string DefaultProjectName = "DefaultProjectName";
        private const string DefaultProjectId = "default-project-id";
        private const AppType DefaultAppType = AppType.Mvc;
        private const FrameworkType DefaultFrameworkType = FrameworkType.NetCore;


        private static readonly AspNetVersion s_defaultAspNetVersion = AspNetVersion.AspNetCore1Preview;

        private static readonly string s_defaultTargetTemplatePath =
            string.Format(TargetTemplatePathFromat, DefaultAppType, s_defaultAspNetVersion.Version);


        private GoogleProjectTemplateSelectorWizard _objectUnderTest;
        private Dictionary<string, string> _replacements;
        private Mock<DTE> _dteMock;
        private Mock<IVsSolution6> _solutionMock;
        private Mock<Action<Dictionary<string, string>>> _cleanupDirectoriesMock;
        private Mock<Func<string, TemplateChooserViewModelResult>> _promptUserMock;
        private IVsHierarchy _newHierarchy;
        private TemplateChooserViewModelResult _promptResult;
        private object[] _customParams;
        private Dictionary<string, string> _newCustomParams;

        [TestInitialize]
        public void BeforeEach()
        {

            _replacements = new Dictionary<string, string>
            {
                {ReplacementsKeys.ProjectNameKey, DefaultProjectName},
                {ReplacementsKeys.DestinationDirectoryKey, DefaultDestinationDirectory},
                {ReplacementsKeys.SolutionDirectoryKey, DefaultSolutionDirectory }
            };
            _customParams = new object[]
            {
                DefaultSelectorTemplatePath
            };
            _promptResult = new TemplateChooserViewModelResult(
                DefaultProjectId, DefaultFrameworkType, s_defaultAspNetVersion, DefaultAppType);

            _promptUserMock = new Mock<Func<string, TemplateChooserViewModelResult>>();
            _promptUserMock.Setup(p => p(It.IsAny<string>())).Returns(() => _promptResult);
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

            _newCustomParams =
                new Dictionary<string, string>
                {
                    {ReplacementsKeys.GcpProjectIdKey, DefaultProjectId},
                    {ReplacementsKeys.SolutionDirectoryKey, DefaultSolutionDirectory},
                    {ReplacementsKeys.PackagesPathKey, @"..\packages\"},
                    {ReplacementsKeys.TargetFrameworkKey, "netcoreapp1.0-preview"}
                };
        }

        private bool TestCustomParams(IEnumerable customParams)
        {
            return _newCustomParams.All(pair => customParams.OfType<object>().Contains($"{pair.Key}={pair.Value}"));
        }

        [TestMethod]
        [ExpectedException(typeof(WizardCancelledException))]
        public void TestRunStartedDefaults()
        {
            try
            {
                _objectUnderTest.RunStarted(_dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (WizardCancelledException)
            {
                _promptUserMock.Verify(p => p(DefaultProjectName), Times.Once);
                _promptUserMock.Verify(p => p(It.IsNotIn(DefaultProjectName)), Times.Never);
                _cleanupDirectoriesMock.Verify(d => d(It.IsAny<Dictionary<string, string>>()), Times.Never);
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                        s_defaultTargetTemplatePath, It.Is<Array>(a => TestCustomParams(a)), null,
                        DefaultDestinationDirectory, DefaultProjectName, It.IsAny<IVsHierarchy>(), out _newHierarchy),
                    Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardCancelledException))]
        public void TestRunStartedDifferentSolutionDir()
        {
            _replacements[ReplacementsKeys.SolutionDirectoryKey] = DefaultDestinationDirectory;
            _newCustomParams[ReplacementsKeys.SolutionDirectoryKey] = DefaultDestinationDirectory;
            _newCustomParams[ReplacementsKeys.PackagesPathKey] = @"packages\";
            try
            {
                _objectUnderTest.RunStarted(_dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (WizardCancelledException)
            {
                _promptUserMock.Verify(p => p(DefaultProjectName), Times.Once);
                _promptUserMock.Verify(p => p(It.IsNotIn(DefaultProjectName)), Times.Never);
                _cleanupDirectoriesMock.Verify(d => d(It.IsAny<Dictionary<string, string>>()), Times.Never);
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                        s_defaultTargetTemplatePath, It.Is<Array>(a => TestCustomParams(a)), It.IsAny<string>(),
                        DefaultDestinationDirectory, DefaultProjectName, It.IsAny<IVsHierarchy>(), out _newHierarchy),
                    Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardCancelledException))]
        public void TestRunStartedDifferentSourceTemplate()
        {
            const string templateBasePath = @"root:\NewBasePath\";
            _customParams = new object[]
            {
                templateBasePath + SelectorTemplateSubPath
            };
            try
            {
                _objectUnderTest.RunStarted(_dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (WizardCancelledException)
            {
                _promptUserMock.Verify(p => p(DefaultProjectName), Times.Once);
                _promptUserMock.Verify(p => p(It.IsNotIn(DefaultProjectName)), Times.Never);
                _cleanupDirectoriesMock.Verify(d => d(It.IsAny<Dictionary<string, string>>()), Times.Never);
                string templatePath = templateBasePath +
                    string.Format(TargetTemplateSubPathFormat, DefaultAppType, s_defaultAspNetVersion.Version);
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                        templatePath, It.Is<Array>(a => TestCustomParams(a)), It.IsAny<string>(),
                        DefaultDestinationDirectory, DefaultProjectName, It.IsAny<IVsHierarchy>(), out _newHierarchy),
                    Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardCancelledException))]
        public void TestRunStartedDifferentDestinationDir()
        {
            const string destinationDirectory = @"root:\Destination";
            _replacements[ReplacementsKeys.DestinationDirectoryKey] = destinationDirectory;
            _newCustomParams[ReplacementsKeys.PackagesPathKey] = $@"..\{DefaultSolutionDirectoryName}\packages\";
            try
            {
                _objectUnderTest.RunStarted(_dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (WizardCancelledException)
            {
                _promptUserMock.Verify(p => p(DefaultProjectName), Times.Once);
                _promptUserMock.Verify(p => p(It.IsNotIn(DefaultProjectName)), Times.Never);
                _cleanupDirectoriesMock.Verify(d => d(It.IsAny<Dictionary<string, string>>()), Times.Never);
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                        s_defaultTargetTemplatePath, It.Is<Array>(a => TestCustomParams(a)), It.IsAny<string>(),
                        destinationDirectory, DefaultProjectName, It.IsAny<IVsHierarchy>(), out _newHierarchy),
                    Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardCancelledException))]
        public void TestRunStartedDifferentProjectName()
        {
            const string newProjectName = "NewProjectName";
            _replacements[ReplacementsKeys.ProjectNameKey] = newProjectName;

            try
            {
                _objectUnderTest.RunStarted(_dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (WizardCancelledException)
            {
                _promptUserMock.Verify(p => p(newProjectName), Times.Once);
                _promptUserMock.Verify(p => p(It.IsNotIn(newProjectName)), Times.Never);
                _cleanupDirectoriesMock.Verify(d => d(It.IsAny<Dictionary<string, string>>()), Times.Never);
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                        s_defaultTargetTemplatePath, It.Is<Array>(a => TestCustomParams(a)), It.IsAny<string>(),
                        DefaultDestinationDirectory, newProjectName, It.IsAny<IVsHierarchy>(), out _newHierarchy),
                    Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardCancelledException))]
        public void TestRunStartedOverridePrompt()
        {
            _replacements.Add(
                ReplacementsKeys.TemplateChooserResultKey,
                JsonConvert.SerializeObject(_promptResult));
            try
            {
                _objectUnderTest.RunStarted(_dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (WizardCancelledException)
            {
                _promptUserMock.Verify(p => p(It.IsAny<string>()), Times.Never);
                _cleanupDirectoriesMock.Verify(d => d(It.IsAny<Dictionary<string, string>>()), Times.Never);
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                        s_defaultTargetTemplatePath, It.Is<Array>(a => TestCustomParams(a)), It.IsAny<string>(),
                        DefaultDestinationDirectory, DefaultProjectName, It.IsAny<IVsHierarchy>(), out _newHierarchy),
                    Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardBackoutException))]
        public void TestRunStartedBackout()
        {
            _promptResult = null;
            try
            {
                _objectUnderTest.RunStarted(
                    _dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (WizardBackoutException)
            {
                _promptUserMock.Verify(p => p(DefaultProjectName), Times.Once);
                _promptUserMock.Verify(p => p(It.IsNotIn(DefaultProjectName)), Times.Never);
                _cleanupDirectoriesMock.Verify(d => d(_replacements), Times.Once);
                _cleanupDirectoriesMock.Verify(d => d(It.IsNotIn(_replacements)), Times.Never);
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                        It.IsAny<string>(), It.Is<Array>(a => TestCustomParams(a)), It.IsAny<string>(),
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IVsHierarchy>(), out _newHierarchy),
                    Times.Never);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(WizardCancelledException))]
        public void TestRunStartedNetFrameworkType()
        {
            _promptResult = new TemplateChooserViewModelResult(
                DefaultProjectId, FrameworkType.NetFramework, s_defaultAspNetVersion, DefaultAppType);
            _newCustomParams[ReplacementsKeys.TargetFrameworkKey] = "net461";
            try
            {
                _objectUnderTest.RunStarted(_dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (WizardCancelledException)
            {
                _promptUserMock.Verify(p => p(DefaultProjectName), Times.Once);
                _promptUserMock.Verify(p => p(It.IsNotIn(DefaultProjectName)), Times.Never);
                _cleanupDirectoriesMock.Verify(d => d(It.IsAny<Dictionary<string, string>>()), Times.Never);
                _solutionMock.Verify(
                    s => s.AddNewProjectFromTemplate(
                        s_defaultTargetTemplatePath, It.Is<Array>(a => TestCustomParams(a)), null,
                        DefaultDestinationDirectory, DefaultProjectName, It.IsAny<IVsHierarchy>(), out _newHierarchy),
                    Times.Once);
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestRunStartedInvalidFrameworkType()
        {
            _promptResult = new TemplateChooserViewModelResult(
                DefaultProjectId, (FrameworkType)(-1), s_defaultAspNetVersion, DefaultAppType);
            try
            {
                _objectUnderTest.RunStarted(_dteMock.Object, _replacements, WizardRunKind.AsNewProject, _customParams);
            }
            catch (InvalidOperationException)
            {
                _promptUserMock.Verify(p => p(DefaultProjectName), Times.Once);
                _promptUserMock.Verify(p => p(It.IsNotIn(DefaultProjectName)), Times.Never);
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
        [ExpectedException(typeof(NotImplementedException))]
        public void TestProjectFinishedGenerating()
        {
            _objectUnderTest.ProjectFinishedGenerating(Mock.Of<Project>());
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void TestProjectItemFinishedGenerating()
        {
            _objectUnderTest.ProjectItemFinishedGenerating(Mock.Of<ProjectItem>());
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void TestShouldAddProjectItem()
        {
            _objectUnderTest.ShouldAddProjectItem("");
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void TestBeforeOpeningFile()
        {
            _objectUnderTest.BeforeOpeningFile(Mock.Of<ProjectItem>());
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void TestRunFinished()
        {
            _objectUnderTest.RunFinished();
        }
    }
}
