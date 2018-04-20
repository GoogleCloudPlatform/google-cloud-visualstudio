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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.PublishDialogSteps.GceStep;
using GoogleCloudExtensionUnitTests.PublishDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.GceStep
{
    [TestClass]
    public class GceStepViewModelTests : PublishDialogStepBaseTestsBase<GceStepViewModel>
    {
        private const string WindowsLicenseUrl = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2016-dc";

        private static readonly AttachedDisk s_windowsDisk = new AttachedDisk
        {
            Licenses = new string[] { WindowsLicenseUrl },
            Boot = true
        };

        private static readonly IList<Instance> s_mockedInstances = new List<Instance>
        {
            new Instance { Name="Ainstance", Disks = new AttachedDisk[] { s_windowsDisk }, Status = "RUNNING" }
        };

        private static readonly IList<WindowsInstanceCredentials> s_mockedInstanceCredentials = new List<WindowsInstanceCredentials>
        {
            new WindowsInstanceCredentials { User="User1", Password="Password1"}
        };

        private TaskCompletionSource<IList<Instance>> _getInstanceListTaskSource;
        private Mock<IGceDataSource> _dataSourceMock;

        private Mock<IWindowsCredentialsStore> _windowsCredentialStoreMock;

        private Mock<Action<Instance>> _manageCredentialsPromptMock;

        private IEnumerable<Instance> _expectedInstances;
        private Instance _expectedSelectedInstance;
        private IEnumerable<WindowsInstanceCredentials> _expectedCredentials;
        private WindowsInstanceCredentials _expectedSelectedCredentials;
        private bool _expectedManageCredentialsCommandCanExecute;

        protected override int RequieredAPIsForStep => 1;

        protected override GceStepViewModel CreateStep()
        {
            return GceStepViewModel.CreateStep(
                apiManager: _apiManagerMock.Object,
                pickProjectPrompt: _pickProjectPromptMock.Object,
                dataSource: _dataSourceMock.Object,
                currentWindowsCredentialStore: _windowsCredentialStoreMock.Object,
                manageCredentialsPrompt: _manageCredentialsPromptMock.Object);
        }

        [TestInitialize]
        public override void BeforeEach()
        {
            _dataSourceMock = new Mock<IGceDataSource>();
            _dataSourceMock.Setup(ds => ds.GetInstanceListAsync()).Returns(() => _getInstanceListTaskSource.Task);

            _windowsCredentialStoreMock = new Mock<IWindowsCredentialsStore>();

            _manageCredentialsPromptMock = new Mock<Action<Instance>>();

            base.BeforeEach();
        }

        [TestMethod]
        public async Task TestOnVisibleNoInstances()
        {
            await GoToNoInstancesDefaultState();

            SetNoInstancesDefaultStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnVisibleNoCredentials()
        {
            await GoToNoCredentialsDefaultState();

            SetNoCredentialsDefaultStateExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public void TestOnVisibleLongRunningLoadingInstances()
        {
            GoToLongRunningLoadingngInstancesDefaultState();

            SetLongRunningValidationDefaultExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestOnVisibleErrorLoadingInstances()
        {
            await GoToErrorLoadingngInstancesDefaultState();

            SetErrorInValidationDefaultExpectedValues();

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToNoInstancesSelectCommmand()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstanceCredentials);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetNoInstancesTargetStateExpectedValues();

            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(new List<Instance>());
            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestFromValidToNoCredentialsSelectCommmand()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstanceCredentials);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();
            SetNoCredentialsTargetStateExpectedValues();

            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(new List<WindowsInstanceCredentials>());
            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestManageCredentialsCommandSuccess()
        {
            await GoToNoCredentialsDefaultState();
            _changedProperties.Clear();

            RunManageCredentialsCommandSuccess();

            SetValidDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        [TestMethod]
        public async Task TestManageCredentialsCommandFailure()
        {
            await GoToNoCredentialsDefaultState();
            _changedProperties.Clear();

            RunManageCredentialsCommandFailure();

            SetNoCredentialsDefaultStateExpectedValues();

            AssertSelectedProjectUnchanged();
            AssertExpectedVisibleState();
        }

        //protected override void InitPositiveValidationMocks()
        //{
        //    InitAreServicesEnabledMock(true);
        //    InitGetInstanceListMock(s_mockedInstances);
        //    InitGetCredentialsForInstanceMock(s_mockedInstanceCredentials);
        //}

        //protected override void InitNegativeValidationMocks()
        //{
        //    InitAreServicesEnabledMock(false);
        //    InitGetInstanceListMock(s_mockedInstances);
        //    InitGetCredentialsForInstanceMock(s_mockedInstanceCredentials);
        //}

        //protected override void InitLongRunningValidationMocks()
        //{
        //    InitLongRunningAreServicesEnabledMock();
        //    InitLongRunningGetInstanceListMock();
        //}

        //protected override void InitErrorValidationMocks()
        //{
        //    InitErrorAreServicesEnabledMock();
        //    InitErrorGetInstanceListMock();
        //}

        private void InitGetInstanceListMock(IList<Instance> result)
        {
            _getInstanceListTaskSource = new TaskCompletionSource<IList<Instance>>();
            _getInstanceListTaskSource.SetResult(result);
        }

        private void InitLongRunningGetInstanceListMock()
        {
            _getInstanceListTaskSource = new TaskCompletionSource<IList<Instance>>();
        }

        private void InitErrorGetInstanceListMock()
        {
            _getInstanceListTaskSource = new TaskCompletionSource<IList<Instance>>();
            _getInstanceListTaskSource.SetException(new DataSourceException());
        }

        private void InitGetCredentialsForInstanceMock(IList<WindowsInstanceCredentials> result)
        {
            _windowsCredentialStoreMock.Setup(s => s.GetCredentialsForInstance(It.IsAny<Instance>())).Returns(result);
        }

        protected void SetInitialStateExpectedValues()
        {
            Assert.IsNull(_objectUnderTest.PublishDialog);
            Assert.IsNull(_objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            CollectionAssert.AreEquivalent(Enumerable.Empty<Instance>().ToList(), _objectUnderTest.Instances.ToList());
            Assert.IsNull(_objectUnderTest.SelectedInstance);
            CollectionAssert.AreEquivalent(Enumerable.Empty<WindowsInstanceCredentials>().ToList(), _objectUnderTest.Credentials.ToList());
            Assert.IsNull(_objectUnderTest.SelectedCredentials);
            Assert.IsFalse(_objectUnderTest.ManageCredentialsCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        protected override void SetNoProjectStateExpectedValues()
        {
            base.SetNoProjectStateExpectedValues();

            _expectedInstances = Enumerable.Empty<Instance>();
            _expectedSelectedInstance = null;
            _expectedCredentials = Enumerable.Empty<WindowsInstanceCredentials>();
            _expectedSelectedCredentials = null;
            _expectedManageCredentialsCommandCanExecute = false;
        }

        protected override void SetValidProjectStateExpectedValues()
        {
            base.SetValidProjectStateExpectedValues();

            _expectedCanPublish = true;
            _expectedInstances = s_mockedInstances;
            _expectedSelectedInstance = s_mockedInstances[0];
            _expectedCredentials = s_mockedInstanceCredentials;
            _expectedSelectedCredentials = s_mockedInstanceCredentials[0];
            _expectedManageCredentialsCommandCanExecute = true;
        }

        protected override void SetInvalidProjectStateExpectedValues()
        {
            base.SetInvalidProjectStateExpectedValues();

            _expectedNeedsApiEnabled = true;
            _expectedEnableApiCommandCanExecute = true;
            _expectedShowInputControls = false;

            _expectedInstances = Enumerable.Empty<Instance>();
            _expectedSelectedInstance = null;
            _expectedCredentials = Enumerable.Empty<WindowsInstanceCredentials>();
            _expectedSelectedCredentials = null;
            _expectedManageCredentialsCommandCanExecute = false;
        }

        protected override void SetLongRunningValidationServicesExpectedValues()
        {
            base.SetLongRunningValidationServicesExpectedValues();

            _expectedLoadingProject = true;
            _expectedShowInputControls = false;

            _expectedInstances = Enumerable.Empty<Instance>();
            _expectedSelectedInstance = null;
            _expectedCredentials = Enumerable.Empty<WindowsInstanceCredentials>();
            _expectedSelectedCredentials = null;
            _expectedManageCredentialsCommandCanExecute = false;
        }

        protected override void SetErrorInValidationServicesExpectedValues()
        {
            base.SetErrorInValidationServicesExpectedValues();

            _expectedShowInputControls = false;
            _expectedGeneralError = true;

            _expectedInstances = Enumerable.Empty<Instance>();
            _expectedSelectedInstance = null;
            _expectedCredentials = Enumerable.Empty<WindowsInstanceCredentials>();
            _expectedSelectedCredentials = null;
            _expectedManageCredentialsCommandCanExecute = false;
        }

        private void SetNoInstancesDefaultStateExpectedValues()
        {
            SetValidDefaultStateExpectedValues();

            SetNoInstancesStateExpectedValues();
        }

        private void SetNoInstancesTargetStateExpectedValues()
        {
            SetValidTargetStateExpectedValues();

            SetNoInstancesStateExpectedValues();
        }

        private void SetNoInstancesStateExpectedValues()
        {
            _expectedCanPublish = false;

            _expectedInstances = Enumerable.Empty<Instance>();
            _expectedSelectedInstance = null;
            _expectedCredentials = Enumerable.Empty<WindowsInstanceCredentials>();
            _expectedSelectedCredentials = null;
            _expectedManageCredentialsCommandCanExecute = false;
        }

        private void SetNoCredentialsDefaultStateExpectedValues()
        {
            SetValidDefaultStateExpectedValues();

            SetNoCredentialsStateExpectedValues();
        }

        private void SetNoCredentialsTargetStateExpectedValues()
        {
            SetValidTargetStateExpectedValues();

            SetNoCredentialsStateExpectedValues();
        }

        private void SetNoCredentialsStateExpectedValues()
        {
            _expectedCanPublish = false;

            _expectedCredentials = Enumerable.Empty<WindowsInstanceCredentials>();
            _expectedSelectedCredentials = null;
            _expectedManageCredentialsCommandCanExecute = true;
        }

        private async Task GoToNoInstancesDefaultState()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(new List<Instance>());
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        private async Task GoToNoCredentialsDefaultState()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(new List<WindowsInstanceCredentials>());
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        private void GoToLongRunningLoadingngInstancesDefaultState()
        {
            InitAreServicesEnabledMock(true);
            InitLongRunningGetInstanceListMock();
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
        }

        private async Task GoToErrorLoadingngInstancesDefaultState()
        {
            InitAreServicesEnabledMock(true);
            InitErrorGetInstanceListMock();
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        //protected override async Task RunEnableApiCommandFailure()
        //{
        //    InitGetInstanceListMock(s_mockedInstances);
        //    InitGetCredentialsForInstanceMock(s_mockedInstanceCredentials);
        //    InitAreServicesEnabledMock(false);
            
        //    InitEnableApiMock();
        //    await RunEnableApiCommand();
        //    await base.RunEnableApiCommandFailure();
        //}

        private void RunManageCredentialsCommandSuccess()
        {
            InitGetCredentialsForInstanceMock(s_mockedInstanceCredentials);
            RunManageCredentialsCommand();
        }

        private void RunManageCredentialsCommandFailure()
        {
            InitGetCredentialsForInstanceMock(new List<WindowsInstanceCredentials>());
            RunManageCredentialsCommand();
        }

        private void RunManageCredentialsCommand()
        {
            _manageCredentialsPromptMock.Setup(p => p(It.IsAny<Instance>()));
            _objectUnderTest.ManageCredentialsCommand.Execute(null);
        }

        protected override void AssertAgainstExpected()
        {
            base.AssertAgainstExpected();

            CollectionAssert.AreEquivalent(_expectedInstances.ToList(), _objectUnderTest.Instances.ToList());
            Assert.AreEqual(_expectedSelectedInstance, _objectUnderTest.SelectedInstance);
            CollectionAssert.AreEquivalent(_expectedCredentials.ToList(), _objectUnderTest.Credentials.ToList());
            Assert.AreEqual(_expectedSelectedCredentials, _objectUnderTest.SelectedCredentials);
            Assert.AreEqual(_expectedManageCredentialsCommandCanExecute, _objectUnderTest.ManageCredentialsCommand.CanExecuteCommand);
        }
    }
}
