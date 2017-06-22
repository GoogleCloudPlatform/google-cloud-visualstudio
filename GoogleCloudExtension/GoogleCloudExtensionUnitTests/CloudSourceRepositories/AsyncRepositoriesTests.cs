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
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests
{
    [TestClass]
    public class AsyncRepositoriesTests
    {
        private AsyncRepositories _testObject;
        private IList<Repo> _testRepos;
        private IList<Repo> _emptyList = new List<Repo>();

        [TestInitialize]
        public void Initialize()
        {
            _testRepos = new List<Repo>
            {
                new Repo()
            };
            _testObject = new AsyncRepositories();
        }

        [TestMethod]
        public void TestInitialState()
        {
            Assert.AreEqual(AsyncRepositories.DisplayOptions.Pending, _testObject.DisplayState);
            Assert.IsNull(_testObject.Value);
        }

        [TestMethod]
        [ExpectedException(typeof(TestException))]
        public void TestCatchException()
        {
            AsyncRepositories.GetCloudReposAsync = async (projectId) =>
            {
                await Task.Delay(1);
                throw new TestException();
            };

            var task = AsyncWait(() => _testObject.StartListRepoTaskAsync("projectid"));
            task.Wait();
            Assert.IsNotNull(task.Result);
            throw task.Result;          
        }

        [TestMethod]
        public void TestPendingAndCompletion()
        {
            ManualResetEventSlim notify = new ManualResetEventSlim();
            var tcs = new TaskCompletionSource<IList<Repo>>();
            AsyncRepositories.GetCloudReposAsync = (projectId) =>
            {
                Task.Run(() =>
                {
                    notify.Wait();
                    tcs.SetResult(_testRepos);
                });
                return tcs.Task;
            };

            Task task;
            try
            {
                task = _testObject.StartListRepoTaskAsync("projectid");
                Assert.AreEqual(AsyncRepositories.DisplayOptions.Pending, _testObject.DisplayState);
                Assert.IsNull(_testObject.Value);
            }
            finally
            {
                notify.Set();
            }

            task.Wait();
            Assert.AreEqual(AsyncRepositories.DisplayOptions.HasItems, _testObject.DisplayState);
            Assert.AreSame(_testRepos, _testObject.Value);
        }

        [TestMethod]
        public void TestEmptyResults()
        {
            AsyncRepositories.GetCloudReposAsync = (projectId) =>
            {                
                return Task.FromResult(_emptyList);
            };
            var task = _testObject.StartListRepoTaskAsync("projectid");
            task.Wait();
            Assert.AreEqual(AsyncRepositories.DisplayOptions.NoItems, _testObject.DisplayState);
            Assert.AreSame(_emptyList, _testObject.Value);

        }
        private async Task<Exception> AsyncWait(Func<Task> task)
        {
            try
            {
                await task();
            }
            catch(Exception ex)
            {
                return ex;
            }
            return null;
        }

        private class TestException : Exception { }
    }
}
