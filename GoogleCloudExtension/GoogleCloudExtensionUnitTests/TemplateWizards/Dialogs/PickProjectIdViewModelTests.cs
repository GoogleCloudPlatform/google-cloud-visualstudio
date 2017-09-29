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

    [TestClass]
    public class PickProjectIdViewModelTests
    {
        private const string TestExceptionMessage = "Test Exception";
        private const string TestProjectId = "loaded-project-id";
        private const string TestInputProjectId = "input-project-id";
        private const string ReloadedProjectId = "reloaded-project-id";
        private const string MockUserName = "UserName";

        private static readonly Project s_defaultProject = new Project { ProjectId = DefaultProjectId };
        private static readonly Project s_testProject = new Project { ProjectId = TestProjectId };
        private static readonly Project s_reloadedProject = new Project { ProjectId = ReloadedProjectId };

        private TaskCompletionSource<IList<Project>> _projectTaskSource;
        private Mock<IPickProjectIdWindow> _windowMock;
        private PickProjectIdViewModel _testObject;
        private List<string> _properiesChanged;
        private PropertyChangedEventHandler _addPropertiesChanged;
        private Mock<Action> _manageAccoutMock;
        private static readonly UserAccount s_defaultAccount = new UserAccount { AccountName = MockUserName };

        private const string DefaultProjectId = "default-project-id";


        [TestInitialize]
        public void BeforeEach()
        {
            _testObject = null;
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            CredentialsStore.Default.CurrentAccount = s_defaultAccount;
            _projectTaskSource = new TaskCompletionSource<IList<Project>>();
            _windowMock = new Mock<IPickProjectIdWindow>();
            _windowMock.Setup(window => window.Close()).Verifiable();
            _properiesChanged = new List<string>();
            _addPropertiesChanged = (sender, args) => _properiesChanged.Add(args.PropertyName);
            _manageAccoutMock = new Mock<Action>();
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
            var testObject = new PickProjectIdViewModel(_windowMock.Object, dataSourceFactory, _manageAccoutMock.Object);
            testObject.PropertyChanged += _addPropertiesChanged;
            return testObject;
        }

        [TestMethod]
        public void TestInitialConditionsNoUserOrProject()
        {
            CredentialsStore.Default.CurrentAccount = null;
            CredentialsStore.Default.UpdateCurrentProject(null);

            _testObject = BuildTestObject();

            Assert.IsNull(_testObject.LoadTask);
            Assert.IsNull(_testObject.ProjectId);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.IsNull(_testObject.Projects);
            Assert.IsNull(_testObject.Result);
            Assert.IsFalse(_testObject.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestInitialConditionsDefaultProjectNoUser()
        {
            CredentialsStore.Default.CurrentAccount = null;
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            _testObject = BuildTestObject();

            Assert.IsNull(_testObject.LoadTask);
            Assert.IsNull(_testObject.Projects);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(DefaultProjectId, _testObject.ProjectId);
            Assert.IsTrue(_testObject.OkCommand.CanExecuteCommand);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void TestInitialConditionsDefaultUserNoProject()
        {
            CredentialsStore.Default.CurrentAccount = s_defaultAccount;
            CredentialsStore.Default.UpdateCurrentProject(null);

            _testObject = BuildTestObject();

            Assert.IsFalse(_testObject.LoadTask.IsCompleted, "Task should be running.");
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
            Assert.IsNull(_testObject.Projects);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.IsNull(_testObject.ProjectId);
            Assert.IsFalse(_testObject.OkCommand.CanExecuteCommand);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void TestInitialConditionsDefaultUserAndProject()
        {
            CredentialsStore.Default.CurrentAccount = s_defaultAccount;
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);

            _testObject = BuildTestObject();

            Assert.IsFalse(_testObject.LoadTask.IsCompleted, "Task should be running.");
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
            Assert.IsNull(_testObject.Projects, "Projects should be null while loading.");
            Assert.IsNull(_testObject.SelectedProject, "Selected project should be null while loading.");
            Assert.AreEqual(DefaultProjectId, _testObject.ProjectId);
            Assert.IsTrue(_testObject.OkCommand.CanExecuteCommand);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void TestChangeUserCommandNoUser()
        {
            _testObject = BuildTestObject();

            CredentialsStore.Default.CurrentAccount = null;
            _testObject.ChangeUserCommand.Execute(null);

            _manageAccoutMock.Verify(f => f(), Times.Once);
            Assert.IsNull(_testObject.LoadTask);
        }

        [TestMethod]
        public void TestChangeUserCommandWithUser()
        {
            CredentialsStore.Default.CurrentAccount = null;
            _testObject = BuildTestObject();

            CredentialsStore.Default.CurrentAccount = s_defaultAccount;
            _testObject.ChangeUserCommand.Execute(null);

            _manageAccoutMock.Verify(f => f(), Times.Once);
            Assert.IsFalse(_testObject.LoadTask.IsCompleted, "Task should be running.");
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
        }

        [TestMethod]
        public void TestSetProjectIdInputWhileLoading()
        {
            _testObject = BuildTestObject();

            _testObject.ProjectId = TestInputProjectId;

            Assert.IsFalse(_testObject.LoadTask.IsCompleted, "Task should be running.");
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
            Assert.IsNull(_testObject.SelectedProject, "Selected project should be null while loading.");
            Assert.AreEqual(TestInputProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Projects, "Projects should be null while loading.");
            CollectionAssert.AreEqual(new[] { nameof(PickProjectIdViewModel.ProjectId) }, _properiesChanged);
            Assert.IsTrue(_testObject.OkCommand.CanExecuteCommand);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void TestSkipCommand()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _testObject = BuildTestObject();

            _testObject.SkipCommand.Execute(null);

            _windowMock.Verify(window => window.Close());
            Assert.AreEqual("", _testObject.Result);
            Assert.AreEqual(DefaultProjectId, CredentialsStore.Default.CurrentProjectId);
        }

        [TestMethod]
        public void TestErrorWhileLoading()
        {
            _testObject = BuildTestObject();

            _projectTaskSource.SetException(new Exception(TestExceptionMessage));

            Assert.IsTrue(_testObject.LoadTask.IsCompleted, "Task should not be running.");
            Assert.IsTrue(_testObject.LoadTask.IsError, "Task should be falulted.");
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
            Assert.AreEqual(TestExceptionMessage, _testObject.LoadTask.ErrorMessage);
            Assert.IsNull(_testObject.Projects);
        }

        [TestMethod]
        public void TestCanceledLoading()
        {
            _testObject = BuildTestObject();

            _projectTaskSource.SetCanceled();

            Assert.IsTrue(_testObject.LoadTask.IsCompleted, "Task should not be running.");
            Assert.IsTrue(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
            Assert.IsNull(_testObject.Projects);
        }

        [TestMethod]
        public void TestCompleteLoadingNoDefaultProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            _testObject = BuildTestObject();

            _projectTaskSource.SetResult(new[] { s_testProject });

            Assert.IsTrue(_testObject.LoadTask.IsCompleted, "Task should not be running.");
            Assert.IsTrue(_testObject.LoadTask.IsSuccess);
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject),
                    nameof(PickProjectIdViewModel.ProjectId)
                },
                _properiesChanged);
            CollectionAssert.AreEqual(new[] { s_testProject }, _testObject.Projects.ToList());
            Assert.AreEqual(s_testProject, _testObject.SelectedProject);
            Assert.AreEqual(TestProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
            Assert.IsTrue(_testObject.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestCompleteLoadingMissingDefaultProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _testObject = BuildTestObject();

            _projectTaskSource.SetResult(new[] { s_testProject });

            Assert.IsTrue(_testObject.LoadTask.IsCompleted, "Task should not be running.");
            Assert.IsTrue(_testObject.LoadTask.IsSuccess);
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject)
                },
                _properiesChanged, string.Join(", ", _properiesChanged));
            CollectionAssert.AreEqual(new[] { s_testProject }, _testObject.Projects.ToList());
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(DefaultProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
            Assert.IsTrue(_testObject.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestCompleteLoadingIncludedDefaultProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _testObject = BuildTestObject();

            _projectTaskSource.SetResult(new[] { s_testProject, s_defaultProject });

            Assert.IsTrue(_testObject.LoadTask.IsCompleted, "Task should not be running.");
            Assert.IsTrue(_testObject.LoadTask.IsSuccess);
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject),
                    nameof(PickProjectIdViewModel.ProjectId)
                },
                _properiesChanged, string.Join(", ", _properiesChanged));
            CollectionAssert.AreEqual(new[] { s_testProject, s_defaultProject }, _testObject.Projects.ToList());
            Assert.AreEqual(s_defaultProject, _testObject.SelectedProject);
            Assert.AreEqual(DefaultProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
            Assert.IsTrue(_testObject.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestOkCommand()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            _testObject = BuildTestObject();
            _projectTaskSource.SetResult(new[] { s_testProject });

            _testObject.OkCommand.Execute(null);

            _windowMock.Verify(window => window.Close());
            Assert.AreEqual(TestProjectId, _testObject.Result);
            Assert.AreEqual(TestProjectId, CredentialsStore.Default.CurrentProjectId);
        }

        [TestMethod]
        public void TestOkCommandOnProjectInput()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _testObject = BuildTestObject();

            _testObject.ProjectId = TestInputProjectId;
            _testObject.OkCommand.Execute(null);

            _windowMock.Verify(window => window.Close());
            Assert.AreEqual(TestInputProjectId, _testObject.Result);
            Assert.AreEqual(DefaultProjectId, CredentialsStore.Default.CurrentProjectId);
        }

        [TestMethod]
        public void TestLoadCompleteAfterInput()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            _testObject = BuildTestObject();

            _testObject.ProjectId = TestInputProjectId;
            _projectTaskSource.SetResult(new[] { s_testProject });

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.ProjectId),
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject)
                },
                _properiesChanged, string.Join(", ", _properiesChanged));
            CollectionAssert.AreEqual(new[] { s_testProject }, _testObject.Projects.ToList());
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(TestInputProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void TestReloadProjects()
        {
            _testObject = BuildTestObject();
            _projectTaskSource.SetResult(new[] { s_testProject });
            _properiesChanged.Clear();

            _testObject.ChangeUserCommand.Execute(null);

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.LoadTask)
                },
                _properiesChanged,
                string.Join(", ", _properiesChanged));
            Assert.IsFalse(_testObject.LoadTask.IsCompleted, "Task should be running.");
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
        }

        [TestMethod]
        public void TestReloadProjectsWithEmptyInput()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _testObject = BuildTestObject();
            _projectTaskSource.SetResult(new[] { s_testProject });
            _properiesChanged.Clear();

            _testObject.ProjectId = "";
            _testObject.ChangeUserCommand.Execute(null);
            _projectTaskSource.SetResult(new[] { s_reloadedProject });

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.ProjectId),
                    nameof(PickProjectIdViewModel.LoadTask),
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject),
                    nameof(PickProjectIdViewModel.ProjectId)
                },
                _properiesChanged, string.Join(", ", _properiesChanged));
            Assert.AreEqual(s_reloadedProject, _testObject.SelectedProject);
            Assert.AreEqual(ReloadedProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void TestReloadProjectsWithNoDefault()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            _testObject = BuildTestObject();

            _projectTaskSource.SetResult(new[] { s_testProject });
            _testObject.ChangeUserCommand.Execute(null);
            _projectTaskSource.SetResult(new[] { s_reloadedProject });

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject),
                    nameof(PickProjectIdViewModel.ProjectId),
                    nameof(PickProjectIdViewModel.LoadTask),
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject),
                    nameof(PickProjectIdViewModel.ProjectId)
                },
                _properiesChanged);
            CollectionAssert.AreEqual(new[] { s_reloadedProject }, _testObject.Projects.ToList());
            Assert.AreEqual(s_reloadedProject, _testObject.SelectedProject);
            Assert.AreEqual(ReloadedProjectId, _testObject.ProjectId);
        }


        [TestMethod]
        public void TestReloadProjectsWithAlwaysMissingDefault()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _testObject = BuildTestObject();
            _projectTaskSource.SetResult(new[] { s_testProject });
            _properiesChanged.Clear();

            _testObject.ChangeUserCommand.Execute(null);
            _projectTaskSource.SetResult(new[] { s_reloadedProject });

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.LoadTask),
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject)
                },
                _properiesChanged, string.Join(", ", _properiesChanged));
            CollectionAssert.AreEqual(new[] { s_reloadedProject }, _testObject.Projects.ToList());
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(DefaultProjectId, _testObject.ProjectId);
        }


        [TestMethod]
        public void TestReloadProjectsWithNewlyMissingDefault()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _testObject = BuildTestObject();
            _projectTaskSource.SetResult(new[] { s_testProject, s_defaultProject });
            _properiesChanged.Clear();

            _testObject.ChangeUserCommand.Execute(null);
            _projectTaskSource.SetResult(new[] { s_reloadedProject });

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.LoadTask),
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject),
                    nameof(PickProjectIdViewModel.ProjectId)
                },
                _properiesChanged, string.Join(", ", _properiesChanged));
            CollectionAssert.AreEqual(new[] { s_reloadedProject }, _testObject.Projects.ToList());
            Assert.AreEqual(s_reloadedProject, _testObject.SelectedProject);
            Assert.AreEqual(ReloadedProjectId, _testObject.ProjectId);
        }

        [TestMethod]
        public void TestReloadProjectsWithInputProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _testObject = BuildTestObject();
            _projectTaskSource.SetResult(new[] { s_testProject });
            _properiesChanged.Clear();

            _testObject.ChangeUserCommand.Execute(null);
            _testObject.ProjectId = TestInputProjectId;
            _projectTaskSource.SetResult(new[] { s_reloadedProject });

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.LoadTask),
                    nameof(PickProjectIdViewModel.ProjectId),
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject)
                },
                _properiesChanged, string.Join(", ", _properiesChanged));
            CollectionAssert.AreEqual(new[] { s_reloadedProject }, _testObject.Projects.ToList());
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(TestInputProjectId, _testObject.ProjectId);
        }

        [TestMethod]
        public void TestReloadProjectsResultWithEarlyInput()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _testObject = BuildTestObject();

            _testObject.ProjectId = TestInputProjectId;
            _projectTaskSource.SetResult(new[] { s_testProject });
            _testObject.ChangeUserCommand.Execute(null);
            _projectTaskSource.SetResult(new[] { s_reloadedProject });

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.ProjectId),
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject),
                    nameof(PickProjectIdViewModel.LoadTask),
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject)
                },
                _properiesChanged, string.Join(", ", _properiesChanged));
            CollectionAssert.AreEqual(new[] { s_reloadedProject }, _testObject.Projects.ToList());
            Assert.IsNull(_testObject.SelectedProject);
            Assert.AreEqual(TestInputProjectId, _testObject.ProjectId);
            Assert.IsNull(_testObject.Result);
        }
    }
}
