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
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.CloudSourceRepository
{
    [TestClass]
    public class CsrCloneWindowViewModelTests
    {
        private CsrCloneWindowViewModel _cloneWindowViewModel;
        private Mock<Project> _testProjectMock;
        private List<Project> _testProjects;
        private TaskCompletionSource<IList<Repo>> _testTaskCompletionSource;
        private Mock<Func<string, Task<IList<Repo>>>> _getCloudReposMock;
        private Mock<IApiManager> _apiManagerMock;
        private Mock<Action> _closeWindowActionMock;

        [TestInitialize]
        public void Initialize()
        {
            _apiManagerMock = new Mock<IApiManager>();
            _testProjectMock = new Mock<Project>();
            _testProjectMock.SetupGet(x => x.ProjectId).Returns("test_project_id");
            _testProjects = new List<Project> { new Mock<Project>().Object, _testProjectMock.Object };
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

            _closeWindowActionMock = new Mock<Action>();
            _cloneWindowViewModel = new CsrCloneWindowViewModel(_closeWindowActionMock.Object, _testProjects);
        }

        [TestMethod]
        public void CheckDefaultCloneFolder()
        {
            Assert.AreEqual(CsrCloneWindowViewModel.s_defaultLocalPath, _cloneWindowViewModel.LocalPath);
        }

        [TestMethod]
        public async Task ChangeSelectedProjectTest()
        {
            Mock<Repo> repoMock = new Mock<Repo>();
            _testTaskCompletionSource.SetResult(new List<Repo> { repoMock.Object, new Mock<Repo>().Object });
            _apiManagerMock.Setup(
                x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>()))
                .Returns(Task.FromResult<bool>(true))
                .Verifiable();
            _cloneWindowViewModel.SelectedProject = _testProjectMock.Object;
            await WaitForBackgroundAsyncTask(_cloneWindowViewModel);
            Assert.AreEqual(repoMock.Object, _cloneWindowViewModel.SelectedRepository);
            Assert.IsFalse(_cloneWindowViewModel.NeedsApiEnabled);
            _apiManagerMock.Verify();
        }

        [TestMethod]
        public async Task CsrApiNotEnabledTest()
        {
            _apiManagerMock.Setup(
                x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>()))
                .Returns(Task.FromResult<bool>(false))
                .Verifiable();
            _cloneWindowViewModel.SelectedProject = _testProjectMock.Object;
            await WaitForBackgroundAsyncTask(_cloneWindowViewModel);
            Assert.AreEqual(null, _cloneWindowViewModel.SelectedRepository);
            Assert.IsTrue(_cloneWindowViewModel.NeedsApiEnabled);
            _apiManagerMock.Verify();
        }

        [TestMethod]
        public async Task CsrApiEnabledTest()
        {
            _apiManagerMock.Setup(
                x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>()))
                .Returns(Task.FromResult<bool>(false));
            _cloneWindowViewModel.SelectedProject = _testProjectMock.Object;
            await WaitForBackgroundAsyncTask(_cloneWindowViewModel);

            _apiManagerMock.Setup(
                x => x.EnableServicesAsync(It.IsAny<IList<string>>()))
                .Returns(Task.FromResult(0))
                .Verifiable();
            Mock<Repo> repoMock = new Mock<Repo>();
            _testTaskCompletionSource.SetResult(new List<Repo> { repoMock.Object, new Mock<Repo>().Object });
            Assert.IsTrue(_cloneWindowViewModel.EnableApiCommand.CanExecuteCommand);
            _cloneWindowViewModel.EnableApiCommand.Execute(null);
            await WaitForBackgroundAsyncTask(_cloneWindowViewModel);
            _apiManagerMock.Verify();
            Assert.AreEqual(repoMock.Object, _cloneWindowViewModel.SelectedRepository);
            Assert.IsFalse(_cloneWindowViewModel.NeedsApiEnabled);
        }

        /// <summary>
        /// This method wait for a few seconds till <param name="cloneWindowViewModel"/> IsReady is true;
        /// </summary>
        private async Task WaitForBackgroundAsyncTask(CsrCloneWindowViewModel cloneWindowViewModel)
        {
            Stopwatch watch = Stopwatch.StartNew();
            while (!cloneWindowViewModel.IsReady && watch.Elapsed < TimeSpan.FromSeconds(1))
            {
                await Task.Delay(10);
            }
            Assert.IsTrue(cloneWindowViewModel.IsReady);
        }
    }
}
