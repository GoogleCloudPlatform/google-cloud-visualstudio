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
using GoogleCloudExtension.PublishDialog.Steps.Flex;
using GoogleCloudExtension.Services.VsProject;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
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
        private Mock<IVsProjectPropertyService> _propertyServiceMock;
        private EnvDTE.Project _mockedProject;

        [ClassInitialize]
        public static void BeforeAll(TestContext context) =>
            GcpPublishStepsUtils.NowOverride = DateTime.Parse("2088-12-23 01:01:01");

        [ClassCleanup]
        public static void AfterAll() => GcpPublishStepsUtils.NowOverride = null;

        protected override void BeforeEach()
        {
            _propertyServiceMock = new Mock<IVsProjectPropertyService>();
            PackageMock.Setup(p => p.GetService<IVsProjectPropertyService>()).Returns(_propertyServiceMock.Object);

            _getApplicationTaskSource = new TaskCompletionSource<Application>();
            _setAppRegionTaskSource = new TaskCompletionSource<bool>();

            _mockedProject = Mock.Of<EnvDTE.Project>();
            _mockedPublishDialog = Mock.Of<IPublishDialog>(
                pd => pd.Project.Name == VisualStudioProjectName && pd.Project.Project == _mockedProject);

            _pickProjectPromptMock = new Mock<Func<Project>>();

            _apiManagerMock = new Mock<IApiManager>();
            _apiManagerMock.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>()))
                .Returns(Task.FromResult(true));

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
        public void TestSetVersion_Null()
        {
            _objectUnderTest.Version = null;

            Assert.IsNull(_objectUnderTest.Version);
            Assert.IsTrue(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public void TestSetVersion_Empty()
        {
            _objectUnderTest.Version = string.Empty;

            Assert.AreEqual(string.Empty, _objectUnderTest.Version);
            Assert.IsTrue(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public void TestSetVersion_Invalid()
        {
            _objectUnderTest.Version = InvalidVersion;

            Assert.AreEqual(InvalidVersion, _objectUnderTest.Version);
            Assert.IsTrue(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public void TestSetVersion_Valid()
        {
            _objectUnderTest.Version = ValidVersion;

            Assert.AreEqual(ValidVersion, _objectUnderTest.Version);
            Assert.IsFalse(_objectUnderTest.HasErrors);
        }
    }
}
