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
        private const string ReloadedProjectId = "reloaded-project-id";
        private const string MockUserName = "UserName";

        private static readonly Project s_testProject = new Project { ProjectId = TestProjectId };
        private static readonly Project s_reloadedProject = new Project { ProjectId = ReloadedProjectId };

        private TaskCompletionSource<IList<Project>> _projectTaskSource;
        private Mock<IPickProjectIdWindow> _windowMock;
        private PickProjectIdViewModel _testObject;
        private IList<string> _properiesChanged;
        private IList<string> _loadTaskPropertiesChanged;
        private PropertyChangedEventHandler _addLoadTaskPropertyChanged;
        private PropertyChangedEventHandler _addPropertiesChanged;
        private PropertyChangedEventHandler _updateLoadTaskEvents;

        protected abstract Project DefaultProject { get; }
        protected abstract string DefaultProjectId { get; }

        [TestInitialize]
        public void BeforeEach()
        {
            CredentialsStore.Default.UpdateCurrentProject(DefaultProject);
            CredentialsStore.Default.CurrentAccount = new UserAccount { AccountName = MockUserName };
            _projectTaskSource = new TaskCompletionSource<IList<Project>>();
            _windowMock = new Mock<IPickProjectIdWindow>();
            _windowMock.Setup(window => window.Close()).Verifiable();
            _properiesChanged = new List<string>();
            _loadTaskPropertiesChanged = new List<string>();
            _addLoadTaskPropertyChanged = (sender, args) => _loadTaskPropertiesChanged.Add(args.PropertyName);
            _addPropertiesChanged = (sender, args) => _properiesChanged.Add(args.PropertyName);
            _updateLoadTaskEvents = (sender, args) =>
            {
                var model = sender as PickProjectIdViewModel;
                if (model?.LoadTask != null &&
                    (nameof(PickProjectIdViewModel.LoadTask).Equals(args.PropertyName) || args.PropertyName == null))
                {
                    model.LoadTask.PropertyChanged += _addLoadTaskPropertyChanged;
                }
            };
            _testObject = BuildTestObject();
        }

        private PickProjectIdViewModel BuildTestObject()
        {
            Func<Task<IList<Project>>> projectsListAsyncCallBack = async () =>
            {
                try
                {
                    return await _projectTaskSource.Task;
                }
                finally
                {
                    _projectTaskSource = new TaskCompletionSource<IList<Project>>();
                }
            };
            Func<IResourceManagerDataSource> dataSourceFactory =
                () => Mock.Of<IResourceManagerDataSource>(
                    ds => ds.GetSortedActiveProjectsAsync() == projectsListAsyncCallBack());
            var testObject = new PickProjectIdViewModel(_windowMock.Object, dataSourceFactory, () => { });
            testObject.PropertyChanged += _addPropertiesChanged;
            if (testObject.LoadTask != null)
            {
                testObject.LoadTask.PropertyChanged += _addLoadTaskPropertyChanged;
            }
            testObject.PropertyChanged += _updateLoadTaskEvents;
            return testObject;
        }

        [TestMethod]
        public void TestNoUser()
        {
            CredentialsStore.Default.CurrentAccount = null;
            _testObject = BuildTestObject();

            Assert.IsNull(_testObject.LoadTask);
            Assert.AreEqual(DefaultProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.IsNull(_testObject.Projects);
            Assert.AreEqual(DefaultProjectId != null, _testObject.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestNoUserOnReload()
        {
            CredentialsStore.Default.CurrentAccount = null;
            _testObject.ChangeUserCommand.Execute(null);

            Assert.IsNull(_testObject.LoadTask);
            Assert.AreEqual(DefaultProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.IsNull(_testObject.Projects);
            Assert.AreEqual(DefaultProjectId != null, _testObject.OkCommand.CanExecuteCommand);
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
            _projectTaskSource.SetResult(new[] { s_testProject });

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
                Assert.AreEqual(s_testProject, _testObject.SelectedProject);
            }
            else
            {
                Assert.AreEqual(0, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
                Assert.IsNull(_testObject.SelectedProject);
            }
            Assert.AreEqual(1, _testObject.Projects.Count);
            Assert.AreEqual(s_testProject, _testObject.Projects[0]);
            Assert.AreEqual(DefaultProjectId ?? TestProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
            Assert.IsTrue(_testObject.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void Test_SelectCommandOnTask()
        {
            _projectTaskSource.SetResult(new[] { s_testProject });
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
            _projectTaskSource.SetResult(new[] { s_testProject });

            Assert.IsFalse(_properiesChanged.Any(nameof(PickProjectIdViewModel.LoadTask).Equals));
            Assert.AreEqual(1, _loadTaskPropertiesChanged.Count);
            Assert.IsNull(_loadTaskPropertiesChanged.Single());
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.Projects).Equals));
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.SelectedProject).Equals));
            Assert.AreEqual(1, _testObject.Projects.Count);
            Assert.AreEqual(s_testProject, _testObject.Projects[0]);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(TestInputProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void Test_SelectCommandOnProjectInputBeforeLoad()
        {
            _testObject.ProjectId = TestInputProjectId;
            _projectTaskSource.SetResult(new[] { s_testProject });
            _testObject.OkCommand.Execute(null);

            _windowMock.Verify(window => window.Close());
            Assert.AreEqual(TestInputProjectId, _testObject.Result);
        }

        [TestMethod]
        public void Test_ReloadProjects()
        {
            _projectTaskSource.SetResult(new[] { s_testProject });
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
            _projectTaskSource.SetResult(new[] { s_testProject });
            _testObject.ProjectId = "";
            _testObject.ChangeUserCommand.Execute(null);
            _properiesChanged.Clear();
            _projectTaskSource.SetResult(new[] { s_reloadedProject });

            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.SelectedProject).Equals));
            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.Projects).Equals));
            Assert.AreEqual(ReloadedProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void Test_ReloadProjectsResult()
        {
            _projectTaskSource.SetResult(new[] { s_testProject });
            _testObject.ChangeUserCommand.Execute(null);
            _projectTaskSource.SetResult(new[] { s_reloadedProject });


            Assert.AreEqual(2, _loadTaskPropertiesChanged.Count);
            Assert.IsTrue(_loadTaskPropertiesChanged.All(name => name == null));
            Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.Projects).Equals));
            Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.SelectedProject).Equals));
            if (DefaultProjectId == null)
            {
                Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
                Assert.AreEqual(s_reloadedProject, _testObject.SelectedProject);
            }
            else
            {
                Assert.AreEqual(0, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
                Assert.IsNull(_testObject.SelectedProject);
            }
            Assert.AreEqual(1, _testObject.Projects.Count);
            Assert.AreEqual(s_reloadedProject, _testObject.Projects[0]);
            Assert.AreEqual(DefaultProjectId ?? ReloadedProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void Test_ReloadProjectsResultWithInput()
        {
            _projectTaskSource.SetResult(new[] { s_testProject });
            _testObject.ChangeUserCommand.Execute(null);
            _testObject.ProjectId = TestInputProjectId;
            _projectTaskSource.SetResult(new[] { s_reloadedProject });

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
            Assert.AreEqual(1, _testObject.Projects.Count);
            Assert.AreEqual(s_reloadedProject, _testObject.Projects[0]);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(TestInputProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void Test_ReloadProjectsResultWithEarlyInput()
        {
            _testObject.ProjectId = TestInputProjectId;
            _projectTaskSource.SetResult(new[] { s_testProject });
            _testObject.ChangeUserCommand.Execute(null);
            _projectTaskSource.SetResult(new[] { s_reloadedProject });

            Assert.AreEqual(1, _properiesChanged.Count(nameof(PickProjectIdViewModel.ProjectId).Equals));
            Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.Projects).Equals));
            Assert.AreEqual(2, _properiesChanged.Count(nameof(PickProjectIdViewModel.SelectedProject).Equals));
            Assert.AreEqual(1, _testObject.Projects.Count);
            Assert.AreEqual(s_reloadedProject, _testObject.Projects[0]);
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
