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

using Google.Apis.Appengine.v1.Data;
using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.FlexStep;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.FlexStep
{
    [TestClass]
    public class FlexStepViewModelTests : ExtensionTestBase
    {
        private const string DefaultProjectId = "DefaultProjectId";
        private const string TargetProjectId = "TargetProjectId";
        private const string VisualStudioProjectName = "VisualStudioProjectName";
        private const string InvalidVersion = "-Invalid Version Name!";
        private const string ValidVersion = "valid-version-name";
        private static readonly Regex s_validNamePattern = new Regex(@"^(?!-)[a-z\d\-]{1,100}$");

        private static readonly Project s_targetProject = new Project { ProjectId = TargetProjectId };
        private static readonly Project s_defaultProject = new Project { ProjectId = DefaultProjectId };
        private static readonly List<string> s_mockedRequiredApis = new List<string> { KnownApis.AppEngineAdminApiName };

        private FlexStepViewModel _objectUnderTest;
        private Mock<IApiManager> _apiManagerMock;
        private TaskCompletionSource<bool> _areServicesEnabledTaskSource;
        private TaskCompletionSource<object> _enableServicesTaskSource;
        private Mock<IGaeDataSource> _gaeDataSourceMock;
        private TaskCompletionSource<Application> _getApplicationTaskSource;
        private Application _mockedApplication;
        private Mock<Func<Task<bool>>> _setAppRegionAsyncFuncMock;
        private TaskCompletionSource<bool> _setAppRegionTaskSource;
        private IPublishDialog _mockedPublishDialog;
        private Mock<Func<Project>> _pickProjectPromptMock;
        private List<string> _changedProperties;

        protected override void BeforeEach()
        {
            base.BeforeEach();

            IParsedProject mockedProject = Mock.Of<IParsedProject>(p => p.Name == VisualStudioProjectName);

            Mock<IPublishDialog> publishDialogMock = new Mock<IPublishDialog>();
            publishDialogMock.Setup(pd => pd.Project).Returns(mockedProject);
            publishDialogMock.Setup(pd => pd.TrackTask(It.IsAny<Task>()));
            _mockedPublishDialog = publishDialogMock.Object;

            _pickProjectPromptMock = new Mock<Func<Project>>();
            _changedProperties = new List<string>();

            _apiManagerMock = new Mock<IApiManager>();
            _apiManagerMock.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>())).Returns(() => _areServicesEnabledTaskSource.Task);
            _apiManagerMock.Setup(x => x.EnableServicesAsync(It.IsAny<IEnumerable<string>>())).Returns(() => _enableServicesTaskSource.Task);

            _gaeDataSourceMock = new Mock<IGaeDataSource>();
            _gaeDataSourceMock.Setup(x => x.GetApplicationAsync()).Returns(() => _getApplicationTaskSource.Task);
            _mockedApplication = Mock.Of<Application>();

            _setAppRegionAsyncFuncMock = new Mock<Func<Task<bool>>>();
            _setAppRegionAsyncFuncMock.Setup(func => func()).Returns(() => _setAppRegionTaskSource.Task);

            _objectUnderTest = FlexStepViewModel.CreateStep(dataSource: _gaeDataSourceMock.Object, apiManager: _apiManagerMock.Object, pickProjectPrompt: _pickProjectPromptMock.Object, setAppRegionAsyncFunc: _setAppRegionAsyncFuncMock.Object);
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
        }

        protected override void AfterEach()
        {
            _objectUnderTest.OnFlowFinished();

            base.AfterEach();
        }

        [TestMethod]
        public void TestInitialState()
        {
            AssertInitialState();
        }

        [TestMethod]
        public void TestInitialStateNoProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);

            AssertSelectedProjectUnchanged();
            AssertInitialState();
        }

        [TestMethod]
        public void TestInitialStateProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnVisibleNoProject()
        {
            await OnVisibleWithProject(null);

            AssertSelectedProjectChanged();
            AssertNoProjectInitialVersion();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetApplicationCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestOnVisiblePositiveValidation()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestOnVisibleNegativeServicesValidation()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertInvalidServicesProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestOnVisibleNegativeApplicationValidation()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertInvalidApplicationProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public void TestOnVisibleLongRunningServicesValidation()
        {
            InitLongRunningAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);

            Task onVisibleTask = OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public void TestOnVisibleLongRunningApplicationValidation()
        {
            InitAreServicesEnabledMock(true);
            InitLongRunningGetApplicationMock();

            Task onVisibleTask = OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestOnVisibleErrorInServicesValidation()
        {
            InitErrorAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestOnVisibleErrorInApplicationValidation()
        {
            InitAreServicesEnabledMock(true);
            InitErrorGetApplicationMock();

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNoExternal()
        {
            await OnVisibleWithProject(null);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectUnchanged();
            AssertNoProjectInitialVersion();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetApplicationCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToPositiveValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNegativeServicesValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidServicesProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromNoToNegativeApplicationValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidApplicationProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToLongRunningServicesValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitLongRunningAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromNoToLongRunningApplicationValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(true);
            InitLongRunningGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToErrorInServicesValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitErrorAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromNoToErrorInApplicationValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(true);
            InitErrorGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToNoExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectChanged();
            AssertNoProjectInitialVersion();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetApplicationCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToPositiveValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToNegativeServicesValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidServicesProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromValidToNegativeApplicationValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitGetApplicationMock(null);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidApplicationProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToLongRunningServicesValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromValidToLongRunningApplicationValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToErrorInServicesValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromValidToErrorInApplicationValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNoExternal()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectChanged();
            AssertNoProjectInitialVersion();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetApplicationCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToPositiveValidationExternal()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNegativeServiceValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidServicesProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromInvalidToNegativeApplicationValidationExternal()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidApplicationProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToLongRunningServicesValidationExternal()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromInvalidToLongRunningApplicationValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToErrorInServicesValidationExternal()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromInvalidToErrorInApplicationValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNoSelectCommand()
        {
            await OnVisibleWithProject(null);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertNoProjectInitialVersion();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetApplicationCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToPositiveValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNegativeServicesValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidServicesProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromNoToNegativeApplicationValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidApplicationProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToLongRunningServicesValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitLongRunningAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromNoToLongRunningApplicationValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(true);
            InitLongRunningGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToErrorInServicesValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitErrorAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromNoToErrorInApplicationValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(true);
            InitErrorGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToNoSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertValidProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetApplicationCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToNegativeServicesValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidServicesProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromValidToNegativeApplicationValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitGetApplicationMock(null);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidApplicationProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToLongRunningServicesValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromValidToLongRunningApplicationValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToErrorInServicesValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromValidToErrorInApplicationValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToSameValidSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertValidProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToSameInvalidSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertInvalidServicesProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromValidProjectInvalidVersionToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);
            await SetVersion(InvalidVersion);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInvalidVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromInvalidToNoSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertInvalidServicesProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetApplicationCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNegativeServiceValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidServicesProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromInvalidToNegativeApplicationValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidApplicationProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToLongRunningServicesValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromInvalidToLongRunningApplicationValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToErrorInServicesValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromInvalidToErrorInApplicationValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToSamePositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertValidProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToSameNegativeValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertInvalidServicesProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromInvalidProjectAndVersionToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);
            await SetVersion(InvalidVersion);

            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInvalidVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToNoSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertErrorInValidationInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetApplicationCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToPositiveValidationSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToNegativeServiceValidationSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidServicesProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToNegativeApplicationValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitErrorGetApplicationMock();
            await OnVisibleWithProject(s_defaultProject);

            InitGetApplicationMock(null);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidApplicationProjectInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToLongRunningServiceValidationSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToLongRunningApplicationValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitErrorGetApplicationMock();
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToErrorInServicesValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitErrorGetApplicationMock();
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToErrorInApplicationValidationSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitErrorGetApplicationMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationInitialVersion(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.AtMostOnce());
            AssertGetApplicationCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestEnableApisCommandSuccess()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitEnableApiMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await RunEnableApiCommand();

            AssertSelectedProjectUnchanged();
            AssertValidProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.Once());
            AssertEnableServicesCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestEnableApisCommandFailure()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            InitEnableApiMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await RunEnableApiCommand();

            AssertSelectedProjectUnchanged();
            AssertInvalidServicesProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.AtMostOnce());
            AssertEnableServicesCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestSetAppRegionCommandSuccess()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);

            InitGetApplicationMock(_mockedApplication);
            InitSetAppRegionMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await RunSetAppRegionCommand();

            AssertSelectedProjectUnchanged();
            AssertValidProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetApplicationCalled(Times.Once());
            AssertSetAppRegionCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestSetAppRegionCommandFailure()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);

            InitSetAppRegionMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await RunSetAppRegionCommand();

            AssertSelectedProjectUnchanged();
            AssertInvalidApplicationProjectInitialVersion(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetApplicationCalled(Times.Never());
            AssertSetAppRegionCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestValidProjectNullVersion()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(null);

            AssertValidProjectNullVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectNullVersion()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(null);

            AssertInvalidServicesProjectNullVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectEmptyVersion()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(string.Empty);

            AssertValidProjectEmptyVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectEmptyVersion()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(string.Empty);

            AssertInvalidServicesProjectEmptyVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectInvalidVersion()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(InvalidVersion);

            AssertValidProjectInvalidVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectInvalidVersion()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(InvalidVersion);

            AssertInvalidServicesProjectInvalidVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestValidProjectValidVersion()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(ValidVersion);

            AssertValidProjectValidVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestInvalidProjectValidVersion()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            await SetVersion(ValidVersion);

            AssertInvalidServicesProjectValidVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestFromValidProjectInvalidVersionToValidVersion()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);
            await SetVersion(InvalidVersion);

            await SetVersion(ValidVersion);

            AssertValidProjectValidVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestFromInvalidProjectInvalidVersionToValidVersion()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);
            await SetVersion(InvalidVersion);

            await SetVersion(ValidVersion);

            AssertInvalidServicesProjectValidVersion(DefaultProjectId);
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromValidState()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromValidEventHandling()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromInvalidState()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromInvalidEventHandling()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromErrorState()
        {
            InitErrorAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromErrorEventHandling()
        {
            InitErrorAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        private void InitAreServicesEnabledMock(bool servicesEnabled)
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _areServicesEnabledTaskSource.SetResult(servicesEnabled);
        }

        private void InitLongRunningAreServicesEnabledMock()
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
        }

        private void InitErrorAreServicesEnabledMock()
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _areServicesEnabledTaskSource.SetException(new DataSourceException());
        }

        private void InitGetApplicationMock(Application application)
        {
            _getApplicationTaskSource = new TaskCompletionSource<Application>();
            _getApplicationTaskSource.SetResult(application);
        }

        private void InitLongRunningGetApplicationMock()
        {
            _getApplicationTaskSource = new TaskCompletionSource<Application>();
        }

        private void InitErrorGetApplicationMock()
        {
            _getApplicationTaskSource = new TaskCompletionSource<Application>();
            _getApplicationTaskSource.SetException(new DataSourceException());
        }

        private void InitEnableApiMock()
        {
            _enableServicesTaskSource = new TaskCompletionSource<object>();
            _enableServicesTaskSource.SetResult(null);
        }

        private void InitSetAppRegionMock(bool result)
        {
            _setAppRegionTaskSource = new TaskCompletionSource<bool>();
            _setAppRegionTaskSource.SetResult(result);
        }

        private void ResetMockCalls()
        {
            _apiManagerMock.ResetCalls();
            _gaeDataSourceMock.ResetCalls();
        }

        private async Task OnVisibleWithProject(Project project)
        {
            CredentialsStore.Default.UpdateCurrentProject(project);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        private async Task OnProjectChangedExternally(Project changedTo)
        {
            CredentialsStore.Default.UpdateCurrentProject(changedTo);
            await _objectUnderTest.AsyncAction;
        }

        private async Task OnProjectChangedSelectProjectCommand(Project changedTo)
        {
            _pickProjectPromptMock.Setup(f => f()).Returns(changedTo);
            _objectUnderTest.SelectProjectCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        private async Task RunEnableApiCommand()
        {
            _objectUnderTest.EnableApiCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        private async Task RunSetAppRegionCommand()
        {
            _objectUnderTest.SetAppRegionCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        private async Task SetVersion(string version)
        {
            _objectUnderTest.Version = version;
            await _objectUnderTest.ValidationDelayTask;
        }

        private void RaiseFlowFinished()
        {
            Mock<IPublishDialog> publishDialogMock = Mock.Get(_mockedPublishDialog);
            publishDialogMock.Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
        }

        private void AssertInitialState()
        {
            AssertInitialVersionState();

            Assert.IsNotNull(_objectUnderTest.AsyncAction);
            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsNull(_objectUnderTest.PublishDialog);
            Assert.IsNull(_objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanGoNext);

            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);

            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertNoProjectInitialVersion()
        {
            AssertInvariantsAfterVisible();

            AssertInitialVersionState();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsNull(_objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);

            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);

            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertValidProjectInitialVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertValidProject(expectedProjectId);
            AssertInitialVersionState();

            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsTrue(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectNullVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertValidProject(expectedProjectId);

            Assert.IsNull(_objectUnderTest.Version);

            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectEmptyVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertValidProject(expectedProjectId);

            Assert.AreEqual(string.Empty, _objectUnderTest.Version);

            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectInvalidVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertValidProject(expectedProjectId);

            Assert.AreEqual(InvalidVersion, _objectUnderTest.Version);

            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertValidProjectValidVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertValidProject(expectedProjectId);

            Assert.AreEqual(ValidVersion, _objectUnderTest.Version);

            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsTrue(_objectUnderTest.CanPublish);
        }

        private void AssertValidProject(string expectedProjectId)
        {
            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsTrue(_objectUnderTest.ShowInputControls);
        }

        private void AssertInvalidServicesProjectInitialVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertInvalidServicesProject(expectedProjectId);
            AssertInitialVersionState();

            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidServicesProjectNullVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertInvalidServicesProject(expectedProjectId);

            Assert.IsNull(_objectUnderTest.Version);

            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidServicesProjectEmptyVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertInvalidServicesProject(expectedProjectId);

            Assert.AreEqual(string.Empty, _objectUnderTest.Version);

            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidServicesProjectInvalidVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertInvalidServicesProject(expectedProjectId);

            Assert.AreEqual(InvalidVersion, _objectUnderTest.Version);

            Assert.IsTrue(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidServicesProjectValidVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertInvalidServicesProject(expectedProjectId);

            Assert.AreEqual(ValidVersion, _objectUnderTest.Version);

            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidServicesProject(string expectedProjectId)
        {
            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsTrue(_objectUnderTest.NeedsApiEnabled);
            Assert.IsTrue(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertInvalidApplicationProjectInitialVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertInvalidApplicationProject(expectedProjectId);
            AssertInitialVersionState();

            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        private void AssertInvalidApplicationProject(string expectedProjectId)
        {
            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.NeedsAppCreated);
            Assert.IsTrue(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertLongRunningValidationInitialVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertInitialVersionState();

            Assert.IsFalse(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsTrue(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertErrorInValidationInitialVersion(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            AssertInitialVersionState();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertInitialVersionState()
        {
            Assert.IsNotNull(_objectUnderTest.Version);
            Assert.IsTrue(s_validNamePattern.IsMatch(_objectUnderTest.Version));
        }

        private void AssertSelectedProjectChanged()
        {
            CollectionAssert.Contains(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
        }

        private void AssertSelectedProjectUnchanged()
        {
            CollectionAssert.DoesNotContain(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
        }

        private void AssertAreServicesEnabledCalled(Times times)
        {
            _apiManagerMock.Verify(api => api.AreServicesEnabledAsync(It.IsAny<IList<string>>()), times);
        }

        private void AssertGetApplicationCalled(Times times)
        {
            _gaeDataSourceMock.Verify(src => src.GetApplicationAsync(), times);
        }

        private void AssertEnableServicesCalled(Times times)
        {
            _apiManagerMock.Verify(api => api.EnableServicesAsync(It.IsAny<IEnumerable<string>>()), times);
        }

        private void AssertSetAppRegionCalled(Times times)
        {
            _setAppRegionAsyncFuncMock.Verify(f => f(), times);
        }

        private void AssertInvariantsAfterVisible()
        {
            Assert.IsNotNull(_objectUnderTest.AsyncAction);
            Assert.AreEqual(_mockedPublishDialog, _objectUnderTest.PublishDialog);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsTrue(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
        }
    }
}
