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
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

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

        private TestPublishDialogStep _objectUnderTest;
        private Mock<IApiManager> _apiManagerMock;
        private TaskCompletionSource<bool> _areServicesEnabledTaskSource;
        private TaskCompletionSource<object> _enableServicesTaskSource;
        private IPublishDialog _mockedPublishDialog;
        private Mock<Func<Project>> _pickProjectPromptMock;
        private List<string> _changedProperties;

        /// <summary>
        /// A minimal implementation of PublishDialogStepBase.
        /// </summary>
        private class TestPublishDialogStep : PublishDialogStepBase
        {
            public TestPublishDialogStep(IApiManager apiManager, Func<Project> pickProjectPrompt) : base(apiManager, pickProjectPrompt) { }

            public override FrameworkElement Content { get; } = Mock.Of<FrameworkElement>();

            public override IPublishDialogStep Next() => null;

            public override void Publish() { }

            protected internal override IList<string> ApisRequieredForPublishing() => RequiredApis;

            public List<string> RequiredApis { private get; set; } = s_mockedRequiredApis;

            protected override void ClearLoadedProjectData()
            {
                ClearLoadedProjectDataCallCount++;
            }

            public int ClearLoadedProjectDataCallCount { get; private set; } = 0;

            protected override async Task LoadAnyProjectDataAsync()
            {
                LoadAnyProjectDataCallCount++;
                await LoadAnyProjectDataResult.Task;
                LoadAnyProjectDataResult = new TaskCompletionSource<object>();
            }

            public int LoadAnyProjectDataCallCount { get; private set; } = 0;

            public TaskCompletionSource<object> LoadAnyProjectDataResult { get; private set; } =
                new TaskCompletionSource<object>();

            protected override async Task LoadValidProjectDataAsync()
            {
                LoadValidProjectDataAsyncCallCount++;
                await LoadValidProjectDataResult.Task;
                LoadValidProjectDataResult = new TaskCompletionSource<object>();
            }

            public int LoadValidProjectDataAsyncCallCount { get; private set; } = 0;

            public TaskCompletionSource<object> LoadValidProjectDataResult { get; set; } =
                new TaskCompletionSource<object>();

            public new bool IsValidGcpProject => base.IsValidGcpProject;

            protected override async Task InitializeDialogAsync()
            {
                InitializeDialogAsyncCallCount++;
                await InitializeDialogAsyncResult.Task;
                InitializeDialogAsyncResult = new TaskCompletionSource<object>();
            }

            public TaskCompletionSource<object> InitializeDialogAsyncResult { get; private set; } = new TaskCompletionSource<object>();

            public int InitializeDialogAsyncCallCount { get; private set; } = 0;

            public async Task InitializeDialogAsyncBase() => await base.InitializeDialogAsync();

            protected internal override void OnFlowFinished() => OnFlowFinishedCallCount++;

            public void OnFlowFinishedBase() => base.OnFlowFinished();

            public int OnFlowFinishedCallCount { get; private set; } = 0;

            protected override async Task ValidateProjectAsync()
            {
                base.IsValidGcpProject = await ValidateProjectResult.Task;
                ValidateProjectResult = new TaskCompletionSource<bool>();
            }

            public TaskCompletionSource<bool> ValidateProjectResult { get; private set; } =
                new TaskCompletionSource<bool>();

            public Task ValidateProjectAsyncBase()
            {
                return base.ValidateProjectAsync();
            }

            public Task ReloadProjectAsyncBase()
            {
                return base.ReloadProjectAsync();
            }

            public void StartAndTrackBase(Func<Task> func) => base.StartAndTrack(func);
        }

        protected override void BeforeEach()
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _enableServicesTaskSource = new TaskCompletionSource<object>();

            var mockedProject = Mock.Of<IParsedProject>(p => p.Name == VisualStudioProjectName);

            _mockedPublishDialog = Mock.Of<IPublishDialog>(d => d.Project == mockedProject);

            _pickProjectPromptMock = new Mock<Func<Project>>();
            _changedProperties = new List<string>();

            _apiManagerMock = new Mock<IApiManager>();
            _apiManagerMock.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>())).Returns(() => _areServicesEnabledTaskSource.Task);
            _apiManagerMock.Setup(x => x.EnableServicesAsync(It.IsAny<IEnumerable<string>>())).Returns(() => _enableServicesTaskSource.Task);

            _objectUnderTest = new TestPublishDialogStep(_apiManagerMock.Object, _pickProjectPromptMock.Object);
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
        }

        protected override void AfterEach()
        {
            _objectUnderTest.OnFlowFinished();
        }

        [TestMethod]
        public void TestInitialState()
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

        [TestMethod]
        public void TestOnVisibleInitializes()
        {
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            Assert.AreEqual(_mockedPublishDialog, _objectUnderTest.PublishDialog);
            Assert.IsTrue(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
            Assert.AreEqual(1, _objectUnderTest.InitializeDialogAsyncCallCount);
        }

        [TestMethod]
        public void TestOnVisibleAddsFlowFinishedHandler()
        {
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            Mock.Get(_mockedPublishDialog).Raise(pd => pd.FlowFinished += null, _mockedPublishDialog, null);
            CredentialsStore.Default.UpdateCurrentProject(new Project { ProjectId = "new-project-id" });

            Assert.AreEqual(1, _objectUnderTest.OnFlowFinishedCallCount);
        }

        [TestMethod]
        public void TestOnVisibleRemovesOldFlowFinishedHandler()
        {
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            _objectUnderTest.OnVisible(null);
            Mock.Get(_mockedPublishDialog).Raise(pd => pd.FlowFinished += null, _mockedPublishDialog, null);

            Assert.AreEqual(0, _objectUnderTest.OnFlowFinishedCallCount);
        }

        [TestMethod]
        public void TestOnVisibleAddsProjectChangedHandler()
        {
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            Task latestAction = _objectUnderTest.AsyncAction;

            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            Assert.AreNotEqual(latestAction, _objectUnderTest.AsyncAction);
        }

        [TestMethod]
        public void TestOnVisibleRemovesOldProjectChangedHandler()
        {
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            _objectUnderTest.OnVisible(null);
            Task latestAction = _objectUnderTest.AsyncAction;

            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            Assert.AreEqual(latestAction, _objectUnderTest.AsyncAction);
        }

        [TestMethod]
        public async Task TestValidateProjectEmptyProjectIsInvalid()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);

            await _objectUnderTest.ValidateProjectAsyncBase();

            Assert.IsFalse(_objectUnderTest.IsValidGcpProject);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public async Task TestValidateProjectValidProjectWithNoRequiredApis()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.RequiredApis = new List<string>();
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            await _objectUnderTest.ValidateProjectAsyncBase();

            Assert.IsTrue(_objectUnderTest.IsValidGcpProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public void TestValidateProjectValidProjectWithRequiredApisLoading()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.RequiredApis = s_mockedRequiredApis;
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            Task validateTask = _objectUnderTest.ValidateProjectAsyncBase();

            Assert.IsFalse(validateTask.IsCompleted);
            Assert.IsFalse(_objectUnderTest.IsValidGcpProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.AsyncAction.IsCompleted);
        }

        [TestMethod]
        public async Task TestValidateProjectValidProjectWithRequiredApisNotEnabled()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.RequiredApis = s_mockedRequiredApis;
            _areServicesEnabledTaskSource.SetResult(false);
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            await _objectUnderTest.ValidateProjectAsyncBase();

            Assert.IsFalse(_objectUnderTest.IsValidGcpProject);
            Assert.IsTrue(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public async Task TestValidateProjectValidProjectWithRequiredApisEnabled()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.RequiredApis = s_mockedRequiredApis;
            _areServicesEnabledTaskSource.SetResult(true);
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            await _objectUnderTest.ValidateProjectAsyncBase();

            Assert.IsTrue(_objectUnderTest.IsValidGcpProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }


        [TestMethod]
        public void TestReloadProjectAsyncLoading()
        {
            _objectUnderTest.GeneralError = true;

            _objectUnderTest.ReloadProjectAsyncBase();

            Assert.IsTrue(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.AreEqual(1, _objectUnderTest.ClearLoadedProjectDataCallCount);
            Assert.AreEqual(1, _objectUnderTest.LoadAnyProjectDataCallCount);
        }

        [TestMethod]
        public void TestReloadProjectAsyncLoadsValidProjectDataOnValidProject()
        {
            _objectUnderTest.ValidateProjectResult.SetResult(true);
            _objectUnderTest.ReloadProjectAsyncBase();

            Assert.AreEqual(1, _objectUnderTest.LoadValidProjectDataAsyncCallCount);
        }

        [TestMethod]
        public void TestReloadProjectAsyncSkipsLoadValidProjectDataOnInvalidProject()
        {
            _objectUnderTest.ValidateProjectResult.SetResult(false);
            _objectUnderTest.ReloadProjectAsyncBase();

            Assert.AreEqual(0, _objectUnderTest.LoadValidProjectDataAsyncCallCount);
        }

        [TestMethod]
        public async Task TestReloadProjectAsyncDataLoaded()
        {
            _objectUnderTest.ValidateProjectResult.SetResult(false);
            _objectUnderTest.LoadAnyProjectDataResult.SetResult(null);

            await _objectUnderTest.ReloadProjectAsyncBase();

            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.GeneralError);
        }

        [TestMethod]
        public async Task TestReloadProjectAsyncError()
        {
            _objectUnderTest.ValidateProjectResult.SetResult(false);
            _objectUnderTest.LoadAnyProjectDataResult.SetException(new Exception("Test Exception"));

            await _objectUnderTest.ReloadProjectAsyncBase();

            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsTrue(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
        }

        [TestMethod]
        public void TestInitializeDialogAsyncRaisesGcpProjectIdPropertyChanged()
        {
            _objectUnderTest.InitializeDialogAsyncResult.SetResult(null);

            Task initTask = _objectUnderTest.InitializeDialogAsyncBase();

            Assert.IsFalse(initTask.IsCompleted);
            CollectionAssert.Contains(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
        }

        [TestMethod]
        public void TestEnableApisCommandCallsEnableServices()
        {
            _objectUnderTest.RequiredApis = s_mockedRequiredApis;
            _objectUnderTest.EnableApiCommand.Execute(null);

            _apiManagerMock.Verify(api => api.EnableServicesAsync(s_mockedRequiredApis), Times.Once());
        }

        [TestMethod]
        public void TestEnableApisCommandReloadsProject()
        {
            _enableServicesTaskSource.SetResult(null);
            _objectUnderTest.EnableApiCommand.Execute(null);

            // Verify project reload started.
            Assert.IsTrue(_objectUnderTest.LoadingProject);
        }

        [TestMethod]
        public void TestEnableApisCommandFailure()
        {
            _enableServicesTaskSource.SetException(new Exception("Test Exception"));
            _objectUnderTest.EnableApiCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsFaulted);
            // Verify project reload did not start.
            Assert.IsFalse(_objectUnderTest.LoadingProject);
        }

        [TestMethod]
        public void TestOnFlowFinishedResetsProperties()
        {
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            _objectUnderTest.OnFlowFinishedBase();

            Assert.IsNull(_objectUnderTest.PublishDialog);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsFalse(_objectUnderTest.IsValidGcpProject);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
            Assert.AreEqual(1, _objectUnderTest.ClearLoadedProjectDataCallCount);
        }

        [TestMethod]
        public void TestOnFlowFinishedRemovesHandlers()
        {
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            _objectUnderTest.OnFlowFinishedBase();
            Task latestAction = _objectUnderTest.AsyncAction;
            Mock.Get(_mockedPublishDialog).Raise(pd => pd.FlowFinished += null, _mockedPublishDialog, null);
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            Assert.AreEqual(0, _objectUnderTest.OnFlowFinishedCallCount);
            Assert.AreEqual(latestAction, _objectUnderTest.AsyncAction);
        }

        [TestMethod]
        public void TestStartAndTrackSetsAsyncAction()
        {
            Task task = Task.FromResult(true);
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            _objectUnderTest.StartAndTrackBase(() => task);

            Assert.AreEqual(task, _objectUnderTest.AsyncAction);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestStartAndTrackThrowsArgumentNullException()
        {
            _objectUnderTest.StartAndTrackBase(() => null);
        }
    }
}
