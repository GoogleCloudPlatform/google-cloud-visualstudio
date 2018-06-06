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
using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps.Flex;
using GoogleCloudExtension.Services.VsProject;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Project = Google.Apis.CloudResourceManager.v1.Data.Project;

namespace GoogleCloudExtensionUnitTests.PublishDialog.Steps.Flex
{
    [TestClass]
    public class FlexStepViewModelTests : ExtensionTestBase
    {
        private const string VisualStudioProjectName = "VisualStudioProjectName";
        private const string InvalidVersion = "-Invalid Version Name!";
        private const string ValidVersion = "valid-version-name";

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
        private IAppEngineFlexDeployment _mockedAppEngineFlexDeployment;
        private List<string> _propertiesChanges;

        [ClassInitialize]
        public static void BeforeAll(TestContext context) =>
            GcpPublishStepsUtils.NowOverride = DateTime.Parse("2088-12-23 01:01:01");

        [ClassCleanup]
        public static void AfterAll() => GcpPublishStepsUtils.NowOverride = null;

        protected override void BeforeEach()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            _propertyServiceMock = new Mock<IVsProjectPropertyService>();
            _mockedAppEngineFlexDeployment = Mock.Of<IAppEngineFlexDeployment>();

            PackageMock.Setup(p => p.GetService<IVsProjectPropertyService>()).Returns(_propertyServiceMock.Object);
            PackageMock.Setup(p => p.GetService<IAppEngineFlexDeployment>()).Returns(_mockedAppEngineFlexDeployment);

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
            _getApplicationTaskSource.SetResult(_mockedApplication);

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
            _getApplicationTaskSource.SetResult(_mockedApplication);
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
        public void TestSetServices_SetsServiceToFirst()
        {
            const string firstServiceId = "service-1";
            string[] services = { firstServiceId, "service-2" };

            _objectUnderTest.Services = services;

            Assert.AreEqual(firstServiceId, _objectUnderTest.Service);
        }

        [TestMethod]
        public void TestSetServices_SetsServiceFromAppYaml()
        {
            const string secondServiceId = "service-2";
            Mock.Get(_mockedAppEngineFlexDeployment).Setup(aed => aed.GetAppEngineService(It.IsAny<IParsedProject>()))
                .Returns(secondServiceId);
            string[] services = { "service-1", secondServiceId };

            _objectUnderTest.Services = services;

            Assert.AreEqual(secondServiceId, _objectUnderTest.Service);
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
            Mock.Get(_mockedAppEngineFlexDeployment).Setup(aed => aed.GetAppEngineService(It.IsAny<IParsedProject>()))
                .Returns(serviceId);

            _objectUnderTest.Service = serviceId;

            Assert.IsFalse(_objectUnderTest.UpdateAppYamlServiceEnabled);
        }

        [TestMethod]
        public void TestSetService_DifferentFromAppYamlEnablesUpdateAppYamlService()
        {
            Mock.Get(_mockedAppEngineFlexDeployment).Setup(aed => aed.GetAppEngineService(It.IsAny<IParsedProject>()))
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

            Mock.Get(_mockedAppEngineFlexDeployment).Verify(
                s => s.SaveServiceToAppYaml(_objectUnderTest.PublishDialog.Project, serviceId));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SkipsUpdateAppYamlWhenDisabled()
        {
            const string serviceId = "service-id";
            _objectUnderTest.UpdateAppYamlService = false;
            _objectUnderTest.Service = serviceId;

            _objectUnderTest.OnNotVisible();

            Mock.Get(_mockedAppEngineFlexDeployment).Verify(
                s => s.SaveServiceToAppYaml(_objectUnderTest.PublishDialog.Project, serviceId), Times.Never);
        }
    }
}
