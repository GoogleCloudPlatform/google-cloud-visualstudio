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
        /// Expected values for those properties that might depend on descendant features.
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

            AssertSelectedProjectUnchanged();
            AssertInitialState();
        }

        [TestMethod]
        public void TestInitialStateWithProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnVisibleNoProject()
        {
            SetNoProjectStateExpectedValues();

            await OnVisibleWithProject(null);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnVisiblePositiveValidation()
        {
            InitAreServicesEnabledMock(true);
            SetValidDefaultStateExpectedValues();

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnVisibleNegativeValidation()
        {
            InitAreServicesEnabledMock(false);
            SetInvalidDefaultStateExpectedValues();

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public void TestOnVisibleLongRunningValidation()
        {
            InitLongRunningAreServicesEnabledMock();

            Task onVisibleTask = OnVisibleWithProject(s_defaultProject);

            SetLongRunningValidationDefaultExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnVisibleErrorInValidation()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            SetErrorInValidationDefaultExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromNoToNoExternal()
        {
            await OnVisibleWithProject(null);
            _changedProperties.Clear();
            await OnProjectChangedExternally(null);

            SetNoProjectStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromNoToValidExternal()
        {
            await OnVisibleWithProject(null);
            _changedProperties.Clear();
            InitAreServicesEnabledMock(true);
            await OnProjectChangedExternally(s_targetProject);

            SetValidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromNoToInvalidExternal()
        {
            await OnVisibleWithProject(null);
            _changedProperties.Clear();
            InitAreServicesEnabledMock(false);
            await OnProjectChangedExternally(s_targetProject);

            SetInvalidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToNoExternal()
        {
            InitAreServicesEnabledMock(true);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetNoProjectStateExpectedValues();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToValidExternal()
        {
            InitAreServicesEnabledMock(true);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidTargetStateExpectedValues();

            InitAreServicesEnabledMock(true);
            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToInvalidExternal()
        {
            InitAreServicesEnabledMock(true);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidTargetStateExpectedValues();

            InitAreServicesEnabledMock(false);
            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToNoExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetNoProjectStateExpectedValues();
            await OnProjectChangedExternally(null);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToValidExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidTargetStateExpectedValues();

            InitAreServicesEnabledMock(true);
            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToInvalidExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidTargetStateExpectedValues();

            InitAreServicesEnabledMock(false);
            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromNoToNoSelectCommand()
        {
            await OnVisibleWithProject(null);
            _changedProperties.Clear();
            await OnProjectChangedSelectProjectCommand(null);

            SetNoProjectStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromNoToValidSelectCommand()
        {
            await OnVisibleWithProject(null);
            _changedProperties.Clear();
            InitAreServicesEnabledMock(true);
            await OnProjectChangedSelectProjectCommand(s_targetProject);

            SetValidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromNoToInvalidSelectCommand()
        {
            await OnVisibleWithProject(null);
            _changedProperties.Clear();
            InitAreServicesEnabledMock(false);
            await OnProjectChangedSelectProjectCommand(s_targetProject);

            SetInvalidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToNoSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToValidSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidTargetStateExpectedValues();

            InitAreServicesEnabledMock(true);
            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToInvalidSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidTargetStateExpectedValues();

            InitAreServicesEnabledMock(false);
            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToSameValidSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();

            InitAreServicesEnabledMock(true);
            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToSameInvalidSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            InitAreServicesEnabledMock(false);
            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToNoSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            await OnProjectChangedSelectProjectCommand(null);

            SetInvalidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToValidSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            InitAreServicesEnabledMock(true);
            await OnProjectChangedSelectProjectCommand(s_targetProject);

            SetValidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToInvalidSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            InitAreServicesEnabledMock(false);
            await OnProjectChangedSelectProjectCommand(s_targetProject);

            SetInvalidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToSameValidSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            InitAreServicesEnabledMock(true);
            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            SetValidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromInvalidToSameInvalidSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            InitAreServicesEnabledMock(false);
            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            SetInvalidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToValidSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            InitAreServicesEnabledMock(true);
            await OnProjectChangedSelectProjectCommand(s_targetProject);

            SetValidTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToErrorInValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetErrorInValidationTargetExpectedValues();

            InitErrorAreServicesEnabledMock();
            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToLongRunningValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetLongRunningValidationTargetExpectedValues();

            InitLongRunningAreServicesEnabledMock();
            Task onProjectChanged = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEnableAPIsCommandSuccess()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            InitAreServicesEnabledMock(true);
            InitEnableApiMock();
            await RunEnableApiCommand();

            SetValidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEnableAPIsCommandFailure()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            InitAreServicesEnabledMock(false);
            InitEnableApiMock();
            await RunEnableApiCommand();

            SetInvalidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromValidState()
        {
            InitAreServicesEnabledMock(true);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromValidEventHandling()
        {
            InitAreServicesEnabledMock(true);
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
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromInvalidEventHandling()
        {
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
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromErrorEventHandling()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        //protected virtual void InitPositiveValidationMocks()
        //{
        //    InitAreServicesEnabledMock(true);
        //}

        //protected virtual void InitNegativeValidationMocks()
        //{
        //    InitAreServicesEnabledMock(false);
        //}

        //protected virtual void InitLongRunningValidationMocks()
        //{
        //    InitLongRunningAreServicesEnabledMock();
        //}

        //protected virtual void InitErrorValidationMocks()
        //{
        //    InitErrorAreServicesEnabledMock();
        //}

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

        //protected virtual void SetInitialStateExpectedValues()
        //{
        //    _expectedProjectId = null;
        //    _expectedCanPublish = false;
        //    _expectedLoadingProject = false;
        //    _expectedNeedsApiEnabled = false;
        //    _expectedEnableApiCommandCanExecute = false;
        //    _expectedRequiredApisCount = RequieredAPIsForStep;
        //    _expectedGeneralError = false;
        //    _expectedInputHasErrors = false;
        //    _expectedShowInputControls = false;
        //}

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
            _expectedShowInputControls = false;
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

        protected async Task OnVisibleWithProject(Project project)
        {
            CredentialsStore.Default.UpdateCurrentProject(project);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        protected async Task OnProjectChangedExternally(Project changedTo)
        {
            CredentialsStore.Default.UpdateCurrentProject(changedTo);
            await _objectUnderTest.AsyncAction;
        }

        protected async Task OnProjectChangedSelectProjectCommand(Project changedTo)
        {
            _pickProjectPromptMock.Setup(f => f()).Returns(changedTo);
            _objectUnderTest.SelectProjectCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        protected async Task RunEnableApiCommand()
        {
            _objectUnderTest.EnableApiCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        protected void RaiseFlowFinished()
        {
            Mock<IPublishDialog> publishDialogMock = Mock.Get(_mockedPublishDialog);
            publishDialogMock.Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
        }

        protected virtual void AssertInitialState()
        {
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
        }
    }
}
