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
    public abstract class PublishDialogStepBaseTestsBase<TStep>
        where TStep : PublishDialogStepBase
    {
        protected const string DefaultProjectId = "DefaultProjectId";
        protected const string TargetProjectId = "TargetProjectId";
        protected const string VisualStudioProjectName = "VisualStudioProjectName";

        protected static readonly Project s_targetProject = new Project { ProjectId = TargetProjectId };
        protected static readonly Project s_defaultProject = new Project { ProjectId = DefaultProjectId };

        protected TStep _objectUnderTest;
        protected Mock<IApiManager> _apiManagerMock;
        protected TaskCompletionSource<bool> _areServicesEnabledTaskSource;
        protected TaskCompletionSource<object> _enableServicesTaskSource;
        protected IPublishDialog _mockedPublishDialog;
        protected Mock<Func<Project>> _pickProjectPromptMock;
        protected List<string> _changedProperties;

        /// <summary>
        /// Expected values for those properties that might depend on descendand features.
        /// </summary>
        protected string _expectedProjectId;
        protected bool _expectedCanPublish;
        protected bool _expectedLoadingProject;
        protected bool _expectedNeedsApiEnabled;
        protected bool _expectedEnableApiCommandCanExecute;
        protected int _expectedRequiredApisCount;
        protected bool _expectedShowInputControls;
        protected bool _expectedGeneralError;
        protected bool _expectedInputHasErrors;

        protected abstract int RequieredAPIsForStep { get; }

        protected abstract TStep CreateStep();

        [TestInitialize]
        public virtual void BeforeEach()
        {
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

            _objectUnderTest = CreateStep();
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
        }

        [TestCleanup]
        public void AfterEach()
        {
            _objectUnderTest.OnFlowFinished();
        }

        [TestMethod]
        public void TestInitialStateNoProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            SetInitialStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertInitialState();
        }

        [TestMethod]
        public void TestInitialStateWithProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            SetInitialStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnVisibleNoProject()
        {
            await GoToNoProjectState();

            SetNoProjectStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnVisiblePositiveValidation()
        {
            await GoToValidDefaultState();

            SetValidDefaultStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnVisibleNegativeValidation()
        {
            await GoToInvalidDefaultState();

            SetInvalidDefaultStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public void TestOnVisibleLongRunningValidation()
        {
            GoToLongRunningValidationDefaultState();

            SetLongRunningValidationDefaultExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnVisibleErrorInValidation()
        {
            await GoToErrorInValidationDefaultState();

            SetErrorInValidationDefaultExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromNoToNoExternal()
        {
            await GoToNoProjectState();
            _changedProperties.Clear();
            await TransitionToNoProjectExternal();

            SetNoProjectStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromNoToValidExternal()
        {
            await GoToNoProjectState();
            _changedProperties.Clear();
            await TransitionToValidTargetExternal();

            SetValidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromNoToInvalidExternal()
        {
            await GoToNoProjectState();
            _changedProperties.Clear();
            await TransitionToInvalidTargetExternal();

            SetInvalidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToNoExternal()
        {
            await GoToValidDefaultState();
            _changedProperties.Clear();
            await TransitionToNoProjectExternal();

            SetNoProjectStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToValidExternal()
        {
            await GoToValidDefaultState();
            _changedProperties.Clear();
            await TransitionToValidTargetExternal();

            SetValidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToInvalidExternal()
        {
            await GoToValidDefaultState();
            _changedProperties.Clear();
            await TransitionToInvalidTargetExternal();

            SetInvalidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToNoExternal()
        {
            await GoToInvalidDefaultState();
            _changedProperties.Clear();
            await TransitionToNoProjectExternal();

            SetNoProjectStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToValidExternal()
        {
            await GoToInvalidDefaultState();
            _changedProperties.Clear();
            await TransitionToValidTargetExternal();

            SetValidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToInvalidExternal()
        {
            await GoToInvalidDefaultState();
            _changedProperties.Clear();
            await TransitionToInvalidTargetExternal();

            SetInvalidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromNoToNoSelectCommand()
        {
            await GoToNoProjectState();
            _changedProperties.Clear();
            await TransitionToNoProjectSelectCommand();

            SetNoProjectStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromNoToValidSelectCommand()
        {
            await GoToNoProjectState();
            _changedProperties.Clear();
            await TransitionToValidTargetSelectCommand();

            SetValidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromNoToInvalidSelectCommand()
        {
            await GoToNoProjectState();
            _changedProperties.Clear();
            await TransitionToInvalidTargetSelectCommand();

            SetInvalidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToNoSelectCommand()
        {
            await GoToValidDefaultState();
            _changedProperties.Clear();
            await TransitionToNoProjectSelectCommand();

            SetValidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToValidSelectCommand()
        {
            await GoToValidDefaultState();
            _changedProperties.Clear();
            await TransitionToValidTargetSelectCommand();

            SetValidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToInvalidSelectCommand()
        {
            await GoToValidDefaultState();
            _changedProperties.Clear();
            await TransitionToInvalidTargetSelectCommand();

            SetInvalidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToSameValidSelectCommand()
        {
            await GoToValidDefaultState();
            _changedProperties.Clear();
            await TransitionToValidDefaultSelectCommand();

            SetValidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToSameInvalidSelectCommand()
        {
            await GoToValidDefaultState();
            _changedProperties.Clear();
            await TransitionToInvalidDefaultSelectCommand();

            SetInvalidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToNoSelectCommand()
        {
            await GoToInvalidDefaultState();
            _changedProperties.Clear();
            await TransitionToNoProjectSelectCommand();

            SetInvalidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToValidSelectCommand()
        {
            await GoToInvalidDefaultState();
            _changedProperties.Clear();
            await TransitionToValidTargetSelectCommand();

            SetValidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToInvalidSelectCommand()
        {
            await GoToInvalidDefaultState();
            _changedProperties.Clear();
            await TransitionToInvalidTargetSelectCommand();

            SetInvalidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToSameValidSelectCommand()
        {
            await GoToInvalidDefaultState();
            _changedProperties.Clear();
            await TransitionToValidDefaultSelectCommand();

            SetValidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToSameInvalidSelectCommand()
        {
            await GoToInvalidDefaultState();
            _changedProperties.Clear();
            await TransitionToInvalidDefaultSelectCommand();

            SetInvalidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToValidSelectCommand()
        {
            await GoToErrorInValidationDefaultState();
            _changedProperties.Clear();
            await TransitionToValidTargetSelectCommand();

            SetValidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToErrorInValidationSelectCommand()
        {
            await GoToValidDefaultState();
            _changedProperties.Clear();
            await TransitionToErrorInValidationTargetSelectCommand();

            SetErrorInValidationTargetExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToLongRunningValidationSelectCommand()
        {
            await GoToValidDefaultState();
            _changedProperties.Clear();
            TransitionToLongRunningValidationTargetSelectCommand();

            SetLongRunningValidationTargetExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEnableAPIsCommandSuccess()
        {
            await GoToInvalidDefaultState();
            _changedProperties.Clear();

            await RunEnableApiCommandSuccess();

            SetValidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEnableAPIsCommandFailure()
        {
            await GoToInvalidDefaultState();
            _changedProperties.Clear();

            await RunEnableApiCommandFailure();

            SetInvalidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromValid()
        {
            await GoToValidDefaultState();
            _changedProperties.Clear();
            GoToFlowFinishedState();

            SetInitialStateExpectedValues();

            AssertInitialState();

            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);
            AssertSelectedProjectUnchanged();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromInvalid()
        {
            await GoToInvalidDefaultState();
            _changedProperties.Clear();
            GoToFlowFinishedState();

            SetInitialStateExpectedValues();

            AssertInitialState();

            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);
            AssertSelectedProjectUnchanged();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromError()
        {
            await GoToErrorInValidationDefaultState();
            _changedProperties.Clear();
            GoToFlowFinishedState();

            SetInitialStateExpectedValues();

            AssertInitialState();

            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);
            AssertSelectedProjectUnchanged();
        }

        protected virtual void InitPositiveValidationMocks()
        {
            InitAreServicesEnabledMock(true);
        }

        protected virtual void InitNegativeValidationMocks()
        {
            InitAreServicesEnabledMock(false);
        }

        protected virtual void InitLongRunningValidationMocks()
        {
            InitLongRunningAreServicesEnabledMock();
        }

        protected virtual void InitErrorValidationMocks()
        {
            InitErrorAreServicesEnabledMock();
        }

        protected void InitAreServicesEnabledMock(bool servicesEnabled)
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _areServicesEnabledTaskSource.SetResult(servicesEnabled);
        }

        protected void InitLongRunningAreServicesEnabledMock()
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
        }

        protected void InitErrorAreServicesEnabledMock()
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _areServicesEnabledTaskSource.SetException(new DataSourceException());
        }

        protected void InitEnableApiMock()
        {
            _enableServicesTaskSource = new TaskCompletionSource<object>();
            _enableServicesTaskSource.SetResult(null);
        }

        protected virtual void SetInitialStateExpectedValues()
        {
            _expectedProjectId = null;
            _expectedCanPublish = false;
            _expectedLoadingProject = false;
            _expectedNeedsApiEnabled = false;
            _expectedEnableApiCommandCanExecute = false;
            _expectedRequiredApisCount = RequieredAPIsForStep;
            _expectedGeneralError = false;
            _expectedInputHasErrors = false;
            _expectedShowInputControls = false;
        }

        protected virtual void SetNoProjectStateExpectedValues()
        {
            _expectedProjectId = null;
            _expectedCanPublish = false;
            _expectedLoadingProject = false;
            _expectedNeedsApiEnabled = false;
            _expectedEnableApiCommandCanExecute = false;
            _expectedRequiredApisCount = RequieredAPIsForStep;
            _expectedGeneralError = false;
            _expectedInputHasErrors = false;
            _expectedShowInputControls = true;
        }

        protected void SetValidDefaultStateExpectedValues()
        {
            _expectedProjectId = DefaultProjectId;
            SetValidProjectStateExpectedValues();
        }

        protected void SetValidTargetStateExpectedValues()
        {
            _expectedProjectId = TargetProjectId;
            SetValidProjectStateExpectedValues();
        }

        protected virtual void SetValidProjectStateExpectedValues()
        {
            _expectedCanPublish = false;
            _expectedLoadingProject = false;
            _expectedNeedsApiEnabled = false;
            _expectedEnableApiCommandCanExecute = false;
            _expectedRequiredApisCount = RequieredAPIsForStep;
            _expectedGeneralError = false;
            _expectedInputHasErrors = false;
            _expectedShowInputControls = true;
        }

        protected void SetInvalidDefaultStateExpectedValues()
        {
            _expectedProjectId = DefaultProjectId;
            SetInvalidProjectStateExpectedValues();
        }

        protected void SetInvalidTargetStateExpectedValues()
        {
            _expectedProjectId = TargetProjectId;
            SetInvalidProjectStateExpectedValues();
        }

        protected virtual void SetInvalidProjectStateExpectedValues()
        {
            _expectedCanPublish = false;
            _expectedLoadingProject = false;
            _expectedNeedsApiEnabled = false;
            _expectedEnableApiCommandCanExecute = false;
            _expectedRequiredApisCount = RequieredAPIsForStep;
            _expectedGeneralError = false;
            _expectedInputHasErrors = false;
            _expectedShowInputControls = true;
        }

        protected void SetLongRunningValidationDefaultExpectedValues()
        {
            _expectedProjectId = DefaultProjectId;
            SetLongRunningValidationServicesExpectedValues();
        }

        protected void SetLongRunningValidationTargetExpectedValues()
        {
            _expectedProjectId = TargetProjectId;
            SetLongRunningValidationServicesExpectedValues();
        }

        protected virtual void SetLongRunningValidationServicesExpectedValues()
        {
            _expectedCanPublish = false;
            _expectedLoadingProject = false;
            _expectedNeedsApiEnabled = false;
            _expectedEnableApiCommandCanExecute = false;
            _expectedRequiredApisCount = RequieredAPIsForStep;
            _expectedGeneralError = false;
            _expectedInputHasErrors = false;
            _expectedShowInputControls = true;
        }

        protected void SetErrorInValidationDefaultExpectedValues()
        {
            _expectedProjectId = DefaultProjectId;
            SetErrorInValidationServicesExpectedValues();
        }

        protected void SetErrorInValidationTargetExpectedValues()
        {
            _expectedProjectId = TargetProjectId;
            SetErrorInValidationServicesExpectedValues();
        }

        protected virtual void SetErrorInValidationServicesExpectedValues()
        {
            _expectedCanPublish = false;
            _expectedLoadingProject = false;
            _expectedNeedsApiEnabled = false;
            _expectedEnableApiCommandCanExecute = false;
            _expectedRequiredApisCount = RequieredAPIsForStep;
            _expectedGeneralError = false;
            _expectedInputHasErrors = false;
            _expectedShowInputControls = true;
        }

        protected async Task GoToNoProjectState()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        protected async Task GoToValidDefaultState()
        {
            InitPositiveValidationMocks();
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        protected async Task GoToInvalidDefaultState()
        {
            InitNegativeValidationMocks();
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        protected void GoToLongRunningValidationDefaultState()
        {
            InitLongRunningValidationMocks();
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            // Not awaiting here, this potentially won't finish and we want to check
            // the loading project state.
        }

        protected async Task GoToErrorInValidationDefaultState()
        {
            InitErrorValidationMocks();
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;

        }

        protected void GoToFlowFinishedState()
        {
            _changedProperties.Clear();
            Mock<IPublishDialog> publishDialogMock = Mock.Get(_mockedPublishDialog);
            publishDialogMock.Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
        }

        protected async Task TransitionToNoProjectExternal()
        {
            await TransitionToProjectExternal(() => { }, null);
        }

        protected async Task TransitionToValidTargetExternal()
        {
            await TransitionToProjectExternal(InitPositiveValidationMocks, s_targetProject);
        }

        protected async Task TransitionToInvalidTargetExternal()
        {
            await TransitionToProjectExternal(InitNegativeValidationMocks, s_targetProject);
        }

        protected async Task TransitionToProjectExternal(Action initMocks, Project project)
        {
            initMocks();
            CredentialsStore.Default.UpdateCurrentProject(project);
            await _objectUnderTest.AsyncAction;
        }

        protected async Task TransitionToNoProjectSelectCommand()
        {
            await TransitionToProjectSelectCommand(() => { }, null);
        }

        protected async Task TransitionToValidDefaultSelectCommand()
        {
            await TransitionToProjectSelectCommand(InitPositiveValidationMocks, s_defaultProject);
        }

        protected async Task TransitionToValidTargetSelectCommand()
        {
            await TransitionToProjectSelectCommand(InitPositiveValidationMocks, s_targetProject);
        }

        protected async Task TransitionToInvalidDefaultSelectCommand()
        {
            await TransitionToProjectSelectCommand(InitNegativeValidationMocks, s_defaultProject);
        }

        protected async Task TransitionToInvalidTargetSelectCommand()
        {
            await TransitionToProjectSelectCommand(InitNegativeValidationMocks, s_targetProject);
        }

        protected async Task TransitionToErrorInValidationTargetSelectCommand()
        {
            await TransitionToProjectSelectCommand(InitErrorValidationMocks, s_targetProject);
        }

        protected void TransitionToLongRunningValidationTargetSelectCommand()
        {
            InitLongRunningValidationMocks();
            _pickProjectPromptMock.Setup(f => f()).Returns(s_targetProject);
            _objectUnderTest.SelectProjectCommand.Execute(null);
            // Not awaiting here, this potentially won't finish and we want to check
            // the loading project state.
        }

        protected async Task TransitionToProjectSelectCommand(Action initMocks, Project project)
        {
            initMocks();
            _pickProjectPromptMock.Setup(f => f()).Returns(project);
            _objectUnderTest.SelectProjectCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        protected async Task RunEnableApiCommandSuccess()
        {
            InitPositiveValidationMocks();
            await RunEnableApiCommand();
        }

        protected virtual async Task RunEnableApiCommandFailure()
        {
            InitAreServicesEnabledMock(false);
            await RunEnableApiCommand();
        }

        protected async Task RunEnableApiCommand()
        {
            InitEnableApiMock();
            _objectUnderTest.EnableApiCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        protected virtual void AssertInitialState()
        {
            Assert.IsNull(_objectUnderTest.PublishDialog);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsFalse(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);

            AssertAgainstExpected();
        }

        protected void AssertExpectedVisibleState()
        {
            AssertInvariantsAfterVisible();
            AssertAgainstExpected();
        }

        protected void AssertSelectedProjectChanged()
        {
            CollectionAssert.Contains(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
        }

        protected void AssertSelectedProjectUnchanged()
        {
            CollectionAssert.DoesNotContain(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
        }

        protected void AssertInvariantsAfterVisible()
        {
            Assert.AreEqual(_mockedPublishDialog, _objectUnderTest.PublishDialog);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsTrue(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
        }

        protected virtual void AssertAgainstExpected()
        {
            Assert.AreEqual(_expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.AreEqual(_expectedCanPublish, _objectUnderTest.CanPublish);
            Assert.AreEqual(_expectedLoadingProject, _objectUnderTest.LoadingProject);
            Assert.AreEqual(_expectedNeedsApiEnabled, _objectUnderTest.NeedsApiEnabled);
            Assert.AreEqual(_expectedEnableApiCommandCanExecute, _objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.AreEqual(_expectedInputHasErrors, _objectUnderTest.HasErrors);
            Assert.AreEqual(_expectedGeneralError, _objectUnderTest.GeneralError);
            Assert.AreEqual(_expectedShowInputControls, _objectUnderTest.ShowInputControls);
            Assert.AreEqual(_expectedRequiredApisCount, _objectUnderTest.RequiredApis?.Count);
        }
    }
}
