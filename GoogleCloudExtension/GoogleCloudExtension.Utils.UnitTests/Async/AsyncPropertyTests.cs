// Copyright 2018 Google Inc. All Rights Reserved.
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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GoogleCloudExtension.Utils.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtension.Utils.UnitTests.Async
{
    public static class AsyncPropertyTests
    {
        [TestClass]
        public class AsyncProperty0Tests
        {
            [TestMethod]
            public void Test1ArgConstructor_PassesTaskParameterToBase()
            {
                var task = new Task(() => { });
                var objectUnderTest = new AsyncProperty(task);

                Assert.AreEqual(task, objectUnderTest.ActualTask);
            }

            [TestMethod]
            public void Test0ArgConstructor_PassesCompletedTaskToBase()
            {
                var objectUnderTest = new AsyncProperty();

                Assert.IsTrue(objectUnderTest.ActualTask.IsCompleted);
            }

            [TestMethod]
            public void TestCreate_CreatesNewTask()
            {
                Func<object, string> f = o => "new result";
                Task<object> inputTask = Task.FromResult(new object());

                AsyncProperty<string> result = AsyncProperty.Create(inputTask, f);

                Assert.AreNotEqual(inputTask, result.ActualTask);
            }

            [TestMethod]
            public async Task TestCreate_SetsResultantValue()
            {
                const string expectedResult = "Expected Result";

                AsyncProperty<string> result = AsyncProperty.Create(
                    Task.FromResult(new object()),
                    o => expectedResult);
                await result.SafeTask;

                Assert.AreEqual(expectedResult, result.Value);
            }
        }

        [TestClass]
        public class AsyncProperty1Tests
        {
            private TaskCompletionSource<object> _tcs;

            [TestInitialize]
            public void BeforeEach()
            {
                _tcs = new TaskCompletionSource<object>();
            }

            [TestMethod]
            public void Test2ArgConstructor_PassesTaskParameterToBase()
            {
                var task = new Task<object>(() => null);
                var objectUnderTest = new AsyncProperty<object>(task);

                Assert.AreEqual(task, objectUnderTest.ActualTask);
            }

            [TestMethod]
            public void Test2ArgConstructor_SetsValueToGivenDefault()
            {
                var givenDefault = new object();
                var objectUnderTest = new AsyncProperty<object>(_tcs.Task, givenDefault);

                Assert.AreEqual(givenDefault, objectUnderTest.Value);
            }

            [TestMethod]
            public void Test1ArgConstructor_SetsValue()
            {
                var givenDefault = new object();
                var objectUnderTest = new AsyncProperty<object>(givenDefault);

                Assert.AreEqual(givenDefault, objectUnderTest.Value);
            }

            [TestMethod]
            public void Test2ArgConstructor_SetsValueToAlreadyCompletedResult()
            {
                const string defaultObject = "default";
                const string resultObject = "result";
                var objectUnderTest = new AsyncProperty<string>(Task.FromResult(resultObject), defaultObject);

                Assert.AreEqual(resultObject, objectUnderTest.Value);
            }

            [TestMethod]
            public async Task TestOnTaskComplete_SetsValueToResult()
            {
                const string defaultObject = "default";
                const string resultObject = "result";
                var objectUnderTest = new AsyncProperty<object>(_tcs.Task, defaultObject);

                _tcs.SetResult(resultObject);
                await objectUnderTest.SafeTask;

                Assert.AreEqual(resultObject, objectUnderTest.Value);
            }

            [TestMethod]
            public async Task TestOnTaskComplete_KeepsValueDefaultOnException()
            {
                var defaultObject = new object();
                var objectUnderTest = new AsyncProperty<object>(_tcs.Task, defaultObject);
                _tcs.SetException(new Exception());

                await objectUnderTest.SafeTask;

                Assert.AreEqual(defaultObject, objectUnderTest.Value);
            }

            [TestMethod]
            public async Task TestValue_RaisesPropertyChangedWhenSet()
            {
                const string defaultObject = "default";
                const string resultObject = "result";
                var objectUnderTest = new AsyncProperty<object>(_tcs.Task, defaultObject);
                var changedProperties = new List<string>();
                objectUnderTest.PropertyChanged += (sender, args) => changedProperties.Add(args.PropertyName);

                _tcs.SetResult(resultObject);
                await objectUnderTest.SafeTask;

                CollectionAssert.Contains(changedProperties, nameof(objectUnderTest.Value));
            }
        }
    }
}
