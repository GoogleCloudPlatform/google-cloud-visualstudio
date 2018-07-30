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
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.PickProjectDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.PickProjectDialog
{
    [TestClass]
    public class PickProjectIdViewModelTests : ExtensionTestBase
    {
        private const string DefaultProjectId = "default-project-id";
        private const string TestProjectId = "loaded-project-id";
        private const string MockUserName = "UserName";
        private const string TestExceptionMessage = "Test Exception";

        private static readonly Project s_defaultProject = new Project { ProjectId = DefaultProjectId };
        private static readonly Project s_testProject = new Project { ProjectId = TestProjectId };
        private static readonly UserAccount s_defaultAccount = new UserAccount { AccountName = MockUserName };

        private TaskCompletionSource<IList<Project>> _projectTaskSource;
        private PickProjectIdViewModel _testObject;
        private List<string> _properiesChanged;
        private PropertyChangedEventHandler _addPropertiesChanged;

        protected override void BeforeEach()
        {
            _testObject = null;
            _projectTaskSource = new TaskCompletionSource<IList<Project>>();
            PackageMock.Setup(p => p.DataSourceFactory.ResourceManagerDataSource.ProjectsListTask)
                .Returns(() => _projectTaskSource.Task);
            _properiesChanged = new List<string>();
            _addPropertiesChanged = (sender, args) => _properiesChanged.Add(args.PropertyName);
        }

        private PickProjectIdViewModel BuildTestObject()
        {
            var testObject = new PickProjectIdViewModel("Help Text", false);
            testObject.PropertyChanged += _addPropertiesChanged;
            return testObject;
        }

        [TestMethod]
        public void TestInitialConditionsWithoutDefaultUser()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentAccount).Returns(() => null);

            _testObject = BuildTestObject();

            Assert.IsNull(_testObject.LoadTask);
            Assert.IsNull(_testObject.Projects);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.IsFalse(_testObject.OkCommand.CanExecuteCommand);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void TestInitialConditionsWithDefaultUser()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentAccount).Returns(s_defaultAccount);

            _testObject = BuildTestObject();

            Assert.IsFalse(_testObject.LoadTask.IsCompleted, "Task should be running.");
            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsFalse(_testObject.LoadTask.IsSuccess);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.IsFalse(_testObject.OkCommand.CanExecuteCommand);
            Assert.IsNull(_testObject.Result);
        }

        [TestMethod]
        public void TestChangeUserCommandNoUser()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentAccount).Returns(() => null);
            _testObject = BuildTestObject();

            _testObject.ChangeUserCommand.Execute(null);

            PackageMock.Verify(p => p.UserPromptService.PromptUser(It.IsAny<ManageAccountsWindowContent>()));
            Assert.IsNull(_testObject.LoadTask);
        }

        [TestMethod]
        public void TestChangeUserCommand_CallsPromptManageAccount()
        {
            _testObject = BuildTestObject();

            _testObject.ChangeUserCommand.Execute(null);

            PackageMock.Verify(p => p.UserPromptService.PromptUser(It.IsAny<ManageAccountsWindowContent>()));
        }

        [TestMethod]
        public void TestChangeUserCommand_UpdatesHasAccount()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentAccount).Returns(() => null);
            _testObject = BuildTestObject();

            CredentialStoreMock.SetupGet(cs => cs.CurrentAccount).Returns(s_defaultAccount);
            _testObject.ChangeUserCommand.Execute(null);

            Assert.IsTrue(_testObject.HasAccount);
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
            CollectionAssert.That.IsEmpty(_testObject.Projects);
        }

        [TestMethod]
        public void TestOkCommand()
        {
            _testObject = BuildTestObject();
            _testObject.SelectedProject = s_defaultProject;
            var closeMock = new Mock<Action>();
            _testObject.Close += closeMock.Object;

            _testObject.OkCommand.Execute(null);

            closeMock.Verify(f => f());
            Assert.AreEqual(s_defaultProject, _testObject.Result);
        }

        [TestMethod]
        public void TestReloadProjects()
        {
            _testObject = BuildTestObject();
            _projectTaskSource.SetResult(new[] { s_testProject });
            _properiesChanged.Clear();

            _testObject.ChangeUserCommand.Execute(null);

            Assert.IsFalse(_testObject.LoadTask.IsError);
            Assert.IsFalse(_testObject.LoadTask.IsCanceled);
            Assert.IsTrue(_testObject.LoadTask.IsSuccess);
            Assert.IsNull(_testObject.SelectedProject);
            Assert.IsFalse(_testObject.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestLoadProjectsWithMissingSelectedProject()
        {
            _testObject = BuildTestObject();

            _testObject.SelectedProject = s_defaultProject;
            _projectTaskSource.SetResult(new[] { s_testProject });

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.SelectedProject),
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject)
                },
                _properiesChanged);
            CollectionAssert.AreEqual(new[] { s_testProject }, _testObject.Projects.ToList());
            Assert.IsNull(_testObject.SelectedProject);
        }

        [TestMethod]
        public void TestLoadProjectsWithIncludedSelectedProject()
        {
            _testObject = BuildTestObject();

            _testObject.SelectedProject = s_testProject;
            _projectTaskSource.SetResult(new[] { s_testProject });

            CollectionAssert.AreEqual(
                new[]
                {
                    nameof(PickProjectIdViewModel.SelectedProject),
                    nameof(PickProjectIdViewModel.Projects),
                    nameof(PickProjectIdViewModel.SelectedProject)
                },
                _properiesChanged);
            CollectionAssert.AreEqual(new[] { s_testProject }, _testObject.Projects.ToList());
            Assert.AreEqual(s_testProject, _testObject.SelectedProject);
        }
    }
}
