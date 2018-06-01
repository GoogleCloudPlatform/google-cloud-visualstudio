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
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps.Flex;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialog.Steps.Flex
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
        private Mock<IVsBuildPropertyStorage> _vsPropertyStoreMock;

        [ClassInitialize]
        public static void BeforeAll(TestContext context) => GcpPublishStepsUtils.NowOverride = DateTime.Parse("2088-12-23 01:01:01");

        [ClassCleanup]
        public static void AfterAll() => GcpPublishStepsUtils.NowOverride = null;

        protected override void BeforeEach()
        {

            var vsHierarchyMock = new Mock<IVsHierarchy>();
            _vsPropertyStoreMock = vsHierarchyMock.As<IVsBuildPropertyStorage>();
            // ReSharper disable once RedundantAssignment
            IVsHierarchy vsProject = vsHierarchyMock.Object;
            PackageMock.Setup(
                    p => p.GetService<IVsSolution>().GetProjectOfUniqueName(It.IsAny<string>(), out vsProject))
                .Returns(VSConstants.S_OK);

            _getApplicationTaskSource = new TaskCompletionSource<Application>();
            _setAppRegionTaskSource = new TaskCompletionSource<bool>();

            _mockedPublishDialog = Mock.Of<IPublishDialog>(
                pd => pd.Project.Name == VisualStudioProjectName &&
                    pd.Project.Project.UniqueName == "DefaultUniqueName");

            _pickProjectPromptMock = new Mock<Func<Project>>();

            _apiManagerMock = new Mock<IApiManager>();
            _apiManagerMock.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>())).Returns(Task.FromResult(true));

            _gaeDataSourceMock = new Mock<IGaeDataSource>();
            _gaeDataSourceMock.Setup(x => x.GetApplicationAsync()).Returns(() => _getApplicationTaskSource.Task);
            _mockedApplication = Mock.Of<Application>();

            _setAppRegionAsyncFuncMock = new Mock<Func<Task<bool>>>();
            _setAppRegionAsyncFuncMock.Setup(func => func()).Returns(() => _setAppRegionTaskSource.Task);

            _objectUnderTest = new FlexStepViewModel(
                _gaeDataSourceMock.Object, _apiManagerMock.Object, _pickProjectPromptMock.Object,
                _setAppRegionAsyncFuncMock.Object, _mockedPublishDialog);
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
        }

        [TestMethod]
        public void TestValidateProjectAsync_NoProjectSetsNeedsAppCreated()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            _objectUnderTest.NeedsAppCreated = true;

            _objectUnderTest.OnVisible();

            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            _gaeDataSourceMock.Verify(src => src.GetApplicationAsync(), Times.Never());
        }

        [TestMethod]
        public void TestValidateProjectAsync_ErrorInApplicationValidation()
        {
            _getApplicationTaskSource.SetException(new DataSourceException());
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            _objectUnderTest.OnVisible();

            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.LoadProjectTask.IsError);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        [TestMethod]
        public void TestValidateProjectAsync_NeedsAppCreated()
        {
            _getApplicationTaskSource.SetResult(null);
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.NeedsAppCreated);
            Assert.IsTrue(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        [TestMethod]
        public void TestValidateProjectAsync_Succeeds()
        {
            _objectUnderTest.NeedsAppCreated = true;
            _getApplicationTaskSource.SetResult(_mockedApplication);
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            _objectUnderTest.OnVisible();

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
            _objectUnderTest.OnVisible();

            _objectUnderTest.SetAppRegionCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.LoadProjectTask.IsPending);
        }

        [TestMethod]
        public void TestSetAppRegionCommand_SkipsReloadOnSetRegionFailure()
        {
            AsyncProperty originalLoadProjectTask = _objectUnderTest.LoadProjectTask;
            _setAppRegionTaskSource.SetResult(false);
            _objectUnderTest.SetAppRegionCommand.Execute(null);

            Assert.AreEqual(originalLoadProjectTask, _objectUnderTest.LoadProjectTask);
        }

        [TestMethod]
        public void TestSetAppRegionCommand_SkipsReloadOnSetRegionException()
        {
            AsyncProperty originalLoadProjectTask = _objectUnderTest.LoadProjectTask;
            _setAppRegionTaskSource.SetException(new Exception("test exception"));
            _objectUnderTest.SetAppRegionCommand.Execute(null);

            Assert.AreEqual(originalLoadProjectTask, _objectUnderTest.LoadProjectTask);
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
        public void TestOnFlowFinished_SetsNeedsAppCreated()
        {
            _objectUnderTest.NeedsAppCreated = true;
            _objectUnderTest.OnVisible();

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
        }

        [TestMethod]
        public void TestOnFlowFinished_SavesPromoteProperty()
        {
            _objectUnderTest.OnVisible();
            _objectUnderTest.Promote = true;

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            _vsPropertyStoreMock.Verify(
                s => s.SetPropertyValue(
                    FlexStepViewModel.GoogleAppEnginePublish_Promote, null, ParsedDteProjectExtensions.UserFileFlag,
                    bool.TrueString));
        }

        [TestMethod]
        public void TestOnFlowFinished_SavesOpenWebsiteProperty()
        {
            _objectUnderTest.OnVisible();
            _objectUnderTest.OpenWebsite = true;

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            _vsPropertyStoreMock.Verify(
                s => s.SetPropertyValue(
                    FlexStepViewModel.GoogleAppEnginePublish_OpenWebsite, null, ParsedDteProjectExtensions.UserFileFlag,
                    bool.TrueString));
        }

        [TestMethod]
        public void TestOnFlowFinished_SavesNextVersionPropertyForNonStandard()
        {
            _objectUnderTest.OnVisible();
            const string versionString = "version-string-2";
            _objectUnderTest.Version = versionString;

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            _vsPropertyStoreMock.Verify(
                s => s.SetPropertyValue(
                    FlexStepViewModel.GoogleAppEnginePublish_NextVersion, null, ParsedDteProjectExtensions.UserFileFlag,
                    versionString));
        }

        [TestMethod]
        public void TestOnFlowFinished_DeletesNextVersionPropertyForDefaultProperty()
        {
            _objectUnderTest.OnVisible();
            _objectUnderTest.Version = "12345678t123456";

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            _vsPropertyStoreMock.Verify(
                s => s.RemoveProperty(
                    FlexStepViewModel.GoogleAppEnginePublish_NextVersion, null,
                    ParsedDteProjectExtensions.UserFileFlag));
        }

        [TestMethod]
        public void TestOnFlowFinished_DeletesNextVersionPropertyForEmptyProperty()
        {
            _objectUnderTest.OnVisible();
            _objectUnderTest.Version = "   ";

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            _vsPropertyStoreMock.Verify(
                s => s.RemoveProperty(
                    FlexStepViewModel.GoogleAppEnginePublish_NextVersion, null,
                    ParsedDteProjectExtensions.UserFileFlag));
        }

        [TestMethod]
        public void TestOnNotVisible_SavesPromoteProperty()
        {
            _objectUnderTest.Promote = true;

            _objectUnderTest.OnNotVisible();

            _vsPropertyStoreMock.Verify(
                s => s.SetPropertyValue(
                    FlexStepViewModel.GoogleAppEnginePublish_Promote, null, ParsedDteProjectExtensions.UserFileFlag,
                    bool.TrueString));
        }

        [TestMethod]
        public void TestOnNotVisible_SavesOpenWebsiteProperty()
        {
            _objectUnderTest.OpenWebsite = true;

            _objectUnderTest.OnNotVisible();

            _vsPropertyStoreMock.Verify(
                s => s.SetPropertyValue(
                    FlexStepViewModel.GoogleAppEnginePublish_OpenWebsite, null, ParsedDteProjectExtensions.UserFileFlag,
                    bool.TrueString));
        }

        [TestMethod]
        public void TestOnNotVisible_SavesNextVersionPropertyForNonStandard()
        {
            const string versionString = "version-string-2";
            _objectUnderTest.Version = versionString;

            _objectUnderTest.OnNotVisible();

            _vsPropertyStoreMock.Verify(
                s => s.SetPropertyValue(
                    FlexStepViewModel.GoogleAppEnginePublish_NextVersion, null, ParsedDteProjectExtensions.UserFileFlag,
                    versionString));
        }

        [TestMethod]
        public void TestOnNotVisible_DeletesNextVersionPropertyForDefaultProperty()
        {
            _objectUnderTest.Version = "12345678t123456";

            _objectUnderTest.OnNotVisible();

            _vsPropertyStoreMock.Verify(
                s => s.RemoveProperty(
                    FlexStepViewModel.GoogleAppEnginePublish_NextVersion, null,
                    ParsedDteProjectExtensions.UserFileFlag));
        }

        [TestMethod]
        public void TestOnNotVisible_DeletesNextVersionPropertyForEmptyProperty()
        {
            _objectUnderTest.Version = "   ";

            _objectUnderTest.OnNotVisible();

            _vsPropertyStoreMock.Verify(
                s => s.RemoveProperty(
                    FlexStepViewModel.GoogleAppEnginePublish_NextVersion, null,
                    ParsedDteProjectExtensions.UserFileFlag));
        }

        [TestMethod]
        public void TestOnVisible_LoadsPromoteProperty()
        {
            // ReSharper disable once RedundantAssignment
            string promoteProperty = bool.FalseString;
            _vsPropertyStoreMock.Setup(
                s => s.GetPropertyValue(
                    FlexStepViewModel.GoogleAppEnginePublish_Promote, null, ParsedDteProjectExtensions.UserFileFlag,
                    out promoteProperty));

            _objectUnderTest.OnVisible();

            Assert.IsFalse(_objectUnderTest.Promote);
        }

        [TestMethod]
        public void TestOnVisible_SkipLoadOfUnparsablePromoteProperty()
        {
            // ReSharper disable once RedundantAssignment
            var promoteProperty = "unparsable as bool";
            _vsPropertyStoreMock.Setup(
                s => s.GetPropertyValue(
                    FlexStepViewModel.GoogleAppEnginePublish_Promote, null, ParsedDteProjectExtensions.UserFileFlag,
                    out promoteProperty));

            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.Promote);
        }

        [TestMethod]
        public void TestOnVisible_LoadsOpenWebsiteProperty()
        {
            // ReSharper disable once RedundantAssignment
            string openWebsiteProperty = bool.FalseString;
            _vsPropertyStoreMock.Setup(
                s => s.GetPropertyValue(
                    FlexStepViewModel.GoogleAppEnginePublish_OpenWebsite, null, ParsedDteProjectExtensions.UserFileFlag,
                    out openWebsiteProperty));

            _objectUnderTest.OnVisible();

            Assert.IsFalse(_objectUnderTest.OpenWebsite);
        }

        [TestMethod]
        public void TestOnVisible_SkipLoadOfUnparsableOpenWebSiteProperty()
        {
            // ReSharper disable once RedundantAssignment
            var openWebsiteProperty = "unparsable as bool";
            _vsPropertyStoreMock.Setup(
                s => s.GetPropertyValue(
                    FlexStepViewModel.GoogleAppEnginePublish_OpenWebsite, null, ParsedDteProjectExtensions.UserFileFlag,
                    out openWebsiteProperty));

            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.OpenWebsite);
        }

        [TestMethod]
        public void TestOnVisible_LoadsNextVersionProperty()
        {
            // ReSharper disable once RedundantAssignment
            var nextVersionProperty = "NextVersion";
            _vsPropertyStoreMock.Setup(
                s => s.GetPropertyValue(
                    FlexStepViewModel.GoogleAppEnginePublish_NextVersion, null, ParsedDteProjectExtensions.UserFileFlag,
                    out nextVersionProperty));

            _objectUnderTest.OnVisible();

            Assert.AreEqual(nextVersionProperty, _objectUnderTest.Version);
        }

        [TestMethod]
        public void TestOnVisible_SetsVersionToDefaultOnEmptyNextVersionProperty()
        {
            GcpPublishStepsUtils.NowOverride = DateTime.Parse("2008-08-08 08:08:08");
            // ReSharper disable once RedundantAssignment
            var nextVersionProperty = " ";
            _vsPropertyStoreMock.Setup(
                s => s.GetPropertyValue(
                    FlexStepViewModel.GoogleAppEnginePublish_NextVersion, null, ParsedDteProjectExtensions.UserFileFlag,
                    out nextVersionProperty));

            _objectUnderTest.OnVisible();

            Assert.AreEqual(GcpPublishStepsUtils.GetDefaultVersion(), _objectUnderTest.Version);
        }
    }
}
