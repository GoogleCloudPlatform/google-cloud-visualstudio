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

using GoogleCloudExtension;
using GoogleCloudExtension.UserPrompt;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class UserPromptUtilsStaticTests : ExtensionTestBase
    {
        [TestMethod]
        public void TestDefault_DefersToPackage()
        {
            Assert.AreEqual(UserPromptUtils.Default, GoogleCloudExtensionPackage.Instance.UserPromptService);
        }
    }

    [TestClass]
    public class UserPromptUtilsTests : WpfTestBase
    {
        private const string DefaultPrompt = "Default Prompt";
        private const string DefaultTitle = "Default Title";
        private const string ExpectedPrompt = "Expected Prompt";
        private const string ExpectedTitle = "Expected Title";
        private const string ExpectedCancelCaption = "Expected Cancel Caption";
        private const string ExpectedActionCaption = "Expected Action Caption";
        private const string ExpectedMessage = "Expected Message";
        private UserPromptUtils _objectUnderTest;

        private readonly Action<UserPromptWindow> _defaultCloseAction = userPrompt => userPrompt.Close();
        private static readonly string s_expectedErrorPrompt = string.Format(Resources.ExceptionPromptMessage, ExpectedPrompt);

        [TestInitialize]
        public void BeforeEach() => _objectUnderTest = new UserPromptUtils();

        [TestMethod]
        public void TestActionPrompt_PromptsWithTitle()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ActionPrompt(DefaultPrompt, ExpectedTitle));
            string titleResult = userPrompt.Title;
            Assert.AreEqual(ExpectedTitle, titleResult);
        }

        [TestMethod]
        public void TestActionPrompt_PromptsWithGivenPrompt()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ActionPrompt(ExpectedPrompt, DefaultTitle));
            Assert.AreEqual(ExpectedPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public void TestActionPrompt_PromptsWithNullMessageByDefault()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
            Assert.IsNull(userPrompt.ViewModel.Message);
        }

        [TestMethod]
        public void TestActionPrompt_PromptsWithGivenMessage()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(
                () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle, ExpectedMessage));
            Assert.AreEqual(ExpectedMessage, userPrompt.ViewModel.Message);
        }

        [TestMethod]
        public void TestActionPrompt_PromptsWithDefaultActionCaption()
        {
            string defaultActionCaption = Resources.UiYesButtonCaption;
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
            Assert.AreEqual(defaultActionCaption, userPrompt.ViewModel.ActionButtonCaption);
        }

        [TestMethod]
        public void TestActionPrompt_PromptsWithGivenActionCaption()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(
                () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle, actionCaption: ExpectedActionCaption));
            Assert.AreEqual(ExpectedActionCaption, userPrompt.ViewModel.ActionButtonCaption);
        }

        [TestMethod]
        public void TestActionPrompt_PromptsWithDefaultCancelCaption()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
            Assert.AreEqual(Resources.UiCancelButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
        }

        [TestMethod]
        public void TestActionPrompt_PromptsWithGivenCancelCaption()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(
                () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle, cancelCaption: ExpectedCancelCaption));
            Assert.AreEqual(ExpectedCancelCaption, userPrompt.ViewModel.CancelButtonCaption);
        }

        [TestMethod]
        public void TestActionPrompt_PromptsWithNoIconByDefault()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
            Assert.IsNull(userPrompt.ViewModel.Icon);
        }

        [TestMethod]
        public void TestActionPrompt_PromptsWithWariningIcon()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(
                () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle, isWarning: true));

            var bitmapImage = (BitmapImage)userPrompt.ViewModel.Icon;
            StringAssert.EndsWith(bitmapImage.UriSource.AbsolutePath, UserPromptUtils.WarningIconPath);
        }

        [TestMethod]
        public void TestActionPrompt_ReturnsFalseWhenClosed()
        {
            bool result = GetUserPromptResult(
                userPrompt => userPrompt.Close(),
                () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle)
            );
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestActionPrompt_ReturnsTrueOnOkCommand()
        {
            bool result = GetUserPromptResult(
                userPrompt => userPrompt.ViewModel.ActionCommand.Execute(null),
                () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle)
            );
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestOkPrompt_PromptsWithTitle()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(() => _objectUnderTest.OkPrompt(DefaultPrompt, ExpectedTitle));
            Assert.AreEqual(ExpectedTitle, userPrompt.Title);
        }

        [TestMethod]
        public void TestOkPrompt_PromptsWithGivenPrompt()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(() => _objectUnderTest.OkPrompt(ExpectedPrompt, DefaultTitle));
            Assert.AreEqual(ExpectedPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public void TestOkPrompt_SetsCancelCaptionToOk()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(() => _objectUnderTest.OkPrompt(DefaultPrompt, DefaultTitle));
            Assert.AreEqual(Resources.UiOkButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
        }

        [TestMethod]
        public void TestOkPrompt_HasActionButtonFalse()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(() => _objectUnderTest.OkPrompt(DefaultPrompt, DefaultTitle));
            Assert.IsFalse(userPrompt.ViewModel.HasActionButton);
        }

        [TestMethod]
        public void TestErrorPrompt_PromptsWithGivenTitle()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, ExpectedTitle));
            Assert.AreEqual(ExpectedTitle, userPrompt.Title);
        }

        [TestMethod]
        public void TestErrorPrompt_PromptsWithGivenPrompt()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorPrompt(ExpectedPrompt, DefaultTitle));

            Assert.AreEqual(ExpectedPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public void TestErrorPrompt_ErrorDetailsNullByDefault()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle));

            Assert.IsNull(userPrompt.ViewModel.ErrorDetails);
        }

        [TestMethod]
        public void TestErrorPrompt_ShowsGivenErrorDetails()
        {
            const string expectedErrorDetails = "Expected Error Details";
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle, expectedErrorDetails));

            Assert.AreEqual(expectedErrorDetails, userPrompt.ViewModel.ErrorDetails);
        }

        [TestMethod]
        public void TestErrorPrompt_SetsCancelCaptionToOk()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle));

            Assert.AreEqual(Resources.UiOkButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
        }

        [TestMethod]
        public void TestErrorPrompt_HasActionButtonFalse()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle));

            Assert.IsFalse(userPrompt.ViewModel.HasActionButton);
        }

        [TestMethod]
        public void TestErrorPrompt_SetsIconToErrorIcon()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle));

            var bitmapImage = (BitmapImage)userPrompt.ViewModel.Icon;
            StringAssert.EndsWith(bitmapImage.UriSource.AbsolutePath, UserPromptUtils.ErrorIconPath);
        }

        [TestMethod]
        public void TestErrorActionPrompt_PromptsWithGivenTitle()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, ExpectedTitle));
            Assert.AreEqual(ExpectedTitle, userPrompt.Title);
        }

        [TestMethod]
        public void TestErrorActionPrompt_PromptsWithGivenPrompt()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorActionPrompt(ExpectedPrompt, DefaultTitle));

            Assert.AreEqual(ExpectedPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public void TestErrorActionPrompt_ErrorDetailsNullByDefault()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));

            Assert.IsNull(userPrompt.ViewModel.ErrorDetails);
        }

        [TestMethod]
        public void TestErrorActionPrompt_ShowsGivenErrorDetails()
        {
            const string expectedErrorDetails = "Expected Error Details";
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle, expectedErrorDetails));

            Assert.AreEqual(expectedErrorDetails, userPrompt.ViewModel.ErrorDetails);
        }

        [TestMethod]
        public void TestErrorActionPrompt_SetsCancelCaptionToNo()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));

            Assert.AreEqual(Resources.UiNoButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
        }

        [TestMethod]
        public void TestErrorActionPrompt_SetsActionCaptionToYes()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));

            Assert.AreEqual(Resources.UiYesButtonCaption, userPrompt.ViewModel.ActionButtonCaption);
        }

        [TestMethod]
        public void TestErrorActionPrompt_SetsIconToErrorIcon()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));

            var bitmapImage = (BitmapImage)userPrompt.ViewModel.Icon;
            StringAssert.EndsWith(bitmapImage.UriSource.AbsolutePath, UserPromptUtils.ErrorIconPath);
        }

        [TestMethod]
        public void TestErrorActionPrompt_ReturnsFalseWhenClosed()
        {
            bool result = GetUserPromptResult(
                userPrompt => userPrompt.Close(),
                () => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle)
            );
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestErrorActionPrompt_ReturnsTrueOnOkCommand()
        {
            bool result = GetUserPromptResult(
                userPrompt => userPrompt.ViewModel.ActionCommand.Execute(null),
                () => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle)
            );
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestExceptionPrompt_PromptsWithConstantTitle()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ExceptionPrompt(new Exception()));
            Assert.AreEqual(Resources.ExceptionPromptTitle, userPrompt.Title);
        }

        [TestMethod]
        public void TestExceptionPrompt_PromptsWithGivenExceptionMessage()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ExceptionPrompt(new Exception(ExpectedPrompt)));

            Assert.AreEqual(s_expectedErrorPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public void TestExceptionPrompt_PromptsWithAggregateInnerExceptionMessage()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(
                () => _objectUnderTest.ExceptionPrompt(new AggregateException(new Exception(ExpectedPrompt))));

            Assert.AreEqual(s_expectedErrorPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public void TestExceptionPrompt_PromptsAggregateExceptionFirstInnerExceptionMessage()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(
                () => _objectUnderTest.ExceptionPrompt(
                    new AggregateException(new ExceptionWithNullMessage(), new Exception(ExpectedPrompt))));

            Assert.AreEqual(s_expectedErrorPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public void TestExceptionPrompt_PromptsWithAggregateMessageWhenNoInnerExceptions()
        {
            UserPromptWindow userPrompt =
                GetUserPromptWindow(() => _objectUnderTest.ExceptionPrompt(new AggregateException(ExpectedPrompt)));

            Assert.AreEqual(s_expectedErrorPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public void TestExceptionPrompt_ShowsStacktraceAsErrorDetails()
        {
            var exception = new Exception();
            UserPromptWindow userPrompt = GetUserPromptWindow(() => _objectUnderTest.ExceptionPrompt(exception));

            Assert.AreEqual(exception.StackTrace, userPrompt.ViewModel.ErrorDetails);
        }

        [TestMethod]
        public void TestExceptionPrompt_SetsCancelCaptionToOk()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(() => _objectUnderTest.ExceptionPrompt(new Exception()));

            Assert.AreEqual(Resources.UiOkButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
        }

        [TestMethod]
        public void TestExceptionPrompt_HasActionButtonFalse()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(() => _objectUnderTest.ExceptionPrompt(new Exception()));

            Assert.IsFalse(userPrompt.ViewModel.HasActionButton);
        }

        [TestMethod]
        public void TestExceptionPrompt_SetsIconToErrorIcon()
        {
            UserPromptWindow userPrompt = GetUserPromptWindow(() => _objectUnderTest.ExceptionPrompt(new Exception()));

            var bitmapImage = (BitmapImage)userPrompt.ViewModel.Icon;
            StringAssert.EndsWith(bitmapImage.UriSource.AbsolutePath, UserPromptUtils.ErrorIconPath);
        }

        private bool GetUserPromptResult(Action<UserPromptWindow> closeAction, Func<bool> promptAction) =>
            Dispatcher.CurrentDispatcher.Invoke(
                () =>
                {
                    void OnPromptInitialized(object sender, EventArgs args)
                    {
                        closeAction((UserPromptWindow)sender);
                    }

                    UserPromptWindow.UserPromptInitialized += OnPromptInitialized;
                    try
                    {
                        return promptAction();
                    }
                    finally
                    {
                        UserPromptWindow.UserPromptInitialized -= OnPromptInitialized;
                    }
                });

        private UserPromptWindow GetUserPromptWindow(Action promptAction) => Dispatcher.CurrentDispatcher.Invoke(
            () =>
            {
                UserPromptWindow currentUserPrompt = null;

                void OnPromptInitialized(object sender, EventArgs args)
                {
                    currentUserPrompt = (UserPromptWindow)sender;
                    _defaultCloseAction(currentUserPrompt);
                }

                UserPromptWindow.UserPromptInitialized += OnPromptInitialized;
                try
                {
                    promptAction();
                    return currentUserPrompt;
                }
                finally
                {
                    UserPromptWindow.UserPromptInitialized -= OnPromptInitialized;
                }
            });
    }
}
