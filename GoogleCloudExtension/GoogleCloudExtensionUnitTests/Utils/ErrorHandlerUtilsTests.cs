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

using GoogleCloudExtension.Services;
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
        private Mock<IUserPromptService> _promptUserMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _promptUserMock = new Mock<IUserPromptService>();
            PackageMock.Setup(p => p.UserPromptService).Returns(_promptUserMock.Object);
        }

        [TestMethod]
        public void TestHandleExceptions_DoesNotPromptForSuccess()
        {
            ErrorHandlerUtils.HandleExceptions(() => { });

            _promptUserMock.Verify(p => p.ExceptionPrompt(It.IsAny<Exception>()), Times.Never);
        }

        [TestMethod]
        public void TestHandleExceptions_PromptsForNormalException()
        {
            var thrownException = new Exception();

            ErrorHandlerUtils.HandleExceptions(() => throw thrownException);

            _promptUserMock.Verify(p => p.ExceptionPrompt(thrownException), Times.Once);
        }

        [TestMethod]
        public void TestHandleExceptions_ThrowsCriticalException()
        {
            Assert.ThrowsException<AccessViolationException>(
                () => ErrorHandlerUtils.HandleExceptions(() => throw new AccessViolationException()));

            _promptUserMock.Verify(p => p.ExceptionPrompt(It.IsAny<Exception>()), Times.Never);
        }

        [TestMethod]
        public void TestHandleExceptionsAsync_DoesNotPromptForSuccess()
        {
            ErrorHandlerUtils.HandleExceptionsAsync(() => Task.CompletedTask);

            _promptUserMock.Verify(p => p.ExceptionPrompt(It.IsAny<Exception>()), Times.Never);
        }

        [TestMethod]
        public void TestHandleExceptionsAsync_PromptsForNormalException()
        {
            var thrownException = new Exception();
            ErrorHandlerUtils.HandleExceptionsAsync(() => Task.FromException(thrownException));

            _promptUserMock.Verify(p => p.ExceptionPrompt(thrownException), Times.Once);
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
