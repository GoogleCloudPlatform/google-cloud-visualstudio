﻿// Copyright 2018 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Utils.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestingHelpers;

namespace GoogleCloudExtension.Utils.UnitTests.Async
{
    [TestClass]
    public class AsyncPropertyBaseTests
    {
        private TaskCompletionSource<object> _tcs;

        /// <summary>
        /// Minimal test implementation of AsyncPropertyBase, with call count indicators for virtual methods.
        /// </summary>
        private class TestAsyncPropertyBase : AsyncPropertyBase<Task>
        {
            public TestAsyncPropertyBase(Task task) : base(task) { }

            protected override void OnTaskComplete() => OnTaskCompleteCallCount++;

            public int OnTaskCompleteCallCount { get; private set; }

            public static T GetTaskResultSafe2<T>(Task<T> task, T defaultValue) =>
                GetTaskResultSafe(task, defaultValue);

            public static T GetTaskResultSafe2<T>(Task<T> task) =>
                GetTaskResultSafe(task);
        }

        [TestInitialize]
        public void BeforeEach()
        {
            _tcs = new TaskCompletionSource<object>();
        }

        [TestMethod]
        public void TestConstructor_SetsActualTask()
        {
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);

            Assert.AreEqual(_tcs.Task, objectUnderTest.ActualTask);
        }

        [TestMethod]
        public void TestConstructor_SetsSafeTask()
        {
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);

            Assert.IsNotNull(objectUnderTest.SafeTask);
            Assert.AreNotEqual(_tcs.Task, objectUnderTest.SafeTask);
        }

        [TestMethod]
        public void TestAllProperties_NullTaskDefaults()
        {
            var objectUnderTest = new TestAsyncPropertyBase(null);

            Assert.IsFalse(objectUnderTest.IsPending);
            Assert.IsFalse(objectUnderTest.IsCompleted);
            Assert.IsFalse(objectUnderTest.IsSuccess);
            Assert.IsFalse(objectUnderTest.IsCanceled);
            Assert.IsFalse(objectUnderTest.IsError);
            Assert.IsNull(objectUnderTest.ErrorMessage);
            Assert.IsNull(objectUnderTest.ActualTask);
        }

        [TestMethod]
        public void TestAllProperties_PendingTaskDefaults()
        {
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);

            Assert.IsTrue(objectUnderTest.IsPending);
            Assert.IsFalse(objectUnderTest.IsCompleted);
            Assert.IsFalse(objectUnderTest.IsSuccess);
            Assert.IsFalse(objectUnderTest.IsCanceled);
            Assert.IsFalse(objectUnderTest.IsError);
            Assert.IsNull(objectUnderTest.ErrorMessage);
        }

        [TestMethod]
        public async Task TestSafeTask_DoesNotThrowWithNullTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(null);

            await objectUnderTest.SafeTask;
        }

        [TestMethod]
        public async Task TestIsPending_CompletedTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(Task.CompletedTask);

            await objectUnderTest.SafeTask;

            Assert.IsFalse(objectUnderTest.IsPending);
        }

        [TestMethod]
        public async Task TestIsCompleted_CompletedTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(Task.CompletedTask);

            await objectUnderTest.SafeTask;

            Assert.IsTrue(objectUnderTest.IsCompleted);
        }

        [TestMethod]
        public async Task TestIsSuccess_CompletedTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(Task.CompletedTask);

            await objectUnderTest.SafeTask;

            Assert.IsTrue(objectUnderTest.IsSuccess);
        }

        [TestMethod]
        public async Task TestIsCanceled_CanceledTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(Task.FromCanceled(new CancellationToken(true)));

            await objectUnderTest.SafeTask;

            Assert.IsTrue(objectUnderTest.IsCanceled);
        }

        [TestMethod]
        public async Task TestIsError_FaultedTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(Task.FromException(new Exception()));

            await objectUnderTest.SafeTask;

            Assert.IsTrue(objectUnderTest.IsError);
        }

        [TestMethod]
        public async Task TestErrorMessage_SingleExceptionAsync()
        {
            const string testExceptionMessage = "Test Exception Message";
            var objectUnderTest = new TestAsyncPropertyBase(Task.FromException(new Exception(testExceptionMessage)));

            await objectUnderTest.SafeTask;

            Assert.AreEqual(testExceptionMessage, objectUnderTest.ErrorMessage);
        }

        [TestMethod]
        public async Task TestErrorMessage_IsFirstNonNullEmptyMessageOfAggregateInnerExceptionsAsync()
        {
            const string testExceptionMessage = "Test Multi Exception Message";
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);
            _tcs.SetException(new[] { new ExceptionWithNullMessage(), new Exception(testExceptionMessage) });

            await objectUnderTest.SafeTask;

            Assert.AreEqual(testExceptionMessage, objectUnderTest.ErrorMessage);
        }

        [TestMethod]
        public async Task TestErrorMessage_IsAggregateMessageWithOnlyNullEmptyMessagesAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(Task.FromException(new ExceptionWithNullMessage()));

            await objectUnderTest.SafeTask;

            Assert.AreEqual(new AggregateException(new ExceptionWithNullMessage()).Message, objectUnderTest.ErrorMessage);
        }

        [TestMethod]
        public async Task TestOnTaskComplete_NotCalledForNullaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(null);

            await objectUnderTest.SafeTask;

            Assert.AreEqual(0, objectUnderTest.OnTaskCompleteCallCount);
        }


        [TestMethod]
        public void TestOnTaskComplete_NotCalledForPendingTask()
        {
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);

            Assert.AreEqual(0, objectUnderTest.OnTaskCompleteCallCount);
        }

        [TestMethod]
        public async Task TestOnTaskComplete_CalledForSuccessTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);
            _tcs.SetResult(null);
            await objectUnderTest.SafeTask;

            Assert.AreEqual(1, objectUnderTest.OnTaskCompleteCallCount);
        }

        [TestMethod]
        public async Task TestOnTaskComplete_CalledForFaultedTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);
            _tcs.SetException(new Exception());
            await objectUnderTest.SafeTask;

            Assert.AreEqual(1, objectUnderTest.OnTaskCompleteCallCount);
        }

        [TestMethod]
        public async Task TestOnTaskComplete_CalledForCanceledTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);
            _tcs.SetCanceled();
            await objectUnderTest.SafeTask;

            Assert.AreEqual(1, objectUnderTest.OnTaskCompleteCallCount);
        }

        [TestMethod]
        public async Task TestPropertyChangedRaised_NullTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(null);
            var changedProperties = new List<string>();
            objectUnderTest.PropertyChanged += (sender, args) => changedProperties.Add(args.PropertyName);

            await objectUnderTest.SafeTask;

            CollectionAssert.AreEquivalent(new string[] { }, changedProperties);
        }

        [TestMethod]
        public void TestPropertyChangedRaised_PendingTask()
        {
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);
            var changedProperties = new List<string>();
            objectUnderTest.PropertyChanged += (sender, args) => changedProperties.Add(args.PropertyName);

            CollectionAssert.AreEquivalent(new string[] { }, changedProperties);
        }

        [TestMethod]
        public async Task TestPropertyChangedRaised_SuccessTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);
            var changedProperties = new List<string>();
            objectUnderTest.PropertyChanged += (sender, args) => changedProperties.Add(args.PropertyName);

            _tcs.SetResult(null);
            await objectUnderTest.SafeTask;

            CollectionAssert.AreEquivalent(
                new[]
                {
                    nameof(objectUnderTest.IsPending),
                    nameof(objectUnderTest.IsCompleted),
                    nameof(objectUnderTest.IsSuccess)
                }, changedProperties);
        }

        [TestMethod]
        public async Task TestPropertyChangedRaised_FaultedTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);
            var changedProperties = new List<string>();
            objectUnderTest.PropertyChanged += (sender, args) => changedProperties.Add(args.PropertyName);

            _tcs.SetException(new Exception());
            await objectUnderTest.SafeTask;

            CollectionAssert.AreEquivalent(
                new[]
                {
                    nameof(objectUnderTest.IsPending),
                    nameof(objectUnderTest.IsCompleted),
                    nameof(objectUnderTest.IsError),
                    nameof(objectUnderTest.ErrorMessage)
                }, changedProperties);
        }

        [TestMethod]
        public async Task TestPropertyChangedRaised_CanceledTaskAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);
            var changedProperties = new List<string>();
            objectUnderTest.PropertyChanged += (sender, args) => changedProperties.Add(args.PropertyName);

            _tcs.SetCanceled();
            await objectUnderTest.SafeTask;

            CollectionAssert.AreEquivalent(
                new[]
                {
                    nameof(objectUnderTest.IsPending),
                    nameof(objectUnderTest.IsCompleted),
                    nameof(objectUnderTest.IsCanceled)
                }, changedProperties);
        }

        [TestMethod]
        public async Task TestSafeTask_DoesNotThrowOnPropertyChangedHandlerErrorAsync()
        {
            var objectUnderTest = new TestAsyncPropertyBase(_tcs.Task);
            objectUnderTest.PropertyChanged += (sender, args) => throw new Exception();
            _tcs.SetResult(null);

            await objectUnderTest.SafeTask;
        }

        [TestMethod]
        public void TestGetTaskResultSafe_Result()
        {
            const string expectedResult = "Expected Result";

            string actualResult = TestAsyncPropertyBase.GetTaskResultSafe2(Task.FromResult(expectedResult));

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestGetTaskResultSafe_Error()
        {
            string actualResult = TestAsyncPropertyBase.GetTaskResultSafe2(Task.FromException<string>(new Exception()));

            Assert.AreEqual(default(string), actualResult);
        }

        [TestMethod]
        public void TestGetTaskResultSafe_ErrorDefaultValue()
        {
            const string expectedDefaultValue = "Expected Default Value";

            string actualResult = TestAsyncPropertyBase.GetTaskResultSafe2(Task.FromException<string>(new Exception()), expectedDefaultValue);

            Assert.AreEqual(expectedDefaultValue, actualResult);
        }
    }
}
