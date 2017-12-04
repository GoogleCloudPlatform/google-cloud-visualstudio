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

using Google.Apis.CloudSourceRepositories.v1.Data;
using GoogleCloudExtension.CloudSourceRepositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.CloudSourceRepository
{
    [TestClass]
    public class AsyncRepositoriesTests
    {
        private AsyncRepositories _testObject;
        private IList<Repo> _testRepos;
        private IList<Repo> _emptyList = new List<Repo>();
        private TaskCompletionSource<IList<Repo>> _testTaskCompletionSource;
        private Mock<Func<string, Task<IList<Repo>>>> _getCloudReposMock;

        [TestInitialize]
        public void Initialize()
        {
            _testRepos = new List<Repo> { new Repo() };
            _testTaskCompletionSource = new TaskCompletionSource<IList<Repo>>();
            _testObject = new AsyncRepositories();
            _getCloudReposMock = new Mock<Func<string, Task<IList<Repo>>>>();
            _getCloudReposMock.Setup(f => f(It.IsAny<string>())).Returns(() =>
            {
                var task = _testTaskCompletionSource.Task;
                // Reset it to make it easier to call and set multiple times.
                _testTaskCompletionSource = new TaskCompletionSource<IList<Repo>>();
                return task;
            });
            AsyncRepositories.GetCloudReposAsync = _getCloudReposMock.Object;
        }

        [TestMethod]
        public void TestInitialState()
        {
            Assert.AreEqual(AsyncRepositories.DisplayOptions.Pending, _testObject.DisplayState);
            Assert.IsNull(_testObject.Value);
        }

        [TestMethod]
        [ExpectedException(typeof(TestException))]
        public async Task TestCatchException()
        {
            _testTaskCompletionSource.SetException(new TestException());
            await _testObject.StartListRepoTaskAsync("projectid");
        }

        [TestMethod]
        public void TestPendingAndCompletion()
        {            
            Assert.AreEqual(AsyncRepositories.DisplayOptions.Pending, _testObject.DisplayState);
            _testTaskCompletionSource.SetResult(_testRepos);
            _testObject.StartListRepoTaskAsync("projectid").Wait();
            Assert.AreEqual(AsyncRepositories.DisplayOptions.HasItems, _testObject.DisplayState);
            Assert.AreSame(_testRepos, _testObject.Value);
        }

        [TestMethod]
        public void TestEmptyResults()
        {
            _testTaskCompletionSource.SetResult(_emptyList);
            _testObject.StartListRepoTaskAsync("projectid").Wait();
            Assert.AreEqual(AsyncRepositories.DisplayOptions.NoItems, _testObject.DisplayState);
            Assert.AreSame(_emptyList, _testObject.Value);

        }

        [TestMethod]
        public void TestClearList()
        {
            _testTaskCompletionSource.SetResult(_testRepos);
            _testObject.StartListRepoTaskAsync("projectid").Wait();
            _testObject.ClearList();
            Assert.IsNull(_testObject.Value);
            Assert.AreEqual(AsyncRepositories.DisplayOptions.Pending, _testObject.DisplayState);
        }

        private class TestException : Exception { }
    }
}
