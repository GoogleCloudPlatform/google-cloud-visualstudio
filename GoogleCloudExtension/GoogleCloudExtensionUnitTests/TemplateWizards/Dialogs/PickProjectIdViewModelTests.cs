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
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.TemplateWizards.Dialogs.ProjectIdDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.TemplateWizards.Dialogs
{
    public abstract class PickProjectIdViewModelTests
    {
        private const string TestExceptionMessage = "Test Exception";
        private const string TestProjectId = "loaded-project-id";
        private const string TestInputProjectId = "input-project-id";
        private const string ReloadedProjectID = "reloaded-project-id";
        protected abstract string DefaultProjectId { get; }

        private TaskCompletionSource<IList<Project>> _projectTaskSource;
        private Mock<IPickProjectIdWindow> _windowMock;
        private PickProjectIdViewModel _testObject;
        private readonly Project _testProject = new Project { ProjectId = TestProjectId };
        private IList<Project> _testProjectList;
        private readonly Project _reloadedProject = new Project { ProjectId = ReloadedProjectID };
        private List<Project> _reloadedProjectList;
        private IList<string> _properiesChanged;
        private IList<string> _loadTaskPropertiesChanged;
        protected abstract Project DefaultProject { get; }

        [TestInitialize]
        public void BeforeEach()
        {
            CredentialsStore.Default.UpdateCurrentProject(DefaultProject);
            _projectTaskSource = new TaskCompletionSource<IList<Project>>();
            _windowMock = new Mock<IPickProjectIdWindow>();
            _windowMock.Setup(window => window.Close()).Verifiable();
            Func<IResourceManagerDataSource> dataSourceFactory = () =>
            {
                var dataSourceMock = new Mock<IResourceManagerDataSource>();
                dataSourceMock.Setup(ds => ds.GetProjectsListAsync()).Returns(_projectTaskSource.Task);
                return dataSourceMock.Object;
            };
            _testObject = new PickProjectIdViewModel(_windowMock.Object, dataSourceFactory, () => { });
            _testProjectList = new[] { _testProject };
            _reloadedProjectList = new List<Project> { _reloadedProject };
            _properiesChanged = new List<string>();
            _testObject.PropertyChanged += (sender, args) => _properiesChanged.Add(args.PropertyName);
            _loadTaskPropertiesChanged = new List<string>();
            PropertyChangedEventHandler addLoadTaskPropertyChanged =
                (sender, args) => _loadTaskPropertiesChanged.Add(args.PropertyName);
            _testObject.LoadTask.PropertyChanged += addLoadTaskPropertyChanged;
            _testObject.PropertyChanged += (sender, args) =>
            {
                if (nameof(PickProjectIdViewModel.LoadTask).Equals(args.PropertyName))
                {
                    _testObject.LoadTask.PropertyChanged += addLoadTaskPropertyChanged;
                }
            };
        }

        [TestMethod]
        public void Test_LoadingState()
        {
            Assert.IsFalse(_testObject.LoadTask.IsCompleted, "Task should be running.");
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
            Assert.IsFalse(_loadTaskPropertiesChanged.Any());
            Assert.IsNull(_testObject.SelectedProject, "Selected project should be null while loading.");
            Assert.AreEqual(DefaultProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Projects, "Projects should be null while loading.");
            Assert.IsNull(_testObject.Result, "Result should be null.");
            Assert.IsFalse(_properiesChanged.Any());
            Assert.AreEqual(DefaultProjectId != null, _testObject.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void Test_LoadingWithProjectInput()
        {
            _testObject.ProjectId = TestInputProjectId;

            Assert.IsFalse(_testObject.LoadTask.IsCompleted, "Task should be running.");
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
            Assert.IsFalse(_loadTaskPropertiesChanged.Any());
            Assert.IsNull(_testObject.SelectedProject, "Selected project should be null while loading.");
            Assert.AreEqual(TestInputProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Projects, "Projects should be null while loading.");
            Assert.IsNull(_testObject.Result, "Result should be null.");
            Assert.AreEqual(1, _properiesChanged.Count);
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
            Assert.IsTrue(_testObject.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void Test_Skip()
        {
            _testObject.SkipCommand.Execute(null);

            _windowMock.Verify(window => window.Close());
            Assert.AreEqual("", _testObject.Result);
        }

        [TestMethod]
        public void Test_LoadingError()
        {
            _projectTaskSource.SetException(new Exception(TestExceptionMessage));

            Assert.IsTrue(_testObject.LoadTask.IsCompleted, "Task should not be running.");
            Assert.IsTrue(_testObject.LoadTask.IsError, "Task should be falulted.");
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
            Assert.IsFalse(_properiesChanged.Any(nameof(PickProjectIdViewModel.LoadTask).Equals));
            Assert.AreEqual(1, _loadTaskPropertiesChanged.Count, "Should have one property changed event.");
            Assert.IsNull(_loadTaskPropertiesChanged.Single(), "Should have set all properties changed.");
            Assert.AreEqual(TestExceptionMessage, _testObject.LoadTask.ErrorMessage);
            Assert.IsNull(_testObject.Projects);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(DefaultProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
            Assert.IsFalse(_properiesChanged.Any());
            Assert.AreEqual(DefaultProjectId != null, _testObject.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void Test_LoadingCanceled()
        {
            _projectTaskSource.SetCanceled();

            Assert.IsTrue(_testObject.LoadTask.IsCompleted, "Task should not be running.");
            Assert.IsTrue(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
            Assert.IsFalse(_properiesChanged.Any(nameof(PickProjectIdViewModel.LoadTask).Equals));
            Assert.AreEqual(1, _loadTaskPropertiesChanged.Count, "Should have one property changed event.");
            Assert.IsNull(_loadTaskPropertiesChanged.Single(), "Should have set all properties changed.");
            Assert.IsNull(_testObject.Projects);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(DefaultProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
            Assert.IsFalse(_properiesChanged.Any());
            Assert.AreEqual(DefaultProjectId != null, _testObject.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void Test_LoadCompleted()
        {
            _projectTaskSource.SetResult(_testProjectList);

            Assert.IsTrue(_testObject.LoadTask.IsCompleted, "Task should not be running.");
            Assert.IsTrue(_testObject.LoadTask.IsSuccess);
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_properiesChanged.Any(nameof(PickProjectIdViewModel.LoadTask).Equals));
            Assert.AreEqual(1, _loadTaskPropertiesChanged.Count, "Should have one property changed event.");
            Assert.IsNull(_loadTaskPropertiesChanged.Single(), "Should have set all properties changed.");
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.Projects).Equals));
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.SelectedProject).Equals));
            if (DefaultProjectId == null)
            {
                Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
                Assert.AreEqual(_testProject, _testObject.SelectedProject);
            }
            else
            {
                Assert.AreEqual(0, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
                Assert.IsNull(_testObject.SelectedProject);
            }
            Assert.AreEqual(_testProjectList, _testObject.Projects);
            Assert.AreEqual(DefaultProjectId ?? TestProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
            Assert.IsTrue(_testObject.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void Test_SelectCommandOnTask()
        {
            _projectTaskSource.SetResult(_testProjectList);
            _testObject.OkCommand.Execute(null);

            _windowMock.Verify(window => window.Close());
            Assert.AreEqual(DefaultProjectId ?? TestProjectId, _testObject.Result);
        }

        [TestMethod]
        public void Test_SelectCommandOnProjectInput()
        {
            _testObject.ProjectId = TestInputProjectId;
            _testObject.OkCommand.Execute(null);

            _windowMock.Verify(window => window.Close());
            Assert.AreEqual(TestInputProjectId, _testObject.Result);
        }

        [TestMethod]
        public void Test_InputBeforeLoad()
        {
            _testObject.ProjectId = TestInputProjectId;
            _projectTaskSource.SetResult(_testProjectList);

            Assert.IsFalse(_properiesChanged.Any(nameof(PickProjectIdViewModel.LoadTask).Equals));
            Assert.AreEqual(1, _loadTaskPropertiesChanged.Count);
            Assert.IsNull(_loadTaskPropertiesChanged.Single());
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.Projects).Equals));
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.SelectedProject).Equals));
            Assert.AreEqual(_testProjectList, _testObject.Projects);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(TestInputProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void Test_SelectCommandOnProjectInputBeforeLoad()
        {
            _testObject.ProjectId = TestInputProjectId;
            _projectTaskSource.SetResult(_testProjectList);
            _testObject.OkCommand.Execute(null);

            _windowMock.Verify(window => window.Close());
            Assert.AreEqual(TestInputProjectId, _testObject.Result);
        }

        [TestMethod]
        public void Test_ReloadProjects()
        {
            _projectTaskSource.SetResult(_testProjectList);
            _projectTaskSource = new TaskCompletionSource<IList<Project>>();
            _testObject.ChangeUserCommand.Execute(null);

            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.LoadTask).Equals));
            Assert.AreEqual(1, _loadTaskPropertiesChanged.Count);
            Assert.IsNull(_loadTaskPropertiesChanged.Single());
            Assert.IsFalse(_testObject.LoadTask.IsCompleted, "Task should be running.");
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
            Assert.AreEqual(DefaultProjectId ?? TestProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void Test_ReloadProjectsWithEmptyInput()
        {
            _projectTaskSource.SetResult(_testProjectList);
            _projectTaskSource = new TaskCompletionSource<IList<Project>>();
            _testObject.ProjectId = "";
            _testObject.ChangeUserCommand.Execute(null);
            _properiesChanged.Clear();
            _projectTaskSource.SetResult(_reloadedProjectList);

            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.SelectedProject).Equals));
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.Projects).Equals));
            Assert.AreEqual(ReloadedProjectID, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void Test_ReloadProjectsResult()
        {
            _projectTaskSource.SetResult(_testProjectList);
            _projectTaskSource = new TaskCompletionSource<IList<Project>>();
            _testObject.ChangeUserCommand.Execute(null);
            _projectTaskSource.SetResult(_reloadedProjectList);


            Assert.AreEqual(2, _loadTaskPropertiesChanged.Count);
            Assert.IsTrue(_loadTaskPropertiesChanged.All(name => name == null));
            Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.Projects).Equals));
            Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.SelectedProject).Equals));
            if (DefaultProjectId == null)
            {
                Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
                Assert.AreEqual(_reloadedProject, _testObject.SelectedProject);
            }
            else
            {
                Assert.AreEqual(0, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
                Assert.IsNull(_testObject.SelectedProject);
            }
            Assert.AreEqual(_reloadedProjectList, _testObject.Projects);
            Assert.AreEqual(DefaultProjectId ?? ReloadedProjectID, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void Test_ReloadProjectsResultWithInput()
        {
            _projectTaskSource.SetResult(_testProjectList);
            _projectTaskSource = new TaskCompletionSource<IList<Project>>();
            _testObject.ChangeUserCommand.Execute(null);
            _testObject.ProjectId = TestInputProjectId;
            _projectTaskSource.SetResult(_reloadedProjectList);

            Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.Projects).Equals));
            Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.SelectedProject).Equals));
            if (DefaultProjectId == null)
            {
                Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
            }
            else
            {
                Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
            }
            Assert.AreEqual(_reloadedProjectList, _testObject.Projects);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(TestInputProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void Test_ReloadProjectsResultWithEarlyInput()
        {
            _testObject.ProjectId = TestInputProjectId;
            _projectTaskSource.SetResult(_testProjectList);
            _projectTaskSource = new TaskCompletionSource<IList<Project>>();
            _testObject.ChangeUserCommand.Execute(null);
            _projectTaskSource.SetResult(_reloadedProjectList);

            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
            Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.Projects).Equals));
            Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.SelectedProject).Equals));
            Assert.AreEqual(_reloadedProjectList, _testObject.Projects);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(TestInputProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }
    }

    [TestClass]
    public class PickProjectIdVeiwModelTestsNoDefault : PickProjectIdViewModelTests
    {
        protected override string DefaultProjectId { get; } = null;
        protected override Project DefaultProject { get; } = null;
    }

    [TestClass]
    public class PickProjectIdViewModleTestsWithDefault : PickProjectIdViewModelTests
    {
        protected override string DefaultProjectId { get; } = "default-project-id";
        protected override Project DefaultProject => new Project { ProjectId = DefaultProjectId };
    }
}
