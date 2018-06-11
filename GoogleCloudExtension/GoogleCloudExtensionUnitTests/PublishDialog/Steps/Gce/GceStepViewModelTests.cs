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
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps.Gce;
using GoogleCloudExtension.Services.VsProject;
using GoogleCloudExtensionUnitTests.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestingHelpers;
using Project = Google.Apis.CloudResourceManager.v1.Data.Project;

namespace GoogleCloudExtensionUnitTests.PublishDialog.Steps.Gce
{
    [TestClass]
    public class GceStepViewModelTests : ExtensionTestBase
    {
        private const string VisualStudioProjectName = "VisualStudioProjectName";
        private const string WindowsServer2008License = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2008-r2-dc";
        private const string WindowsServer2012License = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2012-r2-dc";
        private const string WindowsServer2016License = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2016-dc";
        private const string AnyOtherLicense = "https://www.googleapis.com/fake/license/for/tests";

        private static readonly Instance s_windows2008RunningInstance = new Instance
        {
            Name = "AInstace",
            Status = "RUNNING",
            Zone = "https://zoneA",
            Disks = new[] { new AttachedDisk { Boot = true, Licenses = new[] { WindowsServer2008License } } }
        };
        private static readonly Instance s_windows2008StagingInstance = new Instance
        {
            Name = "BInstace",
            Status = "STAGING",
            Zone = "https://zoneB",
            Disks = new[] { new AttachedDisk { Boot = true, Licenses = new[] { WindowsServer2008License } } }
        };
        private static readonly Instance s_windows2016Instance = new Instance
        {
            Name = "CInstace",
            Status = "RUNNING",
            Zone = "https://zoneC",
            Disks = new[] { new AttachedDisk { Boot = true, Licenses = new[] { WindowsServer2016License } } }
        };
        private static readonly Instance s_nonWindowsInstance = new Instance
        {
            Name = "DInstace",
            Status = "RUNNING",
            Zone = "https://zoneD",
            Disks = new[] { new AttachedDisk { Boot = true, Licenses = new[] { AnyOtherLicense } } }
        };
        private static readonly Instance s_windows2012Instance = new Instance
        {
            Name = "EInstace",
            Status = "RUNNING",
            Zone = "https://zoneE",
            Disks = new[] { new AttachedDisk { Boot = true, Licenses = new[] { WindowsServer2012License } } }
        };
        private static readonly Instance s_nonBootWindows2012Instance = new Instance
        {
            Name = "FInstace",
            Status = "RUNNING",
            Zone = "https://zoneF",
            Disks = new[] { new AttachedDisk { Boot = false, Licenses = new[] { WindowsServer2012License } } }
        };

        private static readonly IList<Instance> s_allInstances = new List<Instance>
        {
            s_windows2012Instance,
            s_nonBootWindows2012Instance,
            s_nonWindowsInstance,
            s_windows2008StagingInstance,
            s_windows2008RunningInstance,
            s_windows2016Instance

        };

        private static readonly IList<Instance> s_runningWindowsInstances = new List<Instance>
        {
            s_windows2008RunningInstance,
            s_windows2016Instance,
            s_windows2012Instance
        };

        private static readonly WindowsInstanceCredentials s_credentials = new WindowsInstanceCredentials("User1", "Password1");

        private static readonly List<WindowsInstanceCredentials> s_credentialsList =
            new List<WindowsInstanceCredentials>
            {
                s_credentials,
                new WindowsInstanceCredentials ("User2", "Password2")
            };

        private GceStepViewModel _objectUnderTest;
        private TaskCompletionSource<IList<Instance>> _getInstanceListTaskSource;
        private Mock<IWindowsCredentialsStore> _windowsCredentialStoreMock;
        private Mock<Action<Instance>> _manageCredentialsPromptMock;
        private Mock<Func<Project>> _pickProjectPromptMock;
        private List<string> _changedProperties;
        private int _canPublishChangedCount = 0;
        private Mock<IVsProjectPropertyService> _propertyServiceMock;
        private FakeParsedProject _parsedProject;

        protected override void BeforeEach()
        {
            _propertyServiceMock = new Mock<IVsProjectPropertyService>();
            PackageMock.Setup(p => p.GetMefService<IVsProjectPropertyService>()).Returns(_propertyServiceMock.Object);


            _pickProjectPromptMock = new Mock<Func<Project>>();

            var mockedApiManager = Mock.Of<IApiManager>(m =>
               m.AreServicesEnabledAsync(It.IsAny<IList<string>>()) == Task.FromResult(true) &&
               m.EnableServicesAsync(It.IsAny<IEnumerable<string>>()) == Task.FromResult(true));


            _getInstanceListTaskSource = new TaskCompletionSource<IList<Instance>>();
            var mockedDataSource =
                Mock.Of<IGceDataSource>(ds => ds.GetInstanceListAsync() == _getInstanceListTaskSource.Task);

            _windowsCredentialStoreMock = new Mock<IWindowsCredentialsStore>();
            _manageCredentialsPromptMock = new Mock<Action<Instance>>();

            _parsedProject = new FakeParsedProject { Name = VisualStudioProjectName };
            _objectUnderTest = new GceStepViewModel(
                mockedDataSource, mockedApiManager, _pickProjectPromptMock.Object, _windowsCredentialStoreMock.Object,
                _manageCredentialsPromptMock.Object, Mock.Of<IPublishDialog>(pd => pd.Project == _parsedProject));

            _changedProperties = new List<string>();
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
            _objectUnderTest.PublishCommand.CanExecuteChanged += (sender, args) => _canPublishChangedCount++;
        }

        protected override void AfterEach()
        {
            _objectUnderTest.OnFlowFinished();
        }

        [TestMethod]
        public void TestInitialState()
        {
            CollectionAssert.That.IsEmpty(_objectUnderTest.Instances);
            Assert.IsNull(_objectUnderTest.SelectedInstance);
            CollectionAssert.That.IsEmpty(_objectUnderTest.Credentials);
            Assert.IsNull(_objectUnderTest.SelectedCredentials);
            Assert.IsFalse(_objectUnderTest.ManageCredentialsCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.RefreshInstancesCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.OpenWebsite);
            Assert.IsFalse(_objectUnderTest.LaunchRemoteDebugger);
        }

        [TestMethod]
        public void TestSetSelectedInstance()
        {
            _objectUnderTest.SelectedInstance = s_nonWindowsInstance;

            Assert.AreEqual(s_nonWindowsInstance, _objectUnderTest.SelectedInstance);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.SelectedInstance));
        }

        [TestMethod]
        public void TestSetSelectedInstance_UpdatesCredentials()
        {
            _windowsCredentialStoreMock.Setup(s => s.GetCredentialsForInstance(s_windows2016Instance))
                .Returns(s_credentialsList);

            _objectUnderTest.SelectedInstance = s_windows2016Instance;

            Assert.AreEqual(s_credentialsList, _objectUnderTest.Credentials);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.Credentials));
        }

        [TestMethod]
        public void TestSetSelectedInstance_ToNullClearsCredentials()
        {
            _objectUnderTest.SelectedInstance = null;

            Assert.AreEqual(0, _objectUnderTest.Credentials.Count());
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.Credentials));
        }

        [TestMethod]
        public void TestSetSelectedInstance_ToNullDisablesManagedCredentialsCommand()
        {
            _objectUnderTest.ManageCredentialsCommand.CanExecuteCommand = true;

            _objectUnderTest.SelectedInstance = null;

            Assert.IsFalse(_objectUnderTest.ManageCredentialsCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestSetSelectedInstance_ToEnablesManagedCredentialsCommand()
        {
            _objectUnderTest.ManageCredentialsCommand.CanExecuteCommand = false;

            _objectUnderTest.SelectedInstance = s_windows2016Instance;

            Assert.IsTrue(_objectUnderTest.ManageCredentialsCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestSetSelectedCredentials()
        {
            var credentials2 = new WindowsInstanceCredentials("User2", "Password2");
            _objectUnderTest.SelectedCredentials = credentials2;

            Assert.AreEqual(credentials2, _objectUnderTest.SelectedCredentials);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.SelectedCredentials));
        }

        [TestMethod]
        public void TestSetSelectedCredentials_ToNullDisablesCanPublish()
        {
            _getInstanceListTaskSource.SetResult(s_allInstances);
            _windowsCredentialStoreMock.Setup(s => s.GetCredentialsForInstance(s_windows2016Instance))
                .Returns(s_credentialsList);
            _objectUnderTest.OnVisible();
            _objectUnderTest.SelectedCredentials =
                new WindowsInstanceCredentials("User2", "Password2");
            _canPublishChangedCount = 0;

            _objectUnderTest.SelectedCredentials = null;

            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.AreEqual(1, _canPublishChangedCount);
        }

        [TestMethod]
        public void TestSetSelectedCredentials_EnablesCanPublish()
        {
            _getInstanceListTaskSource.SetResult(s_allInstances);
            _windowsCredentialStoreMock.Setup(s => s.GetCredentialsForInstance(s_windows2016Instance))
                .Returns(s_credentialsList);
            _objectUnderTest.OnVisible();
            _objectUnderTest.SelectedCredentials = null;
            _canPublishChangedCount = 0;

            _objectUnderTest.SelectedCredentials = new WindowsInstanceCredentials("User2", "Password2");

            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.AreEqual(1, _canPublishChangedCount);
        }

        [TestMethod]
        public void TestSetOpenWebsite()
        {
            _objectUnderTest.OpenWebsite = false;

            Assert.IsFalse(_objectUnderTest.OpenWebsite);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.OpenWebsite));
        }

        [TestMethod]
        public void TestSetLaunchRemoteDebugger()
        {
            _objectUnderTest.LaunchRemoteDebugger = true;

            Assert.IsTrue(_objectUnderTest.LaunchRemoteDebugger);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.LaunchRemoteDebugger));
        }

        [TestMethod]
        public void TestManageCredentialsCommand_PromptsManageCredentials()
        {
            _objectUnderTest.SelectedInstance = s_windows2016Instance;

            _objectUnderTest.ManageCredentialsCommand.Execute(null);

            _manageCredentialsPromptMock.Verify(f => f(s_windows2016Instance), Times.Once);
        }

        [TestMethod]
        public void TestManageCredentialsCommand_WithNullSelectedInstanceEmptiesCredentials()
        {
            _objectUnderTest.SelectedInstance = null;

            _objectUnderTest.ManageCredentialsCommand.Execute(null);

            CollectionAssert.That.IsEmpty(_objectUnderTest.Credentials);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.Credentials));
        }

        [TestMethod]
        public void TestManageCredentialsCommand_SetsCredentials()
        {
            _objectUnderTest.SelectedInstance = s_windows2016Instance;
            _windowsCredentialStoreMock.Setup(s => s.GetCredentialsForInstance(s_windows2016Instance))
                .Returns(s_credentialsList);

            _objectUnderTest.ManageCredentialsCommand.Execute(null);

            CollectionAssert.AreEqual(s_credentialsList, _objectUnderTest.Credentials.ToList());
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.Credentials));
        }

        [TestMethod]
        public void TestClearLoadedProjectData_ClearsInstances()
        {
            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.LoadProjectTask.IsPending);
            CollectionAssert.That.IsEmpty(_objectUnderTest.Instances);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.Instances));
        }

        [TestMethod]
        public void TestClearLoadedProjectData_SetsSelectedInstanceToNull()
        {
            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.LoadProjectTask.IsPending);
            Assert.IsNull(_objectUnderTest.SelectedInstance);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.SelectedInstance));
        }

        [TestMethod]
        public async Task TestLoadAnyProjectDataAsync_SetsInstancesToRunningWindowsInstances()
        {
            _objectUnderTest.OnVisible();
            _changedProperties.Clear();

            _getInstanceListTaskSource.SetResult(s_allInstances);
            await _objectUnderTest.LoadProjectTask.SafeTask;

            CollectionAssert.AreEqual(s_runningWindowsInstances.ToList(), _objectUnderTest.Instances.ToList());
        }

        [TestMethod]
        public async Task TestLoadAnyProjectDataAsync_SetsSelectedInstance()
        {
            _objectUnderTest.OnVisible();
            _changedProperties.Clear();

            _getInstanceListTaskSource.SetResult(s_allInstances);
            await _objectUnderTest.LoadProjectTask.SafeTask;

            CollectionAssert.Contains(s_runningWindowsInstances.ToList(), _objectUnderTest.SelectedInstance);
        }

        [TestMethod]
        public async Task TestRefreshInstancesCommand_RefreshesInstances()
        {
            _getInstanceListTaskSource.SetResult(s_allInstances);

            _objectUnderTest.RefreshInstancesCommand.Execute(null);
            await _objectUnderTest.LoadProjectTask.SafeTask;

            CollectionAssert.AreEqual(s_runningWindowsInstances.ToList(), _objectUnderTest.Instances.ToList());
        }

        [TestMethod]
        public void TestRefreshInstancesCommand_EnabledByValidProject()
        {
            _getInstanceListTaskSource.SetResult(s_allInstances);
            _objectUnderTest.OnVisible();

            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns("project-id");
            CredentialStoreMock.Raise(cs => cs.CurrentProjectIdChanged += null, CredentialsStore.Default, null);

            Assert.IsTrue(_objectUnderTest.RefreshInstancesCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestRefreshInstancesCommand_DisabledByInvalidProject()
        {
            _getInstanceListTaskSource.SetResult(s_allInstances);
            _objectUnderTest.OnVisible();

            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(() => null);
            CredentialStoreMock.Raise(
                cs => cs.CurrentProjectIdChanged += null, CredentialsStore.Default, null);

            Assert.IsFalse(_objectUnderTest.RefreshInstancesCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsInstanceProperties()
        {
            Instance targetInstance = s_windows2012Instance;
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_parsedProject.Project, GceStepViewModel.InstanceNameProjectPropertyName))
                .Returns(targetInstance.Name);
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_parsedProject.Project, GceStepViewModel.InstanceZoneProjectPropertyName))
                .Returns(targetInstance.GetZoneName());

            _getInstanceListTaskSource.SetResult(s_allInstances);
            _objectUnderTest.OnVisible();

            Assert.AreEqual(targetInstance, _objectUnderTest.SelectedInstance);
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsCredentialProperty()
        {
            var targetCredentials = new WindowsInstanceCredentials("user2", "passwrod");
            _windowsCredentialStoreMock.Setup(s => s.GetCredentialsForInstance(It.IsAny<Instance>())).Returns(
                new[]
                {
                    new WindowsInstanceCredentials("user1", "password"),
                    targetCredentials,
                    new WindowsInstanceCredentials("user3", "passwrod")
                });
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_parsedProject.Project, GceStepViewModel.InstanceUserNameProjectPropertyName))
                .Returns(targetCredentials.User);
            _getInstanceListTaskSource.SetResult(s_allInstances);

            _objectUnderTest.OnVisible();

            Assert.AreEqual(targetCredentials, _objectUnderTest.SelectedCredentials);
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsOpenWebsiteProperty()
        {
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_parsedProject.Project, GceStepViewModel.OpenWebsiteProjectPropertyName))
                .Returns(bool.FalseString);

            _objectUnderTest.OnVisible();

            Assert.IsFalse(_objectUnderTest.OpenWebsite);
        }

        [TestMethod]
        public void TestLoadProjectProperties_HandlesNonBoolOpenWebsiteProperty()
        {
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_parsedProject.Project, GceStepViewModel.OpenWebsiteProjectPropertyName))
                .Returns("unparseable");

            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.OpenWebsite);
        }

        [TestMethod]
        public void TestLoadProjectProperties_LoadsLaunchRemoteDebuggerProperty()
        {
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_parsedProject.Project, GceStepViewModel.LaunchRemoteDebuggerProjectPropertyName))
                .Returns(bool.TrueString);

            _objectUnderTest.OnVisible();

            Assert.IsTrue(_objectUnderTest.LaunchRemoteDebugger);
        }

        [TestMethod]
        public void TestLoadProjectProperties_HandlesNonBoolLaunchRemoteDebuggerProperty()
        {
            _propertyServiceMock
                .Setup(s => s.GetUserProperty(_parsedProject.Project, GceStepViewModel.LaunchRemoteDebuggerProjectPropertyName))
                .Returns("unparseable");

            _objectUnderTest.OnVisible();

            Assert.IsFalse(_objectUnderTest.LaunchRemoteDebugger);
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesInstanceNameProperty()
        {
            Instance targetInstance = s_windows2012Instance;
            _objectUnderTest.SelectedInstance = targetInstance;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, GceStepViewModel.InstanceNameProjectPropertyName, targetInstance.Name));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesInstanceZoneProperty()
        {
            Instance targetInstance = s_windows2012Instance;
            _objectUnderTest.SelectedInstance = targetInstance;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, GceStepViewModel.InstanceZoneProjectPropertyName, targetInstance.GetZoneName()));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesInstanceUserNameProperty()
        {
            const string userName = "testUserName";
            _objectUnderTest.SelectedCredentials = new WindowsInstanceCredentials(userName, "password");

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, GceStepViewModel.InstanceUserNameProjectPropertyName, userName));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesOpenWebsiteProperty()
        {
            _objectUnderTest.OpenWebsite = false;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, GceStepViewModel.OpenWebsiteProjectPropertyName, bool.FalseString));
        }

        [TestMethod]
        public void TestSaveProjectProperties_SavesLaunchRemoteDebuggerProperty()
        {
            _objectUnderTest.LaunchRemoteDebugger = true;

            _objectUnderTest.OnNotVisible();

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, GceStepViewModel.LaunchRemoteDebuggerProjectPropertyName, bool.TrueString));
        }
    }
}
