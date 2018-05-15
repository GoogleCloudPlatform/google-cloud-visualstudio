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
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.FlexStep;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.FlexStep
{
    [TestClass]
    public class FlexStepViewModelTests : ExtensionTestBase
    {
        private const string DefaultProjectId = "DefaultProjectId";
        private const string VisualStudioProjectName = "VisualStudioProjectName";
        private const string InvalidVersion = "-Invalid Version Name!";
        private const string ValidVersion = "valid-version-name";

        private static readonly Project s_defaultProject = new Project { ProjectId = DefaultProjectId };

        private FlexStepViewModel _objectUnderTest;
        private Mock<IApiManager> _apiManagerMock;
        private Mock<IGaeDataSource> _gaeDataSourceMock;
        private TaskCompletionSource<Application> _getApplicationTaskSource;
        private Application _mockedApplication;
        private Mock<Func<Task<bool>>> _setAppRegionAsyncFuncMock;
        private TaskCompletionSource<bool> _setAppRegionTaskSource;
        private IPublishDialog _mockedPublishDialog;
        private Mock<Func<Project>> _pickProjectPromptMock;

        [ClassInitialize]
        public static void BeforeAll(TestContext context) => GcpPublishStepsUtils.NowOverride = DateTime.Parse("2088-12-23 01:01:01");

        [ClassCleanup]
        public static void AfterAll() => GcpPublishStepsUtils.NowOverride = null;

        protected override void BeforeEach()
        {
            _getApplicationTaskSource = new TaskCompletionSource<Application>();
            _setAppRegionTaskSource = new TaskCompletionSource<bool>();

            _mockedPublishDialog = Mock.Of<IPublishDialog>(pd => pd.Project.Name == VisualStudioProjectName);

            _pickProjectPromptMock = new Mock<Func<Project>>();

            _apiManagerMock = new Mock<IApiManager>();
            _apiManagerMock.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>())).Returns(Task.FromResult(true));

            _gaeDataSourceMock = new Mock<IGaeDataSource>();
            _gaeDataSourceMock.Setup(x => x.GetApplicationAsync()).Returns(() => _getApplicationTaskSource.Task);
            _mockedApplication = Mock.Of<Application>();

            _setAppRegionAsyncFuncMock = new Mock<Func<Task<bool>>>();
            _setAppRegionAsyncFuncMock.Setup(func => func()).Returns(() => _setAppRegionTaskSource.Task);

            _objectUnderTest = FlexStepViewModel.CreateStep(
                dataSource: _gaeDataSourceMock.Object,
                apiManager: _apiManagerMock.Object,
                pickProjectPrompt: _pickProjectPromptMock.Object,
                setAppRegionAsyncFunc: _setAppRegionAsyncFuncMock.Object);
            _objectUnderTest.MillisecondsDelay = 0;
        }

        protected override void AfterEach()
        {
            _objectUnderTest.OnFlowFinished();
        }

        [TestMethod]
        public void TestInitialState()
        {
            Assert.AreEqual(GcpPublishStepsUtils.GetDefaultVersion(), _objectUnderTest.Version);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
            Assert.IsTrue(_objectUnderTest.Promote);
            Assert.IsTrue(_objectUnderTest.OpenWebsite);
            Assert.IsInstanceOfType(_objectUnderTest.Content, typeof(FlexStepContent));
        }

        [TestMethod]
        public async Task TestValidateProjectAsync_NoProjectSetsNeedsAppCreated()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            _objectUnderTest.NeedsAppCreated = true;

            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;

            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            _gaeDataSourceMock.Verify(src => src.GetApplicationAsync(), Times.Never());
        }

        [TestMethod]
        public async Task TestValidateProjectAsync_ErrorInApplicationValidation()
        {
            _getApplicationTaskSource.SetException(new DataSourceException());
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;

            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        [TestMethod]
        public async Task TestValidateProjectAsync_NeedsAppCreated()
        {
            _getApplicationTaskSource.SetResult(null);
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;

            Assert.IsTrue(_objectUnderTest.NeedsAppCreated);
            Assert.IsTrue(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        [TestMethod]
        public async Task TestValidateProjectAsync_Succeeds()
        {
            _objectUnderTest.NeedsAppCreated = true;
            _getApplicationTaskSource.SetResult(_mockedApplication);
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;

            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.CanPublish);
        }

        [TestMethod]
        public void TestSetAppRegionCommand_ExecutesDependency()
        {
            _objectUnderTest.SetAppRegionCommand.Execute(null);

            _setAppRegionAsyncFuncMock.Verify(f => f(), Times.Once());
        }

        [TestMethod]
        public void TestSetAppRegionCommand_BeginsReload()
        {
            _setAppRegionTaskSource.SetResult(true);
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            _objectUnderTest.SetAppRegionCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.LoadingProject);
        }

        [TestMethod]
        public void TestSetAppRegionCommand_SkipsReloadOnSetRegionFailure()
        {
            _setAppRegionTaskSource.SetResult(false);
            _objectUnderTest.SetAppRegionCommand.Execute(null);

            Assert.IsFalse(_objectUnderTest.LoadingProject);
        }

        [TestMethod]
        public void TestSetAppRegionCommand_SkipsReloadOnSetRegionException()
        {
            _setAppRegionTaskSource.SetException(new Exception("test exception"));
            _objectUnderTest.SetAppRegionCommand.Execute(null);

            Assert.IsFalse(_objectUnderTest.LoadingProject);
        }

        [TestMethod]
        public async Task TestSetVersion_Null()
        {
            _objectUnderTest.Version = null;
            await _objectUnderTest.ValidationDelayTask;

            Assert.IsNull(_objectUnderTest.Version);
            Assert.IsTrue(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public async Task TestSetVersion_Empty()
        {
            _objectUnderTest.Version = string.Empty;
            await _objectUnderTest.ValidationDelayTask;

            Assert.AreEqual(string.Empty, _objectUnderTest.Version);
            Assert.IsTrue(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public async Task TestSetVersion_Invalid()
        {
            _objectUnderTest.Version = InvalidVersion;
            await _objectUnderTest.ValidationDelayTask;

            Assert.AreEqual(InvalidVersion, _objectUnderTest.Version);
            Assert.IsTrue(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public async Task TestSetVersion_Valid()
        {
            _objectUnderTest.Version = ValidVersion;
            await _objectUnderTest.ValidationDelayTask;

            Assert.AreEqual(ValidVersion, _objectUnderTest.Version);
            Assert.IsFalse(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public void TestOnFlowFinished_SetsNeedsAppCreatedAndVersion()
        {
            _objectUnderTest.NeedsAppCreated = true;
            _objectUnderTest.Version = null;
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.AreEqual(GcpPublishStepsUtils.GetDefaultVersion(), _objectUnderTest.Version);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestNext_NotSupported()
        {
            _objectUnderTest.Next();
        }
    }
}
