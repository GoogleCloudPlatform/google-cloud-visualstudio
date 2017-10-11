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

using GoogleCloudExtension;
using GoogleCloudExtension.PublishDialogSteps.FlexStep;
using GoogleCloudExtension.UserPrompt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.FlexStep
{
    [TestClass]
    public class FlexStepViewModelTests
    {
        private FlexStepViewModel _objectUnderTest;
        private Mock<Func<UserPromptWindow.Options, bool>> _promptUserFunctionMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _promptUserFunctionMock = new Mock<Func<UserPromptWindow.Options, bool>>();
            UserPromptWindow.PromptUserFunction = _promptUserFunctionMock.Object;
            _objectUnderTest = FlexStepViewModel.CreateStep();
        }

        [TestMethod]
        public void TestValidateInputNullVersion()
        {
            _promptUserFunctionMock.Setup(f => f(It.IsAny<UserPromptWindow.Options>())).Returns(true);
            _objectUnderTest.Version = null;

            bool result = _objectUnderTest.ValidateInput();

            Assert.IsFalse(result);
            _promptUserFunctionMock.Verify(
                f => f(
                    It.Is<UserPromptWindow.Options>(
                        o => o.Title == Resources.UiInvalidValueTitle &&
                            o.Prompt == Resources.FlexPublishEmptyVersionMessage)),
                Times.Once);
        }

        [TestMethod]
        public void TestValidateInputEmptyVersion()
        {
            _promptUserFunctionMock.Setup(f => f(It.IsAny<UserPromptWindow.Options>())).Returns(true);
            _objectUnderTest.Version = "";

            bool result = _objectUnderTest.ValidateInput();

            Assert.IsFalse(result);
            Func<UserPromptWindow.Options, bool> optionsPredicate =
                o => o.Title == Resources.UiInvalidValueTitle &&
                o.Prompt == Resources.FlexPublishEmptyVersionMessage;
            _promptUserFunctionMock.Verify(
                f => f(
                    It.Is<UserPromptWindow.Options>(o => optionsPredicate(o))),
                Times.Once);
        }

        [TestMethod]
        public void TestValidateInputInvalidVersion()
        {
            _promptUserFunctionMock.Setup(f => f(It.IsAny<UserPromptWindow.Options>())).Returns(true);
            _objectUnderTest.Version = "-Invalid Version Name!";

            bool result = _objectUnderTest.ValidateInput();

            Assert.IsFalse(result);
            _promptUserFunctionMock.Verify(
                f => f(
                    It.Is<UserPromptWindow.Options>(
                        o => o.Title == Resources.UiInvalidValueTitle &&
                            o.Prompt == string.Format(Resources.FlexPublishInvalidVersionMessage, _objectUnderTest.Version))),
                Times.Once);
        }

        [TestMethod]
        public void TestValidateInputValidVersion()
        {
            _promptUserFunctionMock.Setup(f => f(It.IsAny<UserPromptWindow.Options>())).Returns(true);
            _objectUnderTest.Version = "valid-version-name";

            bool result = _objectUnderTest.ValidateInput();

            Assert.IsTrue(result);
            _promptUserFunctionMock.Verify(f => f(It.IsAny<UserPromptWindow.Options>()), Times.Never);
        }
    }
}
