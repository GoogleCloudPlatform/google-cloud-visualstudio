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
using GoogleCloudExtension.PublishDialogSteps.GceStep;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestingHelpers;
using Project = Google.Apis.CloudResourceManager.v1.Data.Project;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.GceStep
{
    [TestClass]
    public class GceStepViewModelTests : ExtensionTestBase
    {
        private const string DefaultProjectId = "DefaultProjectId";
        private const string VisualStudioProjectName = "VisualStudioProjectName";
        private const string WindowsServer2008License = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2008-r2-dc";
        private const string WindowsServer2012License = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2012-r2-dc";
        private const string WindowsServer2016License = "https://www.googleapis.com/compute/v1/projects/windows-cloud/global/licenses/windows-server-2016-dc";
        private const string AnyOtherLicense = "https://www.googleapis.com/fake/license/for/tests";

        private static readonly Project s_defaultProject = new Project { ProjectId = DefaultProjectId };

        private static readonly Instance s_windows2008RunningInstance = new Instance
        {
            Name = "AInstace",
            Status = "RUNNING",
            Disks = new[] { new AttachedDisk { Boot = true, Licenses = new[] { WindowsServer2008License } } }
        };
        private static readonly Instance s_windows2008StagingInstance = new Instance
        {
            Name = "BInstace",
            Status = "STAGING",
            Disks = new[] { new AttachedDisk { Boot = true, Licenses = new[] { WindowsServer2008License } } }
        };
        private static readonly Instance s_windows2016Instance = new Instance
        {
            Name = "CInstace",
            Status = "RUNNING",
            Disks = new[] { new AttachedDisk { Boot = true, Licenses = new[] { WindowsServer2016License } } }
        };
        private static readonly Instance s_nonWindowsInstance = new Instance
        {
            Name = "DInstace",
            Status = "RUNNING",
            Disks = new[] { new AttachedDisk { Boot = true, Licenses = new[] { AnyOtherLicense } } }
        };
        private static readonly Instance s_windows2012Instance = new Instance
        {
            Name = "EInstace",
            Status = "RUNNING",
            Disks = new[] { new AttachedDisk { Boot = true, Licenses = new[] { WindowsServer2012License } } }
        };
        private static readonly Instance s_nonBootWindows2012Instance = new Instance
        {
            Name = "FInstace",
            Status = "RUNNING",
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

        private static readonly WindowsInstanceCredentials s_credentials = new WindowsInstanceCredentials { User = "User1", Password = "Password1" };

        private static readonly List<WindowsInstanceCredentials> s_credentialsList =
            new List<WindowsInstanceCredentials>
            {
                s_credentials,
                new WindowsInstanceCredentials {User = "User2", Password = "Password2"}
            };

        private GceStepViewModel _objectUnderTest;
        private TaskCompletionSource<IList<Instance>> _getInstanceListTaskSource;
        private Mock<IWindowsCredentialsStore> _windowsCredentialStoreMock;
        private Mock<Action<Instance>> _manageCredentialsPromptMock;
        private IPublishDialog _mockedPublishDialog;
        private Mock<Func<Project>> _pickProjectPromptMock;
        private List<string> _changedProperties;
        private int _canPublishChangedCount = 0;

        protected override void BeforeEach()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            _mockedPublishDialog = Mock.Of<IPublishDialog>(pd => pd.Project.Name == VisualStudioProjectName);

            _pickProjectPromptMock = new Mock<Func<Project>>();

            var mockedApiManager = Mock.Of<IApiManager>(m =>
               m.AreServicesEnabledAsync(It.IsAny<IList<string>>()) == Task.FromResult(true) &&
               m.EnableServicesAsync(It.IsAny<IEnumerable<string>>()) == Task.FromResult(true));


            _getInstanceListTaskSource = new TaskCompletionSource<IList<Instance>>();
            var mockedDataSource =
                Mock.Of<IGceDataSource>(ds => ds.GetInstanceListAsync() == _getInstanceListTaskSource.Task);

            _windowsCredentialStoreMock = new Mock<IWindowsCredentialsStore>();
            _manageCredentialsPromptMock = new Mock<Action<Instance>>();

            _objectUnderTest = GceStepViewModel.CreateStep(
                apiManager: mockedApiManager,
                pickProjectPrompt: _pickProjectPromptMock.Object,
                dataSource: mockedDataSource,
                currentWindowsCredentialStore: _windowsCredentialStoreMock.Object,
                manageCredentialsPrompt: _manageCredentialsPromptMock.Object);

            _changedProperties = new List<string>();
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
            _objectUnderTest.CanPublishChanged += (sender, args) => _canPublishChangedCount++;
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
            Assert.IsTrue(_objectUnderTest.OpenWebsite);
            Assert.IsFalse(_objectUnderTest.LaunchRemoteDebugger);
            Assert.IsInstanceOfType(_objectUnderTest.Content, typeof(GceStepContent));
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
            var credentials2 = new WindowsInstanceCredentials { User = "User2", Password = "Password2" };
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
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            _objectUnderTest.SelectedCredentials =
                new WindowsInstanceCredentials { User = "User2", Password = "Password2" };
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
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            _objectUnderTest.SelectedCredentials = null;
            _canPublishChangedCount = 0;

            _objectUnderTest.SelectedCredentials = new WindowsInstanceCredentials { User = "User2", Password = "Password2" };

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
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            CollectionAssert.That.IsEmpty(_objectUnderTest.Instances);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.Instances));
        }

        [TestMethod]
        public void TestClearLoadedProjectData_SetsSelectedInstanceToNull()
        {
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            Assert.IsNull(_objectUnderTest.SelectedInstance);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.SelectedInstance));
        }

        [TestMethod]
        public async Task TestLoadAnyProjectDataAsync_SetsInstancesToRunningWindowsInstances()
        {
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            _changedProperties.Clear();

            _getInstanceListTaskSource.SetResult(s_allInstances);
            await _objectUnderTest.AsyncAction;

            CollectionAssert.AreEqual(s_runningWindowsInstances.ToList(), _objectUnderTest.Instances.ToList());
        }

        [TestMethod]
        public async Task TestLoadAnyProjectDataAsync_SetsSelectedInstance()
        {
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            _changedProperties.Clear();

            _getInstanceListTaskSource.SetResult(s_allInstances);
            await _objectUnderTest.AsyncAction;

            CollectionAssert.Contains(s_runningWindowsInstances.ToList(), _objectUnderTest.SelectedInstance);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestNext_ThrowsNotSupportedException()
        {
            _objectUnderTest.Next();
        }
    }
}
