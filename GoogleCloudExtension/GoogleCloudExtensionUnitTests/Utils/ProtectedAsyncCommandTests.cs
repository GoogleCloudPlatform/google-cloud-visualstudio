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
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class ProtectedAsyncCommandTests : ExtensionTestBase
    {
        [TestMethod]
        public void TestConstrutor_DefaultsCanExecuteCommandToTrue()
        {
            var objectUnderTest = new ProtectedAsyncCommand(() => Task.CompletedTask);

            Assert.IsTrue(objectUnderTest.CanExecuteCommand);
        }

        [TestMethod]
        public void TestConstrutor_OverridesDefaultCanExecuteCommandWithParameter()
        {
            var objectUnderTest = new ProtectedAsyncCommand(() => Task.CompletedTask, false);

            Assert.IsFalse(objectUnderTest.CanExecuteCommand);
        }

        [TestMethod]
        public void TestConstrutor_InitalizesLatestExecutionWithCompletedTask()
        {
            var objectUnderTest = new ProtectedAsyncCommand(() => Task.CompletedTask);

            Assert.IsTrue(objectUnderTest.LatestExecution.IsCompleted);
        }

        [TestMethod]
        public void TestExecute_InvokesProvidedAction()
        {
            var tcs = new TaskCompletionSource<object>();
            var actionMock = new Mock<Func<Task>>();
            actionMock.Setup(f => f()).Returns(tcs.Task);
            var objectUnderTest = new ProtectedAsyncCommand(actionMock.Object);

            objectUnderTest.Execute(null);

            actionMock.Verify(f => f(), Times.Once);
        }

        [TestMethod]
        public void TestExecute_DoesNotThrowWhenActionErrors()
        {
            var objectUnderTest = new ProtectedAsyncCommand(() => Task.FromException(new Exception()));

            objectUnderTest.Execute(null);
        }

        [TestMethod]
        public void TestExecute_UpdatesLatestExecution()
        {
            var objectUnderTest = new ProtectedAsyncCommand(() => Task.CompletedTask);
            AsyncProperty originalExecution = objectUnderTest.LatestExecution;

            objectUnderTest.Execute(null);

            Assert.AreNotEqual(originalExecution, objectUnderTest.LatestExecution);
        }

        [TestMethod]
        public void TestLatestExecution_TracksActionTask()
        {
            var tcs = new TaskCompletionSource<object>();
            var objectUnderTest = new ProtectedAsyncCommand(() => tcs.Task);

            objectUnderTest.Execute(null);

            Assert.AreEqual(tcs.Task, objectUnderTest.LatestExecution.ActualTask);
        }

        [TestMethod]
        public void TestLatestExecution_NotifiesOnChange()
        {
            var objectUnderTest = new ProtectedAsyncCommand(() => Task.CompletedTask);
            var changedProperties = new List<string>();
            objectUnderTest.PropertyChanged += (sender, args) => changedProperties.Add(args.PropertyName);

            objectUnderTest.Execute(null);

            CollectionAssert.AreEqual(new[] { nameof(objectUnderTest.LatestExecution) }, changedProperties);
        }
    }
}
