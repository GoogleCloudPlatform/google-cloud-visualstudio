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
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
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
    public class UserPromptUtilsTests : WpfTestBase<UserPromptWindow>
    {
        private const string DefaultPrompt = "Default Prompt";
        private const string DefaultTitle = "Default Title";
        private const string ExpectedPrompt = "Expected Prompt";
        private const string ExpectedTitle = "Expected Title";
        private const string ExpectedCancelCaption = "Expected Cancel Caption";
        private const string ExpectedActionCaption = "Expected Action Caption";
        private const string ExpectedMessage = "Expected Message";
        private UserPromptUtils _objectUnderTest;

        private static readonly string s_expectedErrorPrompt = string.Format(
            Resources.ExceptionPromptMessage,
            ExpectedPrompt);

        [TestInitialize]
        public void BeforeEach() => _objectUnderTest = new UserPromptUtils();

        [TestMethod]
        public async Task TestActionPrompt_PromptsWithTitleAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ActionPrompt(DefaultPrompt, ExpectedTitle));
            string titleResult = userPrompt.Title;
            Assert.AreEqual(ExpectedTitle, titleResult);
        }

        [TestMethod]
        public async Task TestActionPrompt_PromptsWithGivenPromptAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ActionPrompt(ExpectedPrompt, DefaultTitle));
            Assert.AreEqual(ExpectedPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public async Task TestActionPrompt_PromptsWithNullMessageByDefaultAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
            Assert.IsNull(userPrompt.ViewModel.Message);
        }

        [TestMethod]
        public async Task TestActionPrompt_PromptsWithGivenMessageAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(
                () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle, ExpectedMessage));
            Assert.AreEqual(ExpectedMessage, userPrompt.ViewModel.Message);
        }

        [TestMethod]
        public async Task TestActionPrompt_PromptsWithDefaultActionCaptionAsync()
        {
            string defaultActionCaption = Resources.UiYesButtonCaption;
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
            Assert.AreEqual(defaultActionCaption, userPrompt.ViewModel.ActionButtonCaption);
        }

        [TestMethod]
        public async Task TestActionPrompt_PromptsWithGivenActionCaptionAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(
                () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle, actionCaption: ExpectedActionCaption));
            Assert.AreEqual(ExpectedActionCaption, userPrompt.ViewModel.ActionButtonCaption);
        }

        [TestMethod]
        public async Task TestActionPrompt_PromptsWithDefaultCancelCaptionAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
            Assert.AreEqual(Resources.UiCancelButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
        }

        [TestMethod]
        public async Task TestActionPrompt_PromptsWithGivenCancelCaptionAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(
                () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle, cancelCaption: ExpectedCancelCaption));
            Assert.AreEqual(ExpectedCancelCaption, userPrompt.ViewModel.CancelButtonCaption);
        }

        [TestMethod]
        public async Task TestActionPrompt_PromptsWithNoIconByDefaultAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
            Assert.IsNull(userPrompt.ViewModel.Icon);
        }

        [TestMethod]
        public async Task TestActionPrompt_PromptsWithWariningIconAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(
                () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle, isWarning: true));

            var bitmapImage = (BitmapImage)userPrompt.ViewModel.Icon;
            StringAssert.EndsWith(bitmapImage.UriSource.AbsolutePath, UserPromptUtils.WarningIconPath);
        }

        [TestMethod]
        public async Task TestActionPrompt_ReturnsFalseWhenClosed()
        {
            bool result = await GetResult(
                userPrompt => userPrompt.Close(),
                () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestActionPrompt_ReturnsTrueOnOkCommand()
        {
            bool result = await GetResult(
                userPrompt => userPrompt.ViewModel.ActionCommand.Execute(null),
                () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestOkPrompt_PromptsWithTitleAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(() => _objectUnderTest.OkPrompt(DefaultPrompt, ExpectedTitle));
            Assert.AreEqual(ExpectedTitle, userPrompt.Title);
        }

        [TestMethod]
        public async Task TestOkPrompt_PromptsWithGivenPromptAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(() => _objectUnderTest.OkPrompt(ExpectedPrompt, DefaultTitle));
            Assert.AreEqual(ExpectedPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public async Task TestOkPrompt_SetsCancelCaptionToOkAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(() => _objectUnderTest.OkPrompt(DefaultPrompt, DefaultTitle));
            Assert.AreEqual(Resources.UiOkButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
        }

        [TestMethod]
        public async Task TestOkPrompt_HasActionButtonFalseAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(() => _objectUnderTest.OkPrompt(DefaultPrompt, DefaultTitle));
            Assert.IsFalse(userPrompt.ViewModel.HasActionButton);
        }

        [TestMethod]
        public async Task TestErrorPrompt_PromptsWithGivenTitleAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, ExpectedTitle));
            Assert.AreEqual(ExpectedTitle, userPrompt.Title);
        }

        [TestMethod]
        public async Task TestErrorPrompt_PromptsWithGivenPromptAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorPrompt(ExpectedPrompt, DefaultTitle));

            Assert.AreEqual(ExpectedPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public async Task TestErrorPrompt_ErrorDetailsNullByDefaultAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle));

            Assert.IsNull(userPrompt.ViewModel.ErrorDetails);
        }

        [TestMethod]
        public async Task TestErrorPrompt_ShowsGivenErrorDetailsAsync()
        {
            const string expectedErrorDetails = "Expected Error Details";
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle, expectedErrorDetails));

            Assert.AreEqual(expectedErrorDetails, userPrompt.ViewModel.ErrorDetails);
        }

        [TestMethod]
        public async Task TestErrorPrompt_SetsCancelCaptionToOkAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle));

            Assert.AreEqual(Resources.UiOkButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
        }

        [TestMethod]
        public async Task TestErrorPrompt_HasActionButtonFalseAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle));

            Assert.IsFalse(userPrompt.ViewModel.HasActionButton);
        }

        [TestMethod]
        public async Task TestErrorPrompt_SetsIconToErrorIconAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle));

            var bitmapImage = (BitmapImage)userPrompt.ViewModel.Icon;
            StringAssert.EndsWith(bitmapImage.UriSource.AbsolutePath, UserPromptUtils.ErrorIconPath);
        }

        [TestMethod]
        public async Task TestErrorActionPrompt_PromptsWithGivenTitleAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, ExpectedTitle));
            Assert.AreEqual(ExpectedTitle, userPrompt.Title);
        }

        [TestMethod]
        public async Task TestErrorActionPrompt_PromptsWithGivenPromptAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorActionPrompt(ExpectedPrompt, DefaultTitle));

            Assert.AreEqual(ExpectedPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public async Task TestErrorActionPrompt_ErrorDetailsNullByDefaultAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));

            Assert.IsNull(userPrompt.ViewModel.ErrorDetails);
        }

        [TestMethod]
        public async Task TestErrorActionPrompt_ShowsGivenErrorDetailsAsync()
        {
            const string expectedErrorDetails = "Expected Error Details";
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle, expectedErrorDetails));

            Assert.AreEqual(expectedErrorDetails, userPrompt.ViewModel.ErrorDetails);
        }

        [TestMethod]
        public async Task TestErrorActionPrompt_SetsCancelCaptionToNoAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));

            Assert.AreEqual(Resources.UiNoButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
        }

        [TestMethod]
        public async Task TestErrorActionPrompt_SetsActionCaptionToYesAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));

            Assert.AreEqual(Resources.UiYesButtonCaption, userPrompt.ViewModel.ActionButtonCaption);
        }

        [TestMethod]
        public async Task TestErrorActionPrompt_SetsIconToErrorIconAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));

            var bitmapImage = (BitmapImage)userPrompt.ViewModel.Icon;
            StringAssert.EndsWith(bitmapImage.UriSource.AbsolutePath, UserPromptUtils.ErrorIconPath);
        }

        [TestMethod]
        public async Task TestErrorActionPrompt_ReturnsFalseWhenClosed()
        {
            bool result = await GetResult(
                userPrompt => userPrompt.Close(),
                () => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestErrorActionPrompt_ReturnsTrueOnOkCommand()
        {
            bool result = await GetResult(
                userPrompt => userPrompt.ViewModel.ActionCommand.Execute(null),
                () => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestExceptionPrompt_PromptsWithConstantTitleAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ExceptionPrompt(new Exception()));
            Assert.AreEqual(Resources.ExceptionPromptTitle, userPrompt.Title);
        }

        [TestMethod]
        public async Task TestExceptionPrompt_PromptsWithGivenExceptionMessageAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ExceptionPrompt(new Exception(ExpectedPrompt)));

            Assert.AreEqual(s_expectedErrorPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public async Task TestExceptionPrompt_PromptsWithAggregateInnerExceptionMessageAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(
                () => _objectUnderTest.ExceptionPrompt(new AggregateException(new Exception(ExpectedPrompt))));

            Assert.AreEqual(s_expectedErrorPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public async Task TestExceptionPrompt_PromptsAggregateExceptionFirstInnerExceptionMessageAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(
                () => _objectUnderTest.ExceptionPrompt(
                    new AggregateException(new ExceptionWithNullMessage(), new Exception(ExpectedPrompt))));

            Assert.AreEqual(s_expectedErrorPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public async Task TestExceptionPrompt_PromptsWithAggregateMessageWhenNoInnerExceptionsAsync()
        {
            UserPromptWindow userPrompt =
                await GetWindow(() => _objectUnderTest.ExceptionPrompt(new AggregateException(ExpectedPrompt)));

            Assert.AreEqual(s_expectedErrorPrompt, userPrompt.ViewModel.Prompt);
        }

        [TestMethod]
        public async Task TestExceptionPrompt_ShowsStacktraceAsErrorDetailsAsync()
        {
            var exception = new Exception();
            UserPromptWindow userPrompt = await GetWindow(() => _objectUnderTest.ExceptionPrompt(exception));

            Assert.AreEqual(exception.StackTrace, userPrompt.ViewModel.ErrorDetails);
        }

        [TestMethod]
        public async Task TestExceptionPrompt_SetsCancelCaptionToOkAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(() => _objectUnderTest.ExceptionPrompt(new Exception()));

            Assert.AreEqual(Resources.UiOkButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
        }

        [TestMethod]
        public async Task TestExceptionPrompt_HasActionButtonFalseAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(() => _objectUnderTest.ExceptionPrompt(new Exception()));

            Assert.IsFalse(userPrompt.ViewModel.HasActionButton);
        }

        [TestMethod]
        public async Task TestExceptionPrompt_SetsIconToErrorIconAsync()
        {
            UserPromptWindow userPrompt = await GetWindow(() => _objectUnderTest.ExceptionPrompt(new Exception()));

            var bitmapImage = (BitmapImage)userPrompt.ViewModel.Icon;
            StringAssert.EndsWith(bitmapImage.UriSource.AbsolutePath, UserPromptUtils.ErrorIconPath);
        }

        protected override void RegisterActivatedEvent(EventHandler handler)
        {
            UserPromptWindow.UserPromptActivated += handler;
        }

        protected override void UnregisterActivatedEvent(EventHandler handler)
        {
            UserPromptWindow.UserPromptActivated -= handler;
        }
    }
}
