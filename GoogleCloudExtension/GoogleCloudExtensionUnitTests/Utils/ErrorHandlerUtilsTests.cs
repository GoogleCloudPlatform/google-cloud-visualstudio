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

using GoogleCloudExtension.UserPrompt;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class ErrorHandlerUtilsTests : ExtensionTestBase
    {
        [TestMethod]
        public void TestHandleExceptions_DoesNotPromptForSuccess()
        {
            ErrorHandlerUtils.HandleExceptions(() => { });

            PromptUserMock.Verify(f => f(It.IsAny<UserPromptWindow.Options>()), Times.Never);
        }

        [TestMethod]
        public void TestHandleExceptions_PromptsForNormalException()
        {
            ErrorHandlerUtils.HandleExceptions(() => throw new Exception());

            PromptUserMock.Verify(f => f(It.IsAny<UserPromptWindow.Options>()), Times.Once);
        }

        [TestMethod]
        public void TestHandleExceptions_ThrowsCriticalException()
        {
            Assert.ThrowsException<AccessViolationException>(
                () => ErrorHandlerUtils.HandleExceptions(() => throw new AccessViolationException()));

            PromptUserMock.Verify(f => f(It.IsAny<UserPromptWindow.Options>()), Times.Never);
        }

        [TestMethod]
        public async Task TestHandleAsyncExceptions_DoesNotPromptForSuccessAsync()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);

            await ErrorHandlerUtils.HandleExceptionsAsync(() => tcs.Task);

            PromptUserMock.Verify(f => f(It.IsAny<UserPromptWindow.Options>()), Times.Never);
        }

        [TestMethod]
        public async Task TestHandleAsyncExceptions_PromptsForNormalExceptionAsync()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetException(new Exception());

            await ErrorHandlerUtils.HandleExceptionsAsync(() => tcs.Task);

            PromptUserMock.Verify(f => f(It.IsAny<UserPromptWindow.Options>()), Times.Once);
        }

        [TestMethod]
        public async Task TestHandleAsyncExceptions_ThrowsCriticalExceptionAsync()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetException(new AccessViolationException());

            await Assert.ThrowsExceptionAsync<AccessViolationException>(
                async () => await ErrorHandlerUtils.HandleExceptionsAsync(() => tcs.Task));

            PromptUserMock.Verify(f => f(It.IsAny<UserPromptWindow.Options>()), Times.Never);
        }

        [TestMethod]
        public void TestIsCriticalException_FalseForNormalException()
        {
            bool result = ErrorHandlerUtils.IsCriticalException(new Exception());

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsCriticalException_TrueForCriticalException()
        {
            bool result = ErrorHandlerUtils.IsCriticalException(new AccessViolationException());

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestIsCriticalException_FalseForAggregateExceptionContainingOnlyNormalException()
        {
            bool result =
                ErrorHandlerUtils.IsCriticalException(new AggregateException(new Exception(), new Exception()));

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestIsCriticalException_FalseForAggregateExceptionContainingACriticalException()
        {
            bool result = ErrorHandlerUtils.IsCriticalException(
                    new AggregateException(new AccessViolationException(), new Exception()));

            Assert.IsTrue(result);
        }
    }
}
