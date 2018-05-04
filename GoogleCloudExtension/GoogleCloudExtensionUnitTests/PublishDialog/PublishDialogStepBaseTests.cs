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

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialog
{
    [TestClass]
    public class PublishDialogStepBaseTests : ExtensionTestBase
    {
        private const string DefaultProjectId = "DefaultProjectId";
        private const string TargetProjectId = "TargetProjectId";
        private const string VisualStudioProjectName = "VisualStudioProjectName";

        private static readonly Project s_targetProject = new Project { ProjectId = TargetProjectId };
        private static readonly Project s_defaultProject = new Project { ProjectId = DefaultProjectId };
        private static readonly List<string> s_mockedRequiredApis = new List<string> { "OneRequieredApi", "AnotherRequiredApi" };

        private PublishDialogStepBase _objectUnderTest;
        private Mock<PublishDialogStepBase> _objectUnderTestImpl;
        private Mock<IApiManager> _apiManagerMock;
        private TaskCompletionSource<bool> _areServicesEnabledTaskSource;
        private TaskCompletionSource<object> _enableServicesTaskSource;
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

            _objectUnderTestImpl = new Mock<PublishDialogStepBase>(_apiManagerMock.Object, _pickProjectPromptMock.Object) { CallBase = true };
            _objectUnderTest = _objectUnderTestImpl.Object;
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
            AssertNoProjectState();
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestOnVisibleNoProjectRequiresApis()
        {
            ImplementRequiredApisMock();

            await OnVisibleWithProject(null);

            AssertSelectedProjectChanged();
            AssertNoProjectState();
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestOnVisibleNoApisRequired()
        {
            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestOnVisiblePositiveValidation()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(true);

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestOnVisibleNegativeValidation()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public void TestOnVisibleLongRunningValidation()
        {
            ImplementRequiredApisMock();
            InitLongRunningAreServicesEnabledMock();

            Task onVisibleTask = OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestOnVisibleErrorInValidation()
        {
            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNoExternal()
        {
            await OnVisibleWithProject(null);

            ImplementRequiredApisMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectUnchanged();
            AssertNoProjectState();
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToDoesNotRequireApisExternal()
        {
            await OnVisibleWithProject(null);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToPositiveValidationExternal()
        {
            await OnVisibleWithProject(null);

            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNegativeValidationExternal()
        {
            await OnVisibleWithProject(null);

            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToLongRunningValidationExternal()
        {
            await OnVisibleWithProject(null);

            ImplementRequiredApisMock();
            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToErrorInValidationExternal()
        {
            await OnVisibleWithProject(null);

            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToNoExternal()
        {
            await OnVisibleWithProject(s_defaultProject);

            ImplementRequiredApisMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectChanged();
            AssertNoProjectState();
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToDoesNotRequiresApisExternal()
        {
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToPositiveValidationExternal()
        {
            await OnVisibleWithProject(s_defaultProject);

            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToNegativeValidationExternal()
        {
            await OnVisibleWithProject(s_defaultProject);

            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToLongRunningValidationExternal()
        {
            await OnVisibleWithProject(s_defaultProject);

            ImplementRequiredApisMock();
            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToErrorInValidationExternal()
        {
            await OnVisibleWithProject(s_defaultProject);

            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNoExternal()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectChanged();
            AssertNoProjectState();
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToDoesNotRequiresApisExternal()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            ImplementEmptyRequiredApisMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToPositiveValidationExternal()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNegativeValidationExternal()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToLongRunningValidationExternal()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToErrorInValidationExternal()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNoSelectCommand()
        {
            await OnVisibleWithProject(null);

            ImplementRequiredApisMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertNoProjectState();
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToDoesNotRequiresApisSelectCommand()
        {
            await OnVisibleWithProject(null);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToPositiveValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNegativeValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToLongRunningValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            ImplementRequiredApisMock();
            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToErrorInValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToNoSelectCommand()
        {
            await OnVisibleWithProject(s_defaultProject);

            ImplementRequiredApisMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertValidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToDoesNotRequiresApisSelectCommand()
        {
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToPositiveValidationSelectCommand()
        {
            await OnVisibleWithProject(s_defaultProject);

            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToNegativeValidationSelectCommand()
        {
            await OnVisibleWithProject(s_defaultProject);

            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToLongRunningValidationSelectCommand()
        {
            await OnVisibleWithProject(s_defaultProject);

            ImplementRequiredApisMock();
            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToErrorInValidationSelectCommand()
        {
            await OnVisibleWithProject(s_defaultProject);

            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToSameValidSelectCommand()
        {
            await OnVisibleWithProject(s_defaultProject);

            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertValidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToSameInvalidSelectCommand()
        {
            await OnVisibleWithProject(s_defaultProject);

            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertInvalidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNoSelectCommand()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertInvalidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToDoesNotRequiresApisSelectCommand()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            ImplementEmptyRequiredApisMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToPositiveValidationSelectCommand()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNegativeValidationSelectCommand()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToLongRunningValidationSelectCommand()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToErrorInValidationSelectCommand()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToSamePositiveValidationSelectCommand()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertValidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToSameNegativeValidationSelectCommand()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertInvalidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToNoSelectCommand()
        {
            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertErrorInValidationState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToDoesNotRequiresApisSelectCommand()
        {
            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            ImplementEmptyRequiredApisMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToPositiveValidationSelectCommand()
        {
            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToNegativeValidationSelectCommand()
        {
            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToLongRunningValidationSelectCommand()
        {
            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToErrorInValidationSelectCommand()
        {
            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestEnableAPIsCommandSuccess()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitEnableApiMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await RunEnableApiCommand();

            AssertSelectedProjectUnchanged();
            AssertValidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestEnableAPIsCommandFailure()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitEnableApiMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await RunEnableApiCommand();

            AssertSelectedProjectUnchanged();
            AssertInvalidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromValidState()
        {
            await OnVisibleWithProject(s_defaultProject);

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromValidEventHandling()
        {
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromInvalidState()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromInvalidEventHandling()
        {
            ImplementRequiredApisMock();
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromErrorState()
        {
            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromErrorEventHandling()
        {
            ImplementRequiredApisMock();
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        private void ImplementRequiredApisMock()
        {
            _objectUnderTestImpl.Setup(d => d.ApisRequieredForPublishing()).Returns(s_mockedRequiredApis);
        }

        private void ImplementEmptyRequiredApisMock()
        {
            _objectUnderTestImpl.Setup(d => d.ApisRequieredForPublishing()).Returns(new List<string>());
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

        private void InitEnableApiMock()
        {
            _enableServicesTaskSource = new TaskCompletionSource<object>();
            _enableServicesTaskSource.SetResult(null);
        }

        private void ResetMockCalls()
        {
            _apiManagerMock.ResetCalls();
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

        private void RaiseFlowFinished()
        {
            Mock<IPublishDialog> publishDialogMock = Mock.Get(_mockedPublishDialog);
            publishDialogMock.Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
        }

        private void AssertInitialState()
        {
            Assert.IsNotNull(_objectUnderTest.AsyncAction);
            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsNull(_objectUnderTest.PublishDialog);
            Assert.IsNull(_objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertNoProjectState()
        {
            AssertInvariantsAfterVisible();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsNull(_objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertValidProjectState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.IsTrue(_objectUnderTest.ShowInputControls);
        }

        private void AssertInvalidProjectState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsTrue(_objectUnderTest.NeedsApiEnabled);
            Assert.IsTrue(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertLongRunningValidationState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            Assert.IsFalse(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsTrue(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertErrorInValidationState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
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
            _apiManagerMock.Verify(api => api.AreServicesEnabledAsync(It.IsAny<List<string>>()), times);
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
