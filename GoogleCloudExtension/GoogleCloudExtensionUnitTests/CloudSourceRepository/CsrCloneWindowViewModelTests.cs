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
using Google.Apis.CloudSourceRepositories.v1.Data;
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.CloudSourceRepositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.CloudSourceRepository
{
    [TestClass]
    public class CsrCloneWindowViewModelTests
    {
        private const int BackGroundAsyncTaskWaitSeconds = 5;
        private CsrCloneWindowViewModel _cloneWindowViewModel;
        private Project _mockedTestProject;
        private List<Project> _testProjects;
        private TaskCompletionSource<IList<Repo>> _testTaskCompletionSource;
        private Mock<Func<string, Task<IList<Repo>>>> _getCloudReposMock;
        private Mock<IApiManager> _apiManagerMock;

        [TestInitialize]
        public void Initialize()
        {
            _apiManagerMock = new Mock<IApiManager>();
            _mockedTestProject = Mock.Of<Project>();
            _testProjects = new List<Project> { Mock.Of<Project>(), _mockedTestProject };
            _getCloudReposMock = new Mock<Func<string, Task<IList<Repo>>>>();
            _testTaskCompletionSource = new TaskCompletionSource<IList<Repo>>();
            _getCloudReposMock.Setup(f => f(It.IsAny<string>())).Returns(() =>
            {
                var task = _testTaskCompletionSource.Task;
                // Reset it to make it easier to call and set multiple times.
                _testTaskCompletionSource = new TaskCompletionSource<IList<Repo>>();
                return task;
            });
            AsyncRepositories.GetCloudReposAsync = _getCloudReposMock.Object;
            CsrCloneWindowViewModel.s_getApiManagerFunc = (projectId) => _apiManagerMock.Object;

            Action testAction = () => { };
            _cloneWindowViewModel = new CsrCloneWindowViewModel(testAction, _testProjects);
        }

        [TestMethod]
        public void ChangeSelectedProjectTest()
        {
            _mockedTestProject.ProjectId = "test_project_id";
            Repo repoMock = Mock.Of<Repo>();
            _testTaskCompletionSource.SetResult(new List<Repo> { repoMock, Mock.Of<Repo>() });
            _apiManagerMock.Setup(
                x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>()))
                .Returns(Task.FromResult<bool>(true))
                .Verifiable();
            _cloneWindowViewModel.SelectedProject = _mockedTestProject;
            WaitForBackgroundAsyncTask(_cloneWindowViewModel);
            Assert.AreEqual(repoMock, _cloneWindowViewModel.SelectedRepository);
            Assert.IsFalse(_cloneWindowViewModel.NeedsApiEnabled);
            _apiManagerMock.Verify();
        }

        [TestMethod]
        public void CsrApiNotEnabledTest()
        {
            _mockedTestProject.ProjectId = "test_project_id";
            _apiManagerMock.Setup(
                x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>()))
                .Returns(Task.FromResult<bool>(false))
                .Verifiable();
            _cloneWindowViewModel.SelectedProject = _mockedTestProject;
            WaitForBackgroundAsyncTask(_cloneWindowViewModel);
            Assert.AreEqual(null, _cloneWindowViewModel.SelectedRepository);
            Assert.IsTrue(_cloneWindowViewModel.NeedsApiEnabled);
            _apiManagerMock.Verify();

            // Now enable it
            _apiManagerMock.Setup(
                x => x.EnableServicesAsync(It.IsAny<IList<string>>()))
                .Returns(Task.FromResult(0))
                .Verifiable();
            Repo repoMock = Mock.Of<Repo>();
            _testTaskCompletionSource.SetResult(new List<Repo> { repoMock, Mock.Of<Repo>() });
            Assert.IsTrue(_cloneWindowViewModel.EnableApiCommand.CanExecuteCommand);
            _cloneWindowViewModel.EnableApiCommand.Execute(null);
            WaitForBackgroundAsyncTask(_cloneWindowViewModel);
            _apiManagerMock.Verify();
            Assert.AreEqual(repoMock, _cloneWindowViewModel.SelectedRepository);
            Assert.IsFalse(_cloneWindowViewModel.NeedsApiEnabled);
        }

        /// <summary>
        /// This method wait for a few seconds till <param name="cloneWindowViewModel"/> IsReady is true;
        /// </summary>
        private void WaitForBackgroundAsyncTask(CsrCloneWindowViewModel cloneWindowViewModel)
        {
            int waitedSeconds = 0;
            Thread.Sleep(10); // Wait 10 milliseconds for async task kick off
            Thread.Yield();
            while (!cloneWindowViewModel.IsReady && waitedSeconds < BackGroundAsyncTaskWaitSeconds)
            {
                Thread.Sleep(1000);
                waitedSeconds++;
            }
            Assert.IsTrue(cloneWindowViewModel.IsReady);
        }
    }
}
