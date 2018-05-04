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
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.GceStep;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Project = Google.Apis.CloudResourceManager.v1.Data.Project;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.GceStep
{
    [TestClass]
    public class GceStepViewModelTests : ExtensionTestBase
    {
        //private IEnumerable<Instance> _expectedInstances;
        //private Instance _expectedSelectedInstance;
        //private IEnumerable<WindowsInstanceCredentials> _expectedCredentials;
        //private WindowsInstanceCredentials _expectedSelectedCredentials;
        //private bool _expectedManageCredentialsCommandCanExecute;

        private const string DefaultProjectId = "DefaultProjectId";
        private const string TargetProjectId = "TargetProjectId";
        private const string VisualStudioProjectName = "VisualStudioProjectName";
        private const string WindowsLicenseUrl = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2016-dc";
        private const string WindowsServer2012License = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2012-r2-dc";
        private const string WindowsServer2008License = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2008-r2-dc";
        private const string WindowsServer2016License = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2016-dc";
        private const string AnyOtherLicense = "https://www.googleapis.com/fake/license/for/tests";

        private static readonly Project s_targetProject = new Project { ProjectId = TargetProjectId };
        private static readonly Project s_defaultProject = new Project { ProjectId = DefaultProjectId };

        private static readonly Instance s_windowsInstance1 = new Instance
        {
            Name = "AInstace",
            Status = "RUNNING",
            Disks = new AttachedDisk[] { new AttachedDisk { Boot = true, Licenses = new string[] { WindowsServer2008License } } }
        };
        private static readonly Instance s_windowsInstance2 = new Instance
        {
            Name = "BInstace",
            Status = "STAGING",
            Disks = new AttachedDisk[] { new AttachedDisk { Boot = true, Licenses = new string[] { WindowsServer2008License } } }
        };
        private static readonly Instance s_windowsInstance3 = new Instance
        {
            Name = "CInstace",
            Status = "RUNNING",
            Disks = new AttachedDisk[] { new AttachedDisk { Boot = true, Licenses = new string[] { WindowsServer2016License } } }
        };
        private static readonly Instance s_anotherInstance = new Instance
        {
            Name = "DInstace",
            Status = "RUNNING",
            Disks = new AttachedDisk[] { new AttachedDisk { Boot = true, Licenses = new string[] { AnyOtherLicense } } }
        };
        private static readonly Instance s_windowsInstance4 = new Instance
        {
            Name = "EInstace",
            Status = "RUNNING",
            Disks = new AttachedDisk[] { new AttachedDisk { Boot = true, Licenses = new string[] { WindowsServer2012License } } }
        };
        private static readonly Instance s_windowsInstance5 = new Instance
        {
            Name = "FInstace",
            Status = "RUNNING",
            Disks = new AttachedDisk[] { new AttachedDisk { Boot = false, Licenses = new string[] { WindowsServer2012License } } }
        };
        private static readonly IList<Instance> s_mockedInstances = new List<Instance>
        {
            s_windowsInstance4, s_windowsInstance5, s_anotherInstance, s_windowsInstance2, s_windowsInstance1, s_windowsInstance3

        };
        private static readonly IList<Instance> s_expectedValidInstances = new List<Instance>
        {
            s_windowsInstance1, s_windowsInstance3, s_windowsInstance4
        };
        private static readonly WindowsInstanceCredentials s_credentials1 = new WindowsInstanceCredentials { User = "User1", Password = "Password1" };
        private static readonly WindowsInstanceCredentials s_credentials2 = new WindowsInstanceCredentials { User = "User2", Password = "Password2" };
        private static readonly WindowsInstanceCredentials s_credentials3 = new WindowsInstanceCredentials { User = "User3", Password = "Password3" };
        private static readonly IList<WindowsInstanceCredentials> s_mockedInstance1Credentials = new List<WindowsInstanceCredentials>
        {
            s_credentials1, s_credentials2
        };
        private static readonly IList<WindowsInstanceCredentials> s_mockedInstance3Credentials = new List<WindowsInstanceCredentials>
        {
            s_credentials3
        };

        private GceStepViewModel _objectUnderTest;
        private Mock<IApiManager> _apiManagerMock;
        private TaskCompletionSource<bool> _areServicesEnabledTaskSource;
        private TaskCompletionSource<object> _enableServicesTaskSource;
        private TaskCompletionSource<IList<Instance>> _getInstanceListTaskSource;
        private Mock<IGceDataSource> _dataSourceMock;
        private Mock<IWindowsCredentialsStore> _windowsCredentialStoreMock;
        private Mock<Action<Instance>> _manageCredentialsPromptMock;
        private IPublishDialog _mockedPublishDialog;
        private Mock<Func<Project>> _pickProjectPromptMock;
        private List<string> _changedProperties;

        protected override void BeforeEach()
        {
            base.BeforeEach();

            IParsedProject mockedProject = Mock.Of<IParsedProject>(p => p.Name == VisualStudioProjectName);

            Mock<IPublishDialog> publishDialogMock = new Mock<IPublishDialog>();
            publishDialogMock.Setup(pd => pd.Project).Returns(mockedProject);
            publishDialogMock.Setup(pd => pd.TrackTask(It.IsAny<Task>()));
            _mockedPublishDialog = publishDialogMock.Object;

            _pickProjectPromptMock = new Mock<Func<Project>>();
            _changedProperties = new List<string>();

            _apiManagerMock = new Mock<IApiManager>();
            _apiManagerMock.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>())).Returns(() => _areServicesEnabledTaskSource.Task);
            _apiManagerMock.Setup(x => x.EnableServicesAsync(It.IsAny<IEnumerable<string>>())).Returns(() => _enableServicesTaskSource.Task);

            _dataSourceMock = new Mock<IGceDataSource>();
            _dataSourceMock.Setup(ds => ds.GetInstanceListAsync()).Returns(() => _getInstanceListTaskSource.Task);

            _windowsCredentialStoreMock = new Mock<IWindowsCredentialsStore>();
            _manageCredentialsPromptMock = new Mock<Action<Instance>>();

            _objectUnderTest = GceStepViewModel.CreateStep(
                apiManager: _apiManagerMock.Object,
                pickProjectPrompt: _pickProjectPromptMock.Object,
                dataSource: _dataSourceMock.Object,
                currentWindowsCredentialStore: _windowsCredentialStoreMock.Object,
                manageCredentialsPrompt: _manageCredentialsPromptMock.Object);
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
        }

        protected override void AfterEach()
        {
            _objectUnderTest.OnFlowFinished();
        }

        [TestMethod]
        public void TestInitialState()
        {
            AssertInitialState();
        }

        [TestMethod]
        public void TestInitialStateNoProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);

            AssertSelectedProjectUnchanged();
            AssertInitialState();
        }

        [TestMethod]
        public void TestInitialStateProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnVisibleNoProject()
        {
            await OnVisibleWithProject(null);

            AssertSelectedProjectChanged();
            AssertNoProjectState();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestOnVisiblePositiveValidation()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertValidProjectValidInstancesCredentialsState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestOnVisibleNoInstances()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(new List<Instance>());

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertValidProjectNoInstancesState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestOnVisibleNoCredentials()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(new List<WindowsInstanceCredentials>());

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertValidProjectNoCredentialsState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public void TestOnVisibleLongRunningLoadingInstances()
        {
            InitAreServicesEnabledMock(true);
            InitLongRunningGetInstanceListMock();

            Task onVisibleTask = OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertValidProjectLongRunningInstancesState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestOnVisibleErrorLoadingInstances()
        {
            InitAreServicesEnabledMock(true);
            InitErrorGetInstanceListMock();

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertValidProjectErrorInInstancesState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestOnVisibleNegativeValidation()
        {
            InitAreServicesEnabledMock(false);

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public void TestOnVisibleLongRunningValidation()
        {
            InitLongRunningAreServicesEnabledMock();

            Task onVisibleTask = OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestOnVisibleErrorInValidation()
        {
            InitErrorAreServicesEnabledMock();

            await OnVisibleWithProject(s_defaultProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToNoExternal()
        {
            await OnVisibleWithProject(null);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectUnchanged();
            AssertNoProjectState();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToPositiveValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectValidInstancesCredentialsState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNegativeValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToLongRunningValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToErrorInValidationExternal()
        {
            await OnVisibleWithProject(null);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToNoExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectChanged();
            AssertNoProjectState();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToPositiveValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectValidInstancesCredentialsState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToNegativeValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToLongRunningValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToErrorInValidationExternal()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToNoExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(null);

            AssertSelectedProjectChanged();
            AssertNoProjectState();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToPositiveValidationExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectValidInstancesCredentialsState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNegativeValidationExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToLongRunningValidationExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToErrorInValidationExternal()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToNoSelectCommand()
        {
            await OnVisibleWithProject(null);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertNoProjectState();
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToPositiveValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectValidInstancesCredentialsState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromNoToNegativeValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToLongRunningValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromNoToErrorInValidationSelectCommand()
        {
            await OnVisibleWithProject(null);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToNoSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertValidProjectValidInstancesCredentialsState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectValidInstancesCredentialsState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToNegativeValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToLongRunningValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToErrorInValidationSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToSameValidSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertValidProjectValidInstancesCredentialsState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromValidToSameInvalidSelectCommand()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertInvalidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToNoInstancesSelectCommmand()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            InitGetInstanceListMock(new List<Instance>());
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectNoInstancesState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromValidToNoCredentialsSelectCommmand()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            InitGetCredentialsForInstanceMock(new List<WindowsInstanceCredentials>());
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedExternally(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectNoCredentialsState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNoSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertInvalidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToPositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectValidInstancesCredentialsState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToNegativeValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToLongRunningValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToErrorInValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitErrorAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromInvalidToSamePositiveValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertValidProjectValidInstancesCredentialsState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromInvalidToSameNegativeValidationSelectCommand()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_defaultProject);

            AssertSelectedProjectUnchanged();
            AssertInvalidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToNoSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(null);

            AssertSelectedProjectUnchanged();
            AssertErrorInValidationState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToPositiveValidationSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertValidProjectValidInstancesCredentialsState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToNegativeValidationSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(false);
            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertInvalidProjectState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToLongRunningValidationSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            InitLongRunningAreServicesEnabledMock();
            _changedProperties.Clear();
            ResetMockCalls();

            Task onProjectChangedTask = OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertLongRunningValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromErrorInValidationToErrorInValidationSelectCommand()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            await OnProjectChangedSelectProjectCommand(s_targetProject);

            AssertSelectedProjectChanged();
            AssertErrorInValidationState(TargetProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestFromOneInstanceToAnother()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            InitGetCredentialsForInstanceMock(s_mockedInstance3Credentials);
            _changedProperties.Clear();
            ResetMockCalls();

            _objectUnderTest.SelectedInstance = s_windowsInstance3;

            AssertSelectedProjectUnchanged();
            AssertValidProjectValidInstancesCredentialsState(DefaultProjectId,
                s_expectedValidInstances, s_windowsInstance3,
                s_mockedInstance3Credentials, s_credentials3);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestFromOneCredentialToAnother()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            _changedProperties.Clear();
            ResetMockCalls();

            _objectUnderTest.SelectedCredentials = s_credentials2;

            AssertSelectedProjectUnchanged();
            AssertValidProjectValidInstancesCredentialsState(DefaultProjectId,
                s_expectedValidInstances, s_windowsInstance1,
                s_mockedInstance1Credentials, s_credentials2);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestEnableAPIsCommandSuccess()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            InitEnableApiMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await RunEnableApiCommand();

            AssertSelectedProjectUnchanged();
            AssertValidProjectValidInstancesCredentialsState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Once());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestEnableAPIsCommandFailure()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            InitEnableApiMock();
            _changedProperties.Clear();
            ResetMockCalls();

            await RunEnableApiCommand();

            AssertSelectedProjectUnchanged();
            AssertInvalidProjectState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Once());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Never());
        }

        [TestMethod]
        public async Task TestManageCredentialsCommandSuccess()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(new List<WindowsInstanceCredentials>());
            await OnVisibleWithProject(s_defaultProject);

            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            InitManageCredentialsPromptMock();
            _changedProperties.Clear();
            ResetMockCalls();

            RunManageCredentialsCommand();

            AssertSelectedProjectUnchanged();
            AssertValidProjectValidInstancesCredentialsState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestManageCredentialsCommandFailure()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(new List<WindowsInstanceCredentials>());
            await OnVisibleWithProject(s_defaultProject);

            InitManageCredentialsPromptMock();
            _changedProperties.Clear();
            ResetMockCalls();

            RunManageCredentialsCommand();

            AssertSelectedProjectUnchanged();
            AssertValidProjectNoCredentialsState(DefaultProjectId);
            AssertAreServicesEnabledCalled(Times.Never());
            AssertGetInstancesCalled(Times.Never());
            AssertGetCredentialsForInstanceCalled(Times.Once());
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromValidState()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromValidEventHandling()
        {
            InitAreServicesEnabledMock(true);
            InitGetInstanceListMock(s_mockedInstances);
            InitGetCredentialsForInstanceMock(s_mockedInstance1Credentials);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromInvalidState()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromInvalidEventHandling()
        {
            InitAreServicesEnabledMock(false);
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromErrorState()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);

            RaiseFlowFinished();

            AssertInitialState();
        }

        [TestMethod]
        public async Task TestOnFlowFinishedFromErrorEventHandling()
        {
            InitErrorAreServicesEnabledMock();
            await OnVisibleWithProject(s_defaultProject);
            _changedProperties.Clear();

            RaiseFlowFinished();
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            AssertSelectedProjectUnchanged();
        }

        private void InitAreServicesEnabledMock(bool servicesEnabled)
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _areServicesEnabledTaskSource.SetResult(servicesEnabled);
        }

        private void InitLongRunningAreServicesEnabledMock()
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
        }

        private void InitErrorAreServicesEnabledMock()
        {
            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _areServicesEnabledTaskSource.SetException(new DataSourceException());
        }

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

        private void InitEnableApiMock()
        {
            _enableServicesTaskSource = new TaskCompletionSource<object>();
            _enableServicesTaskSource.SetResult(null);
        }

        private void InitManageCredentialsPromptMock()
        {
            _manageCredentialsPromptMock.Setup(p => p(It.IsAny<Instance>()));
        }

        private void ResetMockCalls()
        {
            _apiManagerMock.ResetCalls();
            _dataSourceMock.ResetCalls();
            _windowsCredentialStoreMock.ResetCalls();
        }

        private async Task OnVisibleWithProject(Project project)
        {
            CredentialsStore.Default.UpdateCurrentProject(project);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            await _objectUnderTest.AsyncAction;
        }

        private async Task OnProjectChangedExternally(Project changedTo)
        {
            CredentialsStore.Default.UpdateCurrentProject(changedTo);
            await _objectUnderTest.AsyncAction;
        }

        private async Task OnProjectChangedSelectProjectCommand(Project changedTo)
        {
            _pickProjectPromptMock.Setup(f => f()).Returns(changedTo);
            _objectUnderTest.SelectProjectCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        private async Task RunEnableApiCommand()
        {
            _objectUnderTest.EnableApiCommand.Execute(null);
            await _objectUnderTest.AsyncAction;
        }

        private void RunManageCredentialsCommand()
        {
            _objectUnderTest.ManageCredentialsCommand.Execute(null);
        }

        private void RaiseFlowFinished()
        {
            Mock<IPublishDialog> publishDialogMock = Mock.Get(_mockedPublishDialog);
            publishDialogMock.Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
        }

        private void AssertInitialState()
        {
            AssertDefaultInstancesCredentialsState();

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
            Assert.IsFalse(_objectUnderTest.ManageCredentialsCommand.CanExecuteCommand);
        }

        private void AssertNoProjectState()
        {
            AssertInvariantsAfterVisible();
            AssertDefaultInstancesCredentialsState();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsNull(_objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertValidProjectValidInstancesCredentialsState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProjectState(expectedProjectId);
            AssertValidInstancesCredentialsState();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.IsTrue(_objectUnderTest.ShowInputControls);
        }

        private void AssertValidProjectValidInstancesCredentialsState(string expectedProjectId,
            IList<Instance> expectedInstances, Instance expectedSelectedInstance,
            IList<WindowsInstanceCredentials> expectedCredentials, WindowsInstanceCredentials expectedSelectedCredential)
        {
            AssertInvariantsAfterVisible();
            AssertValidProjectState(expectedProjectId);
            AssertValidInstancesCredentialsState(expectedInstances, expectedSelectedInstance, expectedCredentials, expectedSelectedCredential);

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.IsTrue(_objectUnderTest.ShowInputControls);
        }

        private void AssertValidProjectNoInstancesState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProjectState(expectedProjectId);
            AssertDefaultInstancesCredentialsState();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsTrue(_objectUnderTest.ShowInputControls);
        }

        private void AssertValidProjectNoCredentialsState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProjectState(expectedProjectId);
            AssertNoCredentialsState();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsTrue(_objectUnderTest.ShowInputControls);
        }

        private void AssertValidProjectLongRunningInstancesState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProjectState(expectedProjectId);
            AssertDefaultInstancesCredentialsState();

            Assert.IsFalse(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsTrue(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertValidProjectErrorInInstancesState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertValidProjectState(expectedProjectId);
            AssertDefaultInstancesCredentialsState();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsTrue(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertValidProjectState(string expectedProjectId)
        {
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
        }

        private void AssertInvalidProjectState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertDefaultInstancesCredentialsState();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsTrue(_objectUnderTest.NeedsApiEnabled);
            Assert.IsTrue(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertLongRunningValidationState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertDefaultInstancesCredentialsState();

            Assert.IsFalse(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsTrue(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertErrorInValidationState(string expectedProjectId)
        {
            AssertInvariantsAfterVisible();
            AssertDefaultInstancesCredentialsState();

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsCompleted);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.EnableApiCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.ShowInputControls);
        }

        private void AssertDefaultInstancesCredentialsState()
        {
            CollectionAssert.AreEquivalent(new List<Instance>(), _objectUnderTest.Instances.ToList());
            Assert.IsNull(_objectUnderTest.SelectedInstance);
            CollectionAssert.AreEquivalent(new List<WindowsInstanceCredentials>(), _objectUnderTest.Credentials.ToList());
            Assert.IsNull(_objectUnderTest.SelectedCredentials);
            Assert.IsFalse(_objectUnderTest.ManageCredentialsCommand.CanExecuteCommand);
        }

        private void AssertValidInstancesCredentialsState()
        {
            AssertValidInstancesCredentialsState(
                s_expectedValidInstances, s_windowsInstance1,
                s_mockedInstance1Credentials, s_credentials1);
        }

        private void AssertValidInstancesCredentialsState(
            IList<Instance> expectedInstances, Instance expectedSelectedInstance,
            IList<WindowsInstanceCredentials> expectedCredentials, WindowsInstanceCredentials expectedSelectedCredential)
        {
            CollectionAssert.AreEqual(expectedInstances.ToList(), _objectUnderTest.Instances.ToList());
            Assert.AreEqual(expectedSelectedInstance, _objectUnderTest.SelectedInstance);
            CollectionAssert.AreEqual(expectedCredentials.ToList(), _objectUnderTest.Credentials.ToList());
            Assert.AreEqual(expectedSelectedCredential, _objectUnderTest.SelectedCredentials);
            Assert.IsTrue(_objectUnderTest.ManageCredentialsCommand.CanExecuteCommand);
        }

        private void AssertNoCredentialsState()
        {
            CollectionAssert.AreEqual(s_expectedValidInstances.ToList(), _objectUnderTest.Instances.ToList());
            Assert.AreEqual(s_windowsInstance1, _objectUnderTest.SelectedInstance);
            CollectionAssert.AreEqual(new List<WindowsInstanceCredentials>(), _objectUnderTest.Credentials.ToList());
            Assert.IsNull(_objectUnderTest.SelectedCredentials);
            Assert.IsTrue(_objectUnderTest.ManageCredentialsCommand.CanExecuteCommand);
        }

        private void AssertSelectedProjectChanged()
        {
            CollectionAssert.Contains(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
        }

        private void AssertSelectedProjectUnchanged()
        {
            CollectionAssert.DoesNotContain(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
        }

        private void AssertAreServicesEnabledCalled(Times times)
        {
            _apiManagerMock.Verify(api => api.AreServicesEnabledAsync(It.IsAny<List<string>>()), times);
        }

        private void AssertGetInstancesCalled(Times times)
        {
            _dataSourceMock.Verify(src => src.GetInstanceListAsync(), times);
        }

        private void AssertGetCredentialsForInstanceCalled(Times times)
        {
            _windowsCredentialStoreMock.Verify(s => s.GetCredentialsForInstance(It.IsAny<Instance>()), times);
        }

        private void AssertInvariantsAfterVisible()
        {
            Assert.IsNotNull(_objectUnderTest.AsyncAction);
            Assert.AreEqual(_mockedPublishDialog, _objectUnderTest.PublishDialog);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsTrue(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.HasErrors);
        }
    }
}
