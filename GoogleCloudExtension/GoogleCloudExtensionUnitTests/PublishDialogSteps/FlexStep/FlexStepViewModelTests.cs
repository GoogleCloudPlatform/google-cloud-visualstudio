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
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.PublishDialogSteps.FlexStep;
using GoogleCloudExtensionUnitTests.PublishDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.FlexStep
{
    [TestClass]
    public class FlexStepViewModelTests : PublishDialogStepBaseTestsBase<FlexStepViewModel>
    {
        private const string InvalidVersion = "-Invalid Version Name!";
        private const string ValidVersion = "valid-version-name";
        private static readonly Regex s_validNamePattern = new Regex(@"^(?!-)[a-z\d\-]{1,100}$");

        private TaskCompletionSource<Application> _getApplicationTaskSource;
        private Application _mockedApplication;
        private Mock<IGaeDataSource> _gaeDataSourceMock;

        private TaskCompletionSource<bool> _setAppRegionTaskSource;
        private Mock<Func<Task<bool>>> _setAppRegionAsyncFuncMock;

        private bool _expectedNeedsAppCreated;
        private bool _expectedSetAppRegionCommandCanExecute;

        protected override int RequieredAPIsForStep => 1;

        protected override FlexStepViewModel CreateStep()
        {
            return FlexStepViewModel.CreateStep(dataSource: _gaeDataSourceMock.Object, apiManager: _apiManagerMock.Object, pickProjectPrompt: _pickProjectPromptMock.Object, setAppRegionAsyncFunc: _setAppRegionAsyncFuncMock.Object);
        }

        [TestInitialize]
        public override void BeforeEach()
        {
            _gaeDataSourceMock = new Mock<IGaeDataSource>();
            _gaeDataSourceMock.Setup(x => x.GetApplicationAsync()).Returns(() => _getApplicationTaskSource.Task);
            _mockedApplication = Mock.Of<Application>();

            _setAppRegionAsyncFuncMock = new Mock<Func<Task<bool>>>();
            _setAppRegionAsyncFuncMock.Setup(func => func()).Returns(() => _setAppRegionTaskSource.Task);

            base.BeforeEach();
        }

        [TestMethod]
        public async Task TestOnVisibleNeedsApiEnabled()
        {
            await GoToNeedsApiEnabledDefaultState();

            SetInvalidDefaultStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnVisibleNeedsAppCreated()
        {
            await GoToNeedsAppCreatedDefaultState();

            SetNeedsAppCreatedDefaultStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnErrorInNeedsApiEnabled()
        {
            await GoToErrorInNeedsApiEnabledDefaultState();

            SetErrorInValidationDefaultExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnErrorInNeedsAppCreated()
        {
            await GoToErrorInNeedsAppCreatedDefaultProject();

            SetErrorInValidationDefaultExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToNeedsAppCreatedSelectCommmand()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetNeedsAppCreatedTargetStateExpectedValues();

            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestNullVersionValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidVersionStateExpectedValues();

            await GoToVersionState(null);

            AssertVersionExpectedState(null);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestNullVersionInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToVersionState(null);
            SetInvalidVersionStateExpectedValues();

            AssertVersionExpectedState(null);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEmptyVersionValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidVersionStateExpectedValues();

            await GoToVersionState(string.Empty);

            AssertVersionExpectedState(string.Empty);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEmptyVersionInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToVersionState(string.Empty);
            SetInvalidVersionStateExpectedValues();

            AssertVersionExpectedState(string.Empty);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestInvalidVersionValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetInvalidVersionStateExpectedValues();

            await GoToVersionState(InvalidVersion);

            AssertVersionExpectedState(InvalidVersion);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestInvalidVersionInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToVersionState(InvalidVersion);
            SetInvalidVersionStateExpectedValues();

            AssertVersionExpectedState(InvalidVersion);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestValidVersionValidProject()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();
            SetValidVersionStateExpectedValues();

            await GoToVersionState(ValidVersion);

            AssertVersionExpectedState(ValidVersion);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestValidVersionInvalidProject()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(null);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetInvalidDefaultStateExpectedValues();

            await GoToVersionState(ValidVersion);
            SetValidVersionStateExpectedValues();

            AssertVersionExpectedState(ValidVersion);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestSetAppRegionCommandSuccess()
        {
            await GoToNeedsAppCreatedDefaultState();
            _changedProperties.Clear();

            await RunSetAppRegionCommandSuccess();

            SetValidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestSetAppRegionCommandFailure()
        {
            await GoToNeedsAppCreatedDefaultState();
            _changedProperties.Clear();

            await RunSetAppRegionCommandFailure();

            SetNeedsAppCreatedDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        //protected void InitPositiveValidationMocks()
        //{
        //    InitAreServicesEnabledMock(true);
        //    InitGetApplicationMock(_mockedApplication);
        //}

        //protected override void InitNegativeValidationMocks()
        //{
        //    InitAreServicesEnabledMock(false);
        //    InitGetApplicationMock(null);
        //}

        //protected override void InitLongRunningValidationMocks()
        //{
        //    InitLongRunningAreServicesEnabledMock();
        //    InitLongRunningGetApplicationMock();
        //}

        //protected override void InitErrorValidationMocks()
        //{
        //    InitErrorAreServicesEnabledMock();
        //    InitErrorGetApplicationMock();
        //}

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

        private void InitSetAppRegionMock(bool result)
        {
            _setAppRegionTaskSource = new TaskCompletionSource<bool>();
            _setAppRegionTaskSource.SetResult(result);
        }

        protected override void SetNoProjectStateExpectedValues()
        {
            base.SetNoProjectStateExpectedValues();

            _expectedNeedsAppCreated = false;
            _expectedSetAppRegionCommandCanExecute = false;
        }

        protected override void SetValidProjectStateExpectedValues()
        {
            base.SetValidProjectStateExpectedValues();

            _expectedCanPublish = true;

            _expectedNeedsAppCreated = false;
            _expectedSetAppRegionCommandCanExecute = false;
        }

        protected override void SetInvalidProjectStateExpectedValues()
        {
            base.SetInvalidProjectStateExpectedValues();

            _expectedNeedsApiEnabled = true;
            _expectedEnableApiCommandCanExecute = true;
            _expectedShowInputControls = false;

            _expectedNeedsAppCreated = false;
            _expectedSetAppRegionCommandCanExecute = false;
        }

        private void SetNeedsAppCreatedDefaultStateExpectedValues()
        {
            SetInvalidDefaultStateExpectedValues();

            SetNeedsAppCreatedStateExpectedValues();
        }

        private void SetNeedsAppCreatedTargetStateExpectedValues()
        {
            SetInvalidTargetStateExpectedValues();

            SetNeedsAppCreatedStateExpectedValues();
        }

        private void SetNeedsAppCreatedStateExpectedValues()
        {
            _expectedNeedsApiEnabled = false;
            _expectedEnableApiCommandCanExecute = false;
            _expectedNeedsAppCreated = true;
            _expectedSetAppRegionCommandCanExecute = true;
        }

        protected override void SetLongRunningValidationServicesExpectedValues()
        {
            base.SetLongRunningValidationServicesExpectedValues();

            _expectedLoadingProject = true;
            _expectedShowInputControls = false;

            _expectedNeedsAppCreated = false;
            _expectedSetAppRegionCommandCanExecute = false;
        }

        protected override void SetErrorInValidationServicesExpectedValues()
        {
            base.SetErrorInValidationServicesExpectedValues();

            _expectedShowInputControls = false;
            _expectedGeneralError = true;

            _expectedNeedsAppCreated = false;
            _expectedSetAppRegionCommandCanExecute = false;
        }

        private void SetValidVersionStateExpectedValues()
        {
            _expectedInputHasErrors = false;
        }

        private void SetInvalidVersionStateExpectedValues()
        {
            _expectedInputHasErrors = true;
            _expectedCanPublish = false;
        }

        private async Task GoToNeedsApiEnabledDefaultState()
        {
            InitAreServicesEnabledMock(false);
            InitGetApplicationMock(_mockedApplication);
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        private async Task GoToNeedsAppCreatedDefaultState()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        private async Task GoToErrorInNeedsApiEnabledDefaultState()
        {
            InitErrorAreServicesEnabledMock();
            InitGetApplicationMock(_mockedApplication);
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        private async Task GoToErrorInNeedsAppCreatedDefaultProject()
        {
            InitAreServicesEnabledMock(true);
            InitErrorGetApplicationMock();
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        private async Task GoToVersionState(string goToValue)
        {
            _objectUnderTest.Version = goToValue;
            await _objectUnderTest.ValidationDelayTask;
        }

        //protected override async Task RunEnableApiCommandFailure()
        //{
            
        //    InitAreServicesEnabledMock(false);
        //    InitGetApplicationMock(_mockedApplication);
        //    InitEnableApiMock();
        //    await RunEnableApiCommand();
        //    await base.RunEnableApiCommandFailure();
        //}

        private async Task RunSetAppRegionCommandSuccess()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(_mockedApplication);
            await RunSetAppRegionCommand(true);
        }

        private async Task RunSetAppRegionCommandFailure()
        {
            InitAreServicesEnabledMock(true);
            InitGetApplicationMock(null);
            await RunSetAppRegionCommand(false);
        }

        private async Task RunSetAppRegionCommand(bool success)
        {
            InitSetAppRegionMock(success);
            _objectUnderTest.SetAppRegionCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        protected override void AssertInitialState()
        {
            Assert.IsNull(_objectUnderTest.PublishDialog);
            Assert.IsNull(_objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsNotNull(_objectUnderTest.Version);
            Assert.IsTrue(s_validNamePattern.IsMatch(_objectUnderTest.Version));
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertVersionExpectedState(string expected)
        {
            Assert.AreEqual(expected, _objectUnderTest.Version);
        }

        protected override void AssertAgainstExpected()
        {
            base.AssertAgainstExpected();

            Assert.AreEqual(_expectedNeedsAppCreated, _objectUnderTest.NeedsAppCreated);
            Assert.AreEqual(_expectedSetAppRegionCommandCanExecute, _objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
        }
    }
}
