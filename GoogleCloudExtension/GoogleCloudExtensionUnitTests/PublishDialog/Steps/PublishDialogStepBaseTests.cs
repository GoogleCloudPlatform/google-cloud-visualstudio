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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps;
using GoogleCloudExtension.Services.VsProject;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DteProject = EnvDTE.Project;
using GcpProject = Google.Apis.CloudResourceManager.v1.Data.Project;

namespace GoogleCloudExtensionUnitTests.PublishDialog.Steps
{
    [TestClass]
    public class PublishDialogStepBaseTests : ExtensionTestBase
    {
        private const string ValidProjectId = "valid-project-id";
        private const string VisualStudioProjectName = "VisualStudioProjectName";
        private const string SavedConfiguration = "SavedConfiguration";
        private const string Configuration = "SomeConfiguration";

        private static readonly List<string> s_mockedRequiredApis = new List<string> { "OneRequieredApi", "AnotherRequiredApi" };

        private TestPublishDialogStep _objectUnderTest;
        private Mock<IApiManager> _apiManagerMock;
        private TaskCompletionSource<bool> _areServicesEnabledTaskSource;
        private TaskCompletionSource<object> _enableServicesTaskSource;
        private IPublishDialog _mockedPublishDialog;
        private Mock<Func<GcpProject>> _pickProjectPromptMock;
        private List<string> _changedProperties;
        private static readonly List<string> s_configurations = new List<string> { "Debug", "Release" };
        private DteProject _mockedProject;
        private Mock<IVsProjectPropertyService> _propertyServiceMock;

        /// <summary>
        /// A minimal implementation of PublishDialogStepBase. Most functions faked with counter to record call counts.
        /// </summary>
        private class TestPublishDialogStep : PublishDialogStepBase
        {
            public TestPublishDialogStep(
                Func<GcpProject> pickProjectPrompt,
                IPublishDialog publishDialog) : base(
                publishDialog,
                pickProjectPrompt)
            {
                PublishCommandAsync =
                    new Mock<ProtectedAsyncCommand>(Mock.Of<Func<Task>>(), false).Object;
            }

            protected override IList<string> RequiredApis => RequiredApisOverride;
            public List<string> RequiredApisOverride { private get; set; } = s_mockedRequiredApis;

            protected override void ClearLoadedProjectData() => ClearLoadedProjectDataCallCount++;

            public int ClearLoadedProjectDataCallCount { get; set; } = 0;

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

            public TaskCompletionSource<object> LoadValidProjectDataResult { get; private set; } =
                new TaskCompletionSource<object>();

            public new bool IsValidGcpProject
            {
                get => base.IsValidGcpProject;
                set => base.IsValidGcpProject = value;
            }

            protected internal override ProtectedAsyncCommand PublishCommandAsync { get; }

            protected internal override void OnFlowFinished() => OnFlowFinishedCallCount++;

            protected override void LoadProjectProperties() => LoadPropertiesCallCount++;

            public int LoadPropertiesCallCount { get; private set; }

            protected override void SaveProjectProperties() => SavePropertiesCallCount++;

            public int SavePropertiesCallCount { get; private set; }

            public void OnFlowFinishedBase() => base.OnFlowFinished();

            public int OnFlowFinishedCallCount { get; private set; } = 0;

            protected override async Task ValidateProjectAsync()
            {
                base.IsValidGcpProject = await ValidateProjectResult.Task;
                ValidateProjectResult = new TaskCompletionSource<bool>();
            }

            public TaskCompletionSource<bool> ValidateProjectResult { get; private set; } =
                new TaskCompletionSource<bool>();

            public Task ValidateProjectAsyncBase() => base.ValidateProjectAsync();

            protected override void OnIsValidGcpProjectChanged() => OnIsValidGcpProjectChangedCallCount++;

            public int OnIsValidGcpProjectChangedCallCount { get; set; }

            public Task LoadProjectAsync()
            {
                LoadProject();
                return LoadProjectTask.SafeTask;
            }
        }

        protected override void BeforeEach()
        {
            _propertyServiceMock = Mock.Get(PackageMock.Object.GetMefService<IVsProjectPropertyService>());

            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _enableServicesTaskSource = new TaskCompletionSource<object>();

            _mockedProject = Mock.Of<DteProject>(p => p.ConfigurationManager.ConfigurationRowNames == new string[0]);
            _mockedPublishDialog = Mock.Of<IPublishDialog>(
                d => d.Project.Name == VisualStudioProjectName && d.Project.Project == _mockedProject);

            _pickProjectPromptMock = new Mock<Func<GcpProject>>();
            _changedProperties = new List<string>();

            _apiManagerMock = new Mock<IApiManager>();
            _apiManagerMock.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>())).Returns(() => _areServicesEnabledTaskSource.Task);
            _apiManagerMock.Setup(x => x.EnableServicesAsync(It.IsAny<IEnumerable<string>>())).Returns(() => _enableServicesTaskSource.Task);

            PackageMock.Setup(p => p.GetMefService<IApiManager>()).Returns(_apiManagerMock.Object);

            _objectUnderTest = new TestPublishDialogStep(_pickProjectPromptMock.Object, _mockedPublishDialog);
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
        }

        protected override void AfterEach() => _objectUnderTest.OnFlowFinished();

        [TestMethod]
        public void TestInitialState()
        {
            Assert.AreEqual(_mockedPublishDialog, _objectUnderTest.PublishDialog);
            Assert.IsFalse(_objectUnderTest.LoadProjectTask.IsCompleted);
            Assert.IsFalse(_objectUnderTest.LoadProjectTask.IsPending);
            Assert.IsFalse(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.HasErrors);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        [TestMethod]
        public void TestOnVisible_EnablesSelectProjectCommand()
        {
            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestOnVisible_RaisesGcpProjectIdPropertyChanged()
        {
            _objectUnderTest.OnVisible();

            CollectionAssert.Contains(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
        }

        [TestMethod]
        public void TestOnVisible_StartsProjectLoad()
        {
            AsyncProperty originalLoadProjectTask = _objectUnderTest.LoadProjectTask;

            _objectUnderTest.OnVisible();

            Assert.AreNotEqual(originalLoadProjectTask, _objectUnderTest.LoadProjectTask);
        }

        [TestMethod]
        public void TestOnVisible_LoadsProperties()
        {
            _objectUnderTest.OnVisible();

            Assert.AreEqual(1, _objectUnderTest.LoadPropertiesCallCount);
        }

        [TestMethod]
        public void TestOnVisible_AddsFlowFinishedHandler()
        {
            _objectUnderTest.OnVisible();

            Mock.Get(_mockedPublishDialog).Raise(pd => pd.FlowFinished += null, _mockedPublishDialog, null);

            Assert.AreEqual(1, _objectUnderTest.OnFlowFinishedCallCount);
        }

        [TestMethod]
        public void TestOnNotVisible_SavesProperties()
        {
            _objectUnderTest.OnNotVisible();

            Assert.AreEqual(1, _objectUnderTest.SavePropertiesCallCount);
        }

        [TestMethod]
        public void TestOnNotVisible_RemovesOldFlowFinishedHandler()
        {
            _objectUnderTest.OnVisible();

            _objectUnderTest.OnNotVisible();
            Mock.Get(_mockedPublishDialog).Raise(pd => pd.FlowFinished += null, _mockedPublishDialog, null);
            CredentialStoreMock.Raise(
                cs => cs.CurrentProjectIdChanged += null, CredentialsStore.Default, null);

            Assert.AreEqual(0, _objectUnderTest.OnFlowFinishedCallCount);
        }

        [TestMethod]
        public async Task TestValidateProject_EmptyProjectIsInvalid()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(() => null);

            await _objectUnderTest.ValidateProjectAsyncBase();

            Assert.IsFalse(_objectUnderTest.IsValidGcpProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public async Task TestValidateProject_ValidProjectWithNoRequiredApis()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(ValidProjectId);
            _objectUnderTest.RequiredApisOverride = new List<string>();

            await _objectUnderTest.ValidateProjectAsyncBase();

            Assert.IsTrue(_objectUnderTest.IsValidGcpProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public void TestValidateProject_ValidProjectWithRequiredApisLoading()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(ValidProjectId);
            _objectUnderTest.RequiredApisOverride = s_mockedRequiredApis;

            Task validateProjectTask = _objectUnderTest.ValidateProjectAsyncBase();

            Assert.IsFalse(validateProjectTask.IsCompleted);
            Assert.IsFalse(_objectUnderTest.IsValidGcpProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public async Task TestValidateProject_ValidProjectWithRequiredApisNotEnabled()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(ValidProjectId);
            _objectUnderTest.RequiredApisOverride = s_mockedRequiredApis;
            _areServicesEnabledTaskSource.SetResult(false);

            await _objectUnderTest.ValidateProjectAsyncBase();

            Assert.IsFalse(_objectUnderTest.IsValidGcpProject);
            Assert.IsTrue(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public async Task TestValidateProject_ValidProjectWithRequiredApisEnabled()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(ValidProjectId);
            _objectUnderTest.RequiredApisOverride = s_mockedRequiredApis;
            _areServicesEnabledTaskSource.SetResult(true);

            await _objectUnderTest.ValidateProjectAsyncBase();

            Assert.IsTrue(_objectUnderTest.IsValidGcpProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }


        [TestMethod]
        public void TestReloadProjectCommand_Loading()
        {
            _objectUnderTest.LoadProject();

            Assert.IsTrue(_objectUnderTest.LoadProjectTask.IsPending);
            Assert.AreEqual(1, _objectUnderTest.ClearLoadedProjectDataCallCount);
            Assert.AreEqual(1, _objectUnderTest.LoadAnyProjectDataCallCount);
        }

        [TestMethod]
        public void TestReloadProjectAsync_LoadsValidProjectDataOnValidProject()
        {
            _objectUnderTest.ValidateProjectResult.SetResult(true);
            _objectUnderTest.LoadProject();

            Assert.AreEqual(1, _objectUnderTest.LoadValidProjectDataAsyncCallCount);
        }

        [TestMethod]
        public void TestReloadProjectAsync_SkipsLoadValidProjectDataOnInvalidProject()
        {
            _objectUnderTest.ValidateProjectResult.SetResult(false);
            _objectUnderTest.LoadProject();

            Assert.AreEqual(0, _objectUnderTest.LoadValidProjectDataAsyncCallCount);
        }

        [TestMethod]
        public void TestReloadProjectAsync_AwaitsLoadValidProjectDataResult()
        {
            _objectUnderTest.ValidateProjectResult.SetResult(true);
            _objectUnderTest.LoadProject();

            Assert.IsFalse(_objectUnderTest.LoadProjectTask.ActualTask.IsCompleted);
        }

        [TestMethod]
        public async Task TestReloadProjectAsync_DataLoaded()
        {
            _objectUnderTest.ValidateProjectResult.SetResult(true);
            _objectUnderTest.LoadAnyProjectDataResult.SetResult(null);
            _objectUnderTest.LoadValidProjectDataResult.SetResult(null);

            await _objectUnderTest.LoadProjectAsync();

            Assert.IsTrue(_objectUnderTest.LoadProjectTask.IsSuccess);
        }

        [TestMethod]
        public async Task TestReloadProjectAsync_Error()
        {
            _objectUnderTest.ValidateProjectResult.SetResult(false);
            _objectUnderTest.LoadAnyProjectDataResult.SetException(new Exception("Test Exception"));

            await _objectUnderTest.LoadProjectAsync();

            Assert.IsTrue(_objectUnderTest.LoadProjectTask.IsError);
        }

        [TestMethod]
        public void TestEnableApisCommand_CallsEnableServices()
        {
            _objectUnderTest.RequiredApisOverride = s_mockedRequiredApis;
            _objectUnderTest.EnableApiCommand.Execute(null);

            _apiManagerMock.Verify(api => api.EnableServicesAsync(s_mockedRequiredApis), Times.Once());
        }

        [TestMethod]
        public void TestEnableApisCommand_ReloadsProject()
        {
            _enableServicesTaskSource.SetResult(null);
            _objectUnderTest.EnableApiCommand.Execute(null);

            // Verify project reload started.
            Assert.IsTrue(_objectUnderTest.LoadProjectTask.IsPending);
        }

        [TestMethod]
        public async Task TestEnableApisCommand_Failure()
        {
            _enableServicesTaskSource.SetException(new Exception("Test Exception"));
            _objectUnderTest.EnableApiCommand.Execute(null);

            await _objectUnderTest.EnableApiCommand.LatestExecution.SafeTask;

            Assert.IsTrue(_objectUnderTest.EnableApiCommand.LatestExecution.IsError);
            // Verify project reload did not start.
            Assert.IsFalse(_objectUnderTest.LoadProjectTask.IsPending);
        }

        [TestMethod]
        public void TestOnFlowFinished_ResetsProperties()
        {
            _objectUnderTest.OnVisible();
            _objectUnderTest.ClearLoadedProjectDataCallCount = 0;

            _objectUnderTest.OnFlowFinishedBase();

            Assert.IsFalse(_objectUnderTest.IsValidGcpProject);
            Assert.IsFalse(_objectUnderTest.LoadProjectTask.IsPending);
            Assert.IsFalse(_objectUnderTest.LoadProjectTask.IsError);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
            Assert.AreEqual(1, _objectUnderTest.ClearLoadedProjectDataCallCount);
        }

        [TestMethod]
        public void TestOnFlowFinished_SavesProjectProperties()
        {
            _objectUnderTest.OnFlowFinishedBase();

            Assert.AreEqual(1, _objectUnderTest.SavePropertiesCallCount);
        }

        [TestMethod]
        public void TestOnFlowFinished_RemovesHandlers()
        {
            _objectUnderTest.OnVisible();

            _objectUnderTest.OnFlowFinishedBase();
            Mock.Get(_mockedPublishDialog).Raise(pd => pd.FlowFinished += null, _mockedPublishDialog, null);
            CredentialStoreMock.Raise(cs => cs.CurrentProjectIdChanged += null, CredentialsStore.Default, null);

            Assert.AreEqual(0, _objectUnderTest.OnFlowFinishedCallCount);
        }

        [TestMethod]
        public void TestOnIsValidGcpProjectIdChanged_CalledWhenIsValidGcpProjectIdChanges()
        {
            _objectUnderTest.IsValidGcpProject = true;
            _objectUnderTest.OnIsValidGcpProjectChangedCallCount = 0;
            _objectUnderTest.IsValidGcpProject = false;

            Assert.AreEqual(1, _objectUnderTest.OnIsValidGcpProjectChangedCallCount);
        }

        [TestMethod]
        public void TestOnIsValidGcpProjectIdChanged_NotCalledWhenIsValidGcpProjectIdSetToSameValue()
        {
            _objectUnderTest.IsValidGcpProject = true;
            _objectUnderTest.OnIsValidGcpProjectChangedCallCount = 0;
            _objectUnderTest.IsValidGcpProject = true;

            Assert.AreEqual(0, _objectUnderTest.OnIsValidGcpProjectChangedCallCount);
        }

        [TestMethod]
        public void TestConfigurations_SetsProperty()
        {
            _objectUnderTest.Configurations = s_configurations;

            CollectionAssert.AreEqual(s_configurations, _objectUnderTest.Configurations.ToList());
        }

        [TestMethod]
        public void TestConfigurations_RaisesPropertyChanged()
        {
            _objectUnderTest.Configurations = s_configurations;

            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.Configurations));
        }

        [TestMethod]
        public void TestConfigurations_SetsSelectedConfigurationToNullWhenNull()
        {
            _objectUnderTest.Configurations = null;

            Assert.AreEqual(null, _objectUnderTest.SelectedConfiguration);
        }

        [TestMethod]
        public void TestConfigurations_SetsSelectedConfigurationToNullWhenEmpty()
        {
            _objectUnderTest.Configurations = new List<string>();

            Assert.AreEqual(null, _objectUnderTest.SelectedConfiguration);
        }

        [TestMethod]
        public void TestConfigurations_SetsSelectedConfigurationToFirstElement()
        {
            const string expectedConfiguration = Configuration;

            _objectUnderTest.Configurations = new List<string> { expectedConfiguration };

            Assert.AreEqual(expectedConfiguration, _objectUnderTest.SelectedConfiguration);
        }

        [TestMethod]
        public void TestConfigurations_SetsSelectedConfigurationToDefault()
        {
            _objectUnderTest.Configurations = new List<string> { PublishDialogStepBase.DefaultConfiguration };

            Assert.AreEqual(PublishDialogStepBase.DefaultConfiguration, _objectUnderTest.SelectedConfiguration);
        }

        [TestMethod]
        public void TestConfigurations_SetsSelectedConfigurationToSavedConfiguration()
        {
            _propertyServiceMock
                .Setup(ps => ps.GetUserProperty(_mockedProject, PublishDialogStepBase.ConfigurationPropertyName))
                .Returns(SavedConfiguration);

            _objectUnderTest.OnVisible();
            _objectUnderTest.Configurations = new List<string> { PublishDialogStepBase.DefaultConfiguration, SavedConfiguration };

            Assert.AreEqual(SavedConfiguration, _objectUnderTest.SelectedConfiguration);
        }

        [TestMethod]
        public void TestConfigurations_LeavesSelectedConfigurationUnchangedWhenAvailable()
        {
            _propertyServiceMock
                .Setup(ps => ps.GetUserProperty(_mockedProject, PublishDialogStepBase.ConfigurationPropertyName))
                .Returns(SavedConfiguration);

            _objectUnderTest.OnVisible();
            _objectUnderTest.SelectedConfiguration = Configuration;
            _objectUnderTest.Configurations = new List<string>
            {
                PublishDialogStepBase.DefaultConfiguration,
                SavedConfiguration,
                Configuration
            };

            Assert.AreEqual(Configuration, _objectUnderTest.SelectedConfiguration);
        }

        [TestMethod]
        public void TestSelectedConfiguration_SetsProperty()
        {
            _objectUnderTest.SelectedConfiguration = Configuration;

            Assert.AreEqual(Configuration, _objectUnderTest.SelectedConfiguration);
        }

        [TestMethod]
        public void TestSelectedConfiguration_RaisesPropertyChanged()
        {
            _objectUnderTest.SelectedConfiguration = Configuration;

            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.SelectedConfiguration));
        }

        [TestMethod]
        public void TestOnVisible_LoadsConfigurationsFromProject()
        {
            Mock.Get(_mockedProject)
                .Setup(p => p.ConfigurationManager.ConfigurationRowNames)
                .Returns(s_configurations);

            _objectUnderTest.OnVisible();

            CollectionAssert.AreEqual(s_configurations, _objectUnderTest.Configurations.ToList());
        }

        [TestMethod]
        public void TestOnVisible_LoadsConfigurationsFromProjectObjectArray()
        {
            Mock.Get(_mockedProject)
                .Setup(p => p.ConfigurationManager.ConfigurationRowNames)
                .Returns(s_configurations.ToArray<object>);

            _objectUnderTest.OnVisible();

            CollectionAssert.AreEqual(s_configurations, _objectUnderTest.Configurations.ToList());
        }

        [TestMethod]
        public void TestOnVisible_LoadsConfigurationProperty()
        {
            _propertyServiceMock
                .Setup(ps => ps.GetUserProperty(_mockedProject, PublishDialogStepBase.ConfigurationPropertyName))
                .Returns(SavedConfiguration);
            Mock.Get(_mockedProject)
                .Setup(p => p.ConfigurationManager.ConfigurationRowNames)
                .Returns(new[] { PublishDialogStepBase.DefaultConfiguration, SavedConfiguration, Configuration });

            _objectUnderTest.OnVisible();

            Assert.AreEqual(SavedConfiguration, _objectUnderTest.SelectedConfiguration);
        }

        [TestMethod]
        public void TestOnNotVisible_SavesConfigurationProperty()
        {
            _objectUnderTest.SelectedConfiguration = Configuration;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                ps => ps.SaveUserProperty(
                    _mockedProject,
                    PublishDialogStepBase.ConfigurationPropertyName,
                    Configuration));
        }
    }
}
