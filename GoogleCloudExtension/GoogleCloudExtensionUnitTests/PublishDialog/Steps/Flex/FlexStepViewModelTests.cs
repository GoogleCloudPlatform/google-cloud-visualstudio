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
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps.Flex;
using GoogleCloudExtension.Services.Configuration;
using GoogleCloudExtension.Services.VsProject;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestingHelpers;
using Project = Google.Apis.CloudResourceManager.v1.Data.Project;

namespace GoogleCloudExtensionUnitTests.PublishDialog.Steps.Flex
{
    [TestClass]
    public class FlexStepViewModelTests : ExtensionTestBase
    {
        private const string VisualStudioProjectName = "VisualStudioProjectName";
        private const string InvalidVersion = "-Invalid Version Name!";
        private const string ValidVersion = "valid-version-name";

        private static readonly GCloudValidationResult s_validGCloudValidationResult =
            new GCloudValidationResult(true, true, true);
        private static readonly GCloudValidationResult s_invalidGCloudValidationResult =
            new GCloudValidationResult(true);

        private FlexStepViewModel _objectUnderTest;
        private Mock<IGaeDataSource> _gaeDataSourceMock;
        private TaskCompletionSource<Application> _getApplicationTaskSource;
        private Mock<Func<Task<bool>>> _setAppRegionAsyncFuncMock;
        private TaskCompletionSource<bool> _setAppRegionTaskSource;
        private IPublishDialog _mockedPublishDialog;
        private Mock<IVsProjectPropertyService> _propertyServiceMock;
        private EnvDTE.Project _mockedProject;
        private List<string> _propertiesChanges;
        private TaskCompletionSource<GCloudValidationResult> _validateGCloudSource;
        private Mock<IAppEngineConfiguration> _appEngineConfigurationMock;
        private Task _trackedTask;
        private TaskCompletionSource<object> _publishSource;
        private Mock<IAppEngineFlexDeployment> _appEngineDeploymentMock;

        [ClassInitialize]
        public static void BeforeAll(TestContext context) =>
            GcpPublishStepsUtils.NowOverride = DateTime.Parse("2088-12-23 01:01:01");

        [ClassCleanup]
        public static void AfterAll() => GcpPublishStepsUtils.NowOverride = null;

        protected override void BeforeEach()
        {
            _validateGCloudSource = new TaskCompletionSource<GCloudValidationResult>();
            GCloudWrapperUtils.ValidateGCloudAsyncOverride =
                Mock.Of<Func<GCloudComponent, Task<GCloudValidationResult>>>(
                    f => f(It.IsAny<GCloudComponent>()) == _validateGCloudSource.Task);

            _propertyServiceMock = new Mock<IVsProjectPropertyService>();

            _appEngineDeploymentMock = new Mock<IAppEngineFlexDeployment>();
            _publishSource = new TaskCompletionSource<object>();
            _appEngineDeploymentMock.Setup(
                    d => d.PublishProjectAsync(
                        It.IsAny<IParsedProject>(), It.IsAny<AppEngineFlexDeployment.DeploymentOptions>()))
                .Returns(() => _publishSource.Task);
            _appEngineConfigurationMock = new Mock<IAppEngineConfiguration>();

            PackageMock.Setup(p => p.GetMefService<IVsProjectPropertyService>()).Returns(_propertyServiceMock.Object);
            PackageMock.Setup(p => p.GetMefServiceLazy<IAppEngineFlexDeployment>())
                .Returns(_appEngineDeploymentMock.ToLazy());
            PackageMock.Setup(p => p.GetMefServiceLazy<IAppEngineConfiguration>())
                .Returns(_appEngineConfigurationMock.ToLazy());

            _mockedProject = Mock.Of<EnvDTE.Project>();
            _mockedPublishDialog = Mock.Of<IPublishDialog>(
                pd => pd.Project.Name == VisualStudioProjectName && pd.Project.Project == _mockedProject);

            _trackedTask = null;
            Mock.Get(_mockedPublishDialog).Setup(pd => pd.TrackTask(It.IsAny<Task>()))
                .Callback((Task t) => _trackedTask = t);

            var mockedApiManager = Mock.Of<IApiManager>(
                m => m.AreServicesEnabledAsync(It.IsAny<IList<string>>()) == Task.FromResult(true));

            _gaeDataSourceMock = new Mock<IGaeDataSource>();
            _getApplicationTaskSource = new TaskCompletionSource<Application>();
            _gaeDataSourceMock.Setup(x => x.GetApplicationAsync()).Returns(() => _getApplicationTaskSource.Task);

            _setAppRegionAsyncFuncMock = new Mock<Func<Task<bool>>>();
            _setAppRegionTaskSource = new TaskCompletionSource<bool>();
            _setAppRegionAsyncFuncMock.Setup(func => func()).Returns(() => _setAppRegionTaskSource.Task);

            _objectUnderTest = new FlexStepViewModel(
                _gaeDataSourceMock.Object, mockedApiManager, Mock.Of<Func<Project>>(),
                _setAppRegionAsyncFuncMock.Object, _mockedPublishDialog);
            _propertiesChanges = new List<string>();
            _objectUnderTest.PropertyChanged += (sender, args) => _propertiesChanges.Add(args.PropertyName);
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
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(() => null);
            _objectUnderTest.NeedsAppCreated = true;

            _objectUnderTest.OnVisible();

            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            _gaeDataSourceMock.Verify(src => src.GetApplicationAsync(), Times.Never());
        }

        [TestMethod]
        public void TestValidateProjectAsync_ErrorInApplicationValidation()
        {
            _getApplicationTaskSource.SetException(new DataSourceException());

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

            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.NeedsAppCreated);
            Assert.IsTrue(_objectUnderTest.SetAppRegionCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }

        [TestMethod]
        public void TestValidateProjectAsync_Succeeds()
        {
            _objectUnderTest.NeedsAppCreated = true;
            _getApplicationTaskSource.SetResult(Mock.Of<Application>());

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

        [TestMethod]
        public void TestSaveProjectProperties_SavesPromoteProperty()
        {
            _objectUnderTest.Promote = true;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(_mockedProject, FlexStepViewModel.PromoteProjectPropertyName, bool.TrueString));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesOpenWebsiteProperty()
        {
            _objectUnderTest.OpenWebsite = true;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _mockedProject, FlexStepViewModel.OpenWebsiteProjectPropertyName, bool.TrueString));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesNextVersionPropertyForNonStandard()
        {
            const string versionString = "version-string-2";
            _objectUnderTest.Version = versionString;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _mockedProject, FlexStepViewModel.NextVersionProjectPropertyName, versionString));
        }

        [TestMethod]
        public void TestSaveProjectProperties_DeletesNextVersionPropertyForDefaultProperty()
        {
            _objectUnderTest.Version = "12345678t123456";

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.DeleteUserProperty(_mockedProject, FlexStepViewModel.NextVersionProjectPropertyName));
        }

        [TestMethod]
        public void TestSaveProjectProperties_DeletesNextVersionPropertyForEmptyProperty()
        {
            _objectUnderTest.Version = "   ";

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.DeleteUserProperty(_mockedProject, FlexStepViewModel.NextVersionProjectPropertyName));
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsPromoteProperty()
        {
            string promoteProperty = bool.FalseString;
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_mockedProject, FlexStepViewModel.PromoteProjectPropertyName))
                .Returns(promoteProperty);

            _objectUnderTest.OnVisible();

            Assert.IsFalse(_objectUnderTest.Promote);
        }

        [TestMethod]
        public void TestLoadProjectProperties_SkipLoadOfUnparsablePromoteProperty()
        {
            const string promoteProperty = "unparsable as bool";
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_mockedProject, FlexStepViewModel.PromoteProjectPropertyName))
                .Returns(promoteProperty);

            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.Promote);
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsOpenWebsiteProperty()
        {
            string openWebsiteProperty = bool.FalseString;
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_mockedProject, FlexStepViewModel.OpenWebsiteProjectPropertyName))
                .Returns(openWebsiteProperty);

            _objectUnderTest.OnVisible();

            Assert.IsFalse(_objectUnderTest.OpenWebsite);
        }

        [TestMethod]
        public void TestLoadProjectProperties_SkipLoadOfUnparsableOpenWebSiteProperty()
        {
            const string openWebsiteProperty = "unparsable as bool";
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_mockedProject, FlexStepViewModel.OpenWebsiteProjectPropertyName))
                .Returns(openWebsiteProperty);

            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.OpenWebsite);
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsNextVersionProperty()
        {
            const string nextVersionProperty = "NextVersion";
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_mockedProject, FlexStepViewModel.NextVersionProjectPropertyName))
                .Returns(nextVersionProperty);

            _objectUnderTest.OnVisible();

            Assert.AreEqual(nextVersionProperty, _objectUnderTest.Version);
        }

        [TestMethod]
        public void TestLoadProjectProperties_SetsVersionToDefaultOnEmptyNextVersionProperty()
        {
            GcpPublishStepsUtils.NowOverride = DateTime.Parse("2008-08-08 08:08:08");
            const string nextVersionProperty = " ";
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_mockedProject, FlexStepViewModel.NextVersionProjectPropertyName))
                .Returns(nextVersionProperty);

            _objectUnderTest.OnVisible();

            Assert.AreEqual(GcpPublishStepsUtils.GetDefaultVersion(), _objectUnderTest.Version);
        }

        [TestMethod]
        public async Task TestLoadValidProjectDataAsync_SetsServices()
        {
            _getApplicationTaskSource.SetResult(Mock.Of<Application>());
            IList<Service> services =
                new List<Service> { new Service { Id = "default" }, new Service { Id = "service-name" } };
            _gaeDataSourceMock.Setup(ds => ds.GetServiceListAsync()).Returns(Task.FromResult(services));

            _objectUnderTest.OnVisible();
            await _objectUnderTest.LoadProjectTask.SafeTask;

            CollectionAssert.AreEqual(services.Select(s => s.Id).ToList(), _objectUnderTest.Services.ToList());
        }

        [TestMethod]
        public void TestSetServices_SetsProperty()
        {
            var services = new[] { "service-1", "service-2" };

            _objectUnderTest.Services = services;

            CollectionAssert.AreEqual(services, _objectUnderTest.Services.ToList());
        }

        [TestMethod]
        public void TestSetServices_Notifies()
        {
            _objectUnderTest.Services = new[] { "service-1", "service-2" };

            CollectionAssert.Contains(_propertiesChanges, nameof(_objectUnderTest.Services));
        }

        [TestMethod]
        public void TestLoadProperties_SetsServiceFromAppYaml()
        {
            var expectedService = "expected-service";
            _appEngineConfigurationMock.Setup(aed => aed.GetAppEngineService(It.IsAny<IParsedProject>()))
                .Returns(expectedService);

            _objectUnderTest.OnVisible();

            Assert.AreEqual(expectedService, _objectUnderTest.Service);
        }

        [TestMethod]
        public void TestSetService_SetsService()
        {
            const string serviceId = "service-id";

            _objectUnderTest.Service = serviceId;

            Assert.AreEqual(serviceId, _objectUnderTest.Service);
        }

        [TestMethod]
        public void TestSetService_Notifies()
        {
            _objectUnderTest.Service = "service-id";

            CollectionAssert.Contains(_propertiesChanges, nameof(_objectUnderTest.Service));
        }

        [TestMethod]
        public void TestSetService_MatchingAppYamlDisablesUpdateAppYamlService()
        {
            const string serviceId = "service-id";
            _appEngineConfigurationMock.Setup(aed => aed.GetAppEngineService(It.IsAny<IParsedProject>()))
                .Returns(serviceId);

            _objectUnderTest.Service = serviceId;

            Assert.IsFalse(_objectUnderTest.UpdateAppYamlServiceEnabled);
        }

        [TestMethod]
        public void TestSetService_DifferentFromAppYamlEnablesUpdateAppYamlService()
        {
            _appEngineConfigurationMock.Setup(aed => aed.GetAppEngineService(It.IsAny<IParsedProject>()))
                .Returns("service-in-app-yaml");

            _objectUnderTest.Service = "some-other-service";

            Assert.IsTrue(_objectUnderTest.UpdateAppYamlServiceEnabled);
        }

        [TestMethod]
        public void TestSetUpdateAppYamlServiceEnabled_SetsProperty()
        {
            _objectUnderTest.UpdateAppYamlServiceEnabled = true;

            Assert.IsTrue(_objectUnderTest.UpdateAppYamlServiceEnabled);
        }

        [TestMethod]
        public void TestSetUpdateAppYamlServiceEnabled_Notifies()
        {
            _objectUnderTest.UpdateAppYamlServiceEnabled = true;

            CollectionAssert.Contains(_propertiesChanges, nameof(_objectUnderTest.UpdateAppYamlServiceEnabled));
        }

        [TestMethod]
        public void TestUpdateAppYamlServiceEnabled_HidesUpdateAppYamlService()
        {
            _objectUnderTest.UpdateAppYamlServiceEnabled = false;
            _objectUnderTest.UpdateAppYamlService = true;

            Assert.IsFalse(_objectUnderTest.UpdateAppYamlService);
        }

        [TestMethod]
        public void TestUpdateAppYamlServiceEnabled_NotifiesOfUpdateAppYamlService()
        {
            _objectUnderTest.UpdateAppYamlServiceEnabled = false;

            CollectionAssert.Contains(_propertiesChanges, nameof(_objectUnderTest.UpdateAppYamlService));
        }

        [TestMethod]
        public void TestUpdateAppYamlService_IndependentlyFalse()
        {
            _objectUnderTest.UpdateAppYamlServiceEnabled = true;
            _objectUnderTest.UpdateAppYamlService = false;

            Assert.IsFalse(_objectUnderTest.UpdateAppYamlService);
        }

        [TestMethod]
        public void TestUpdateAppYamlService_TrueWhenNotHidden()
        {
            _objectUnderTest.UpdateAppYamlServiceEnabled = true;
            _objectUnderTest.UpdateAppYamlService = true;

            Assert.IsTrue(_objectUnderTest.UpdateAppYamlService);
        }

        [TestMethod]
        public void TestUpdateAppYamlService_Notifies()
        {
            _objectUnderTest.UpdateAppYamlService = true;

            CollectionAssert.Contains(_propertiesChanges, nameof(_objectUnderTest.UpdateAppYamlService));
        }

        [TestMethod]
        public void TestSaveProjectProperties_UpdatesAppYaml()
        {
            const string serviceId = "service-id";
            _objectUnderTest.UpdateAppYamlServiceEnabled = true;
            _objectUnderTest.UpdateAppYamlService = true;
            _objectUnderTest.Service = serviceId;

            _objectUnderTest.OnNotVisible();

            _appEngineConfigurationMock.Verify(
                s => s.SaveServiceToAppYaml(_objectUnderTest.PublishDialog.Project, serviceId));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SkipsUpdateAppYamlWhenDisabled()
        {
            const string serviceId = "service-id";
            _objectUnderTest.UpdateAppYamlService = false;
            _objectUnderTest.Service = serviceId;

            _objectUnderTest.OnNotVisible();

            _appEngineConfigurationMock.Verify(
                s => s.SaveServiceToAppYaml(It.IsAny<IParsedDteProject>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestPublishCommand_DelegatesToPublishCommandAsync()
        {
            Assert.AreEqual(_objectUnderTest.PublishCommandAsync, _objectUnderTest.PublishCommand);
        }

        [TestMethod]
        public async Task TestPublishCommandAsync_TracksVerifyGCloudDependenciesTask()
        {
            _objectUnderTest.PublishCommandAsync.Execute(null);

            Assert.IsFalse(_trackedTask.IsCompleted);
            _validateGCloudSource.SetResult(s_validGCloudValidationResult);
            await _trackedTask;
            Assert.IsTrue(_trackedTask.IsCompleted);
        }

        [TestMethod]
        public async Task TestPublishCommandAsync_SkipsPublishForInvalidGCloud()
        {
            _validateGCloudSource.SetResult(s_invalidGCloudValidationResult);
            const string expectedVersion = "expected-version";
            _objectUnderTest.Version = expectedVersion;

            _objectUnderTest.PublishCommandAsync.Execute(null);
            await _objectUnderTest.PublishCommandAsync.LatestExecution.SafeTask;

            _appEngineDeploymentMock.Verify(
                d => d.PublishProjectAsync(
                    It.IsAny<IParsedDteProject>(), It.IsAny<AppEngineFlexDeployment.DeploymentOptions>()), Times.Never);
        }

        [TestMethod]
        public void TestPublishCommandAsync_PublishesProject()
        {
            const string expectedVersion = "expected-options-version";
            _objectUnderTest.Version = expectedVersion;
            _validateGCloudSource.SetResult(s_validGCloudValidationResult);

            _objectUnderTest.PublishCommandAsync.Execute(null);

            _appEngineDeploymentMock.Verify(
                d => d.PublishProjectAsync(
                    _mockedPublishDialog.Project,
                    It.IsAny<AppEngineFlexDeployment.DeploymentOptions>()));
        }

        [TestMethod]
        public void TestPublishCommandAsync_PublishesProjectToVersion()
        {
            const string expectedVersion = "expected-options-version";
            _objectUnderTest.Version = expectedVersion;
            _validateGCloudSource.SetResult(s_validGCloudValidationResult);

            _objectUnderTest.PublishCommandAsync.Execute(null);

            _appEngineDeploymentMock.Verify(
                d => d.PublishProjectAsync(
                    It.IsAny<IParsedProject>(),
                    It.Is<AppEngineFlexDeployment.DeploymentOptions>(
                        options => options.Version == expectedVersion)));
        }

        [TestMethod]
        public void TestPublishCommandAsync_PublishesProjectToService()
        {
            const string expectedService = "expected-service";
            _objectUnderTest.Service = expectedService;
            _validateGCloudSource.SetResult(s_validGCloudValidationResult);

            _objectUnderTest.PublishCommandAsync.Execute(null);

            _appEngineDeploymentMock.Verify(
                d => d.PublishProjectAsync(
                    It.IsAny<IParsedProject>(),
                    It.Is<AppEngineFlexDeployment.DeploymentOptions>(
                        options => options.Service == expectedService)));
        }

        [TestMethod]
        public void TestPublishCommandAsync_PublishesProjectWithPromoteOption()
        {
            _objectUnderTest.Promote = false;
            _validateGCloudSource.SetResult(s_validGCloudValidationResult);

            _objectUnderTest.PublishCommandAsync.Execute(null);

            _appEngineDeploymentMock.Verify(
                d => d.PublishProjectAsync(
                    It.IsAny<IParsedProject>(),
                    It.Is<AppEngineFlexDeployment.DeploymentOptions>(
                        options => options.Promote == false)));
        }

        [TestMethod]
        public void TestPublishCommandAsync_PublishesProjectWithOpenWebsiteOption()
        {
            _objectUnderTest.OpenWebsite = false;
            _validateGCloudSource.SetResult(s_validGCloudValidationResult);

            _objectUnderTest.PublishCommandAsync.Execute(null);

            _appEngineDeploymentMock.Verify(
                d => d.PublishProjectAsync(
                    It.IsAny<IParsedProject>(),
                    It.Is<AppEngineFlexDeployment.DeploymentOptions>(
                        options => !options.OpenWebsite)));
        }

        [TestMethod]
        public void TestPublishCommandAsync_UpdatesVersionBeforeFinishFlow()
        {
            const string initalVersion = "initial-version";
            _objectUnderTest.Version = initalVersion;
            _validateGCloudSource.SetResult(s_validGCloudValidationResult);
            string versionWhenFinishFlow = null;
            Mock.Get(_mockedPublishDialog).Setup(pd => pd.FinishFlow())
                .Callback(() => versionWhenFinishFlow = _objectUnderTest.Version);

            _objectUnderTest.PublishCommandAsync.Execute(null);

            Assert.AreNotEqual(initalVersion, _objectUnderTest.Version);
            Assert.AreEqual(versionWhenFinishFlow, _objectUnderTest.Version);
        }

        [TestMethod]
        public async Task TestPublishCommandAsync_SkipsUpdateVersionForInvalidGCloud()
        {
            _validateGCloudSource.SetResult(s_invalidGCloudValidationResult);
            const string expectedVersion = "expected-version";
            _objectUnderTest.Version = expectedVersion;

            _objectUnderTest.PublishCommandAsync.Execute(null);
            await _objectUnderTest.PublishCommandAsync.LatestExecution.SafeTask;

            Assert.AreEqual(expectedVersion, _objectUnderTest.Version);
        }

        [TestMethod]
        public void TestPublishCommandAsync_FinishesFlowRegardlessOfPublishCompleting()
        {
            _validateGCloudSource.SetResult(s_validGCloudValidationResult);

            _objectUnderTest.PublishCommandAsync.Execute(null);

            Mock.Get(_mockedPublishDialog).Verify(pd => pd.FinishFlow());
            Assert.IsTrue(_objectUnderTest.PublishCommandAsync.LatestExecution.IsPending);
        }

        [TestMethod]
        public async Task TestPublishCommandAsync_SkipsFinishFlowForInvalidGCloud()
        {
            _validateGCloudSource.SetResult(s_invalidGCloudValidationResult);

            _objectUnderTest.PublishCommandAsync.Execute(null);
            await _objectUnderTest.PublishCommandAsync.LatestExecution.SafeTask;

            Mock.Get(_mockedPublishDialog).Verify(pd => pd.FinishFlow(), Times.Never);
        }
    }
}
