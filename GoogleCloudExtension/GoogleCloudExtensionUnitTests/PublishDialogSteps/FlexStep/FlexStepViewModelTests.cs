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
            await GoToValidDefaultState();
            _changedProperties.Clear();
            await TransitionToNeedsAppCreatedTargetSelectCommand();

            SetNeedsAppCreatedTargetStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestNullVersionValidProject()
        {
            await GoToValidDefaultState();
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();

            await GoToVersionState(null);
            SetInvalidVersionStateExpectedValues();

            AssertVersionExpectedState(null);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestNullVersionInvalidProject()
        {
            await GoToInvalidDefaultState();
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
            await GoToValidDefaultState();
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();

            await GoToVersionState(string.Empty);
            SetInvalidVersionStateExpectedValues();

            AssertVersionExpectedState(string.Empty);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestEmptyVersionInvalidProject()
        {
            await GoToInvalidDefaultState();
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
            await GoToValidDefaultState();
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();

            await GoToVersionState(InvalidVersion);
            SetInvalidVersionStateExpectedValues();

            AssertVersionExpectedState(InvalidVersion);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestInvalidVersionInvalidProject()
        {
            await GoToInvalidDefaultState();
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
            await GoToValidDefaultState();
            _changedProperties.Clear();
            SetValidDefaultStateExpectedValues();

            await GoToVersionState(ValidVersion);
            SetValidVersionStateExpectedValues();

            AssertVersionExpectedState(ValidVersion);
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestValidVersionInvalidProject()
        {
            await GoToInvalidDefaultState();
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

        protected override void InitPositiveValidationMocks()
        {
            base.InitPositiveValidationMocks();
            InitGetApplicationMock(_mockedApplication);
        }

        protected override void InitNegativeValidationMocks()
        {
            base.InitNegativeValidationMocks();
            InitGetApplicationMock(null);
        }

        protected override void InitLongRunningValidationMocks()
        {
            base.InitLongRunningValidationMocks();
            InitLongRunningGetApplicationMock();
        }

        protected override void InitErrorValidationMocks()
        {
            base.InitErrorValidationMocks();
            InitErrorGetApplicationMock();
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

        private void InitSetAppRegionMock(bool result)
        {
            _setAppRegionTaskSource = new TaskCompletionSource<bool>();
            _setAppRegionTaskSource.SetResult(result);
        }

        protected override void SetInitialStateExpectedValues()
        {
            base.SetInitialStateExpectedValues();

            _expectedNeedsAppCreated = false;
            _expectedSetAppRegionCommandCanExecute = false;
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

        private async Task TransitionToNeedsAppCreatedTargetSelectCommand()
        {
            Action initMocks = () =>
            {
                InitAreServicesEnabledMock(true);
                InitGetApplicationMock(null);
            };
            await TransitionToProjectSelectCommand(initMocks, s_targetProject);
        }

        protected override async Task RunEnableApiCommandFailure()
        {
            InitGetApplicationMock(_mockedApplication);
            await base.RunEnableApiCommandFailure();
        }

        private async Task RunSetAppRegionCommandSuccess()
        {
            InitPositiveValidationMocks();
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
            base.AssertInitialState();

            Assert.IsNotNull(_objectUnderTest.Version);
            Assert.IsTrue(s_validNamePattern.IsMatch(_objectUnderTest.Version));
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
