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
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Theming;
using GoogleCloudExtension.UserPrompt;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.Services
{
    public static class UserPromptServiceTests
    {
        [TestClass]
        public class StaticTests : ExtensionTestBase
        {
            [TestMethod]
            public void TestDefault_DefersToPackage()
            {
                Assert.AreEqual(UserPromptService.Default, GoogleCloudExtensionPackage.Instance.UserPromptService);
            }
        }

        [TestClass]
        public class UserPromptWindowTests : WpfTestBase<UserPromptWindow>
        {
            private const string DefaultPrompt = "Default Prompt";
            private const string DefaultTitle = "Default Title";
            private const string ExpectedPrompt = "Expected Prompt";
            private const string ExpectedTitle = "Expected Title";
            private const string ExpectedCancelCaption = "Expected Cancel Caption";
            private const string ExpectedActionCaption = "Expected Action Caption";
            private const string ExpectedMessage = "Expected Message";
            private UserPromptService _objectUnderTest;

            private static readonly string s_expectedErrorPrompt = string.Format(
                Resources.ExceptionPromptMessage,
                ExpectedPrompt);

            [TestInitialize]
            public void BeforeEach() => _objectUnderTest = new UserPromptService();

            [TestMethod]
            public async Task TestActionPrompt_PromptsWithTitle()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ActionPrompt(DefaultPrompt, ExpectedTitle));
                string titleResult = userPrompt.Title;
                Assert.AreEqual(ExpectedTitle, titleResult);
            }

            [TestMethod]
            public async Task TestActionPrompt_PromptsWithGivenPrompt()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ActionPrompt(ExpectedPrompt, DefaultTitle));
                Assert.AreEqual(ExpectedPrompt, userPrompt.ViewModel.Prompt);
            }

            [TestMethod]
            public async Task TestActionPrompt_PromptsWithNullMessageByDefault()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
                Assert.IsNull(userPrompt.ViewModel.Message);
            }

            [TestMethod]
            public async Task TestActionPrompt_PromptsWithGivenMessage()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(
                    () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle, ExpectedMessage));
                Assert.AreEqual(ExpectedMessage, userPrompt.ViewModel.Message);
            }

            [TestMethod]
            public async Task TestActionPrompt_PromptsWithDefaultActionCaption()
            {
                string defaultActionCaption = Resources.UiYesButtonCaption;
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
                Assert.AreEqual(defaultActionCaption, userPrompt.ViewModel.ActionButtonCaption);
            }

            [TestMethod]
            public async Task TestActionPrompt_PromptsWithGivenActionCaption()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(
                    () => _objectUnderTest.ActionPrompt(
                        DefaultPrompt,
                        DefaultTitle,
                        actionCaption: ExpectedActionCaption));
                Assert.AreEqual(ExpectedActionCaption, userPrompt.ViewModel.ActionButtonCaption);
            }

            [TestMethod]
            public async Task TestActionPrompt_PromptsWithDefaultCancelCaption()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
                Assert.AreEqual(Resources.UiCancelButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
            }

            [TestMethod]
            public async Task TestActionPrompt_PromptsWithGivenCancelCaption()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(
                    () => _objectUnderTest.ActionPrompt(
                        DefaultPrompt,
                        DefaultTitle,
                        cancelCaption: ExpectedCancelCaption));
                Assert.AreEqual(ExpectedCancelCaption, userPrompt.ViewModel.CancelButtonCaption);
            }

            [TestMethod]
            public async Task TestActionPrompt_PromptsWithNoIconByDefault()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle));
                Assert.IsNull(userPrompt.ViewModel.Icon);
            }

            [TestMethod]
            public async Task TestActionPrompt_PromptsWithWariningIcon()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(
                    () => _objectUnderTest.ActionPrompt(DefaultPrompt, DefaultTitle, isWarning: true));

                var bitmapImage = (BitmapImage)userPrompt.ViewModel.Icon;
                StringAssert.EndsWith(bitmapImage.UriSource.AbsolutePath, UserPromptService.WarningIconPath);
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
            public async Task TestOkPrompt_PromptsWithTitle()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(() => _objectUnderTest.OkPrompt(DefaultPrompt, ExpectedTitle));
                Assert.AreEqual(ExpectedTitle, userPrompt.Title);
            }

            [TestMethod]
            public async Task TestOkPrompt_PromptsWithGivenPrompt()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(() => _objectUnderTest.OkPrompt(ExpectedPrompt, DefaultTitle));
                Assert.AreEqual(ExpectedPrompt, userPrompt.ViewModel.Prompt);
            }

            [TestMethod]
            public async Task TestOkPrompt_SetsCancelCaptionToOk()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(() => _objectUnderTest.OkPrompt(DefaultPrompt, DefaultTitle));
                Assert.AreEqual(Resources.UiOkButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
            }

            [TestMethod]
            public async Task TestOkPrompt_HasActionButtonFalse()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(() => _objectUnderTest.OkPrompt(DefaultPrompt, DefaultTitle));
                Assert.IsFalse(userPrompt.ViewModel.HasActionButton);
            }

            [TestMethod]
            public async Task TestErrorPrompt_PromptsWithGivenTitle()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, ExpectedTitle));
                Assert.AreEqual(ExpectedTitle, userPrompt.Title);
            }

            [TestMethod]
            public async Task TestErrorPrompt_PromptsWithGivenPrompt()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorPrompt(ExpectedPrompt, DefaultTitle));

                Assert.AreEqual(ExpectedPrompt, userPrompt.ViewModel.Prompt);
            }

            [TestMethod]
            public async Task TestErrorPrompt_ErrorDetailsNullByDefault()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle));

                Assert.IsNull(userPrompt.ViewModel.ErrorDetails);
            }

            [TestMethod]
            public async Task TestErrorPrompt_ShowsGivenErrorDetails()
            {
                const string expectedErrorDetails = "Expected Error Details";
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle, expectedErrorDetails));

                Assert.AreEqual(expectedErrorDetails, userPrompt.ViewModel.ErrorDetails);
            }

            [TestMethod]
            public async Task TestErrorPrompt_SetsCancelCaptionToOk()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle));

                Assert.AreEqual(Resources.UiOkButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
            }

            [TestMethod]
            public async Task TestErrorPrompt_HasActionButtonFalse()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle));

                Assert.IsFalse(userPrompt.ViewModel.HasActionButton);
            }

            [TestMethod]
            public async Task TestErrorPrompt_SetsIconToErrorIcon()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorPrompt(DefaultPrompt, DefaultTitle));

                var bitmapImage = (BitmapImage)userPrompt.ViewModel.Icon;
                StringAssert.EndsWith(bitmapImage.UriSource.AbsolutePath, UserPromptService.ErrorIconPath);
            }

            [TestMethod]
            public async Task TestErrorActionPrompt_PromptsWithGivenTitle()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, ExpectedTitle));
                Assert.AreEqual(ExpectedTitle, userPrompt.Title);
            }

            [TestMethod]
            public async Task TestErrorActionPrompt_PromptsWithGivenPrompt()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorActionPrompt(ExpectedPrompt, DefaultTitle));

                Assert.AreEqual(ExpectedPrompt, userPrompt.ViewModel.Prompt);
            }

            [TestMethod]
            public async Task TestErrorActionPrompt_ErrorDetailsNullByDefault()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));

                Assert.IsNull(userPrompt.ViewModel.ErrorDetails);
            }

            [TestMethod]
            public async Task TestErrorActionPrompt_ShowsGivenErrorDetails()
            {
                const string expectedErrorDetails = "Expected Error Details";
                UserPromptWindow userPrompt =
                    await GetWindowAsync(
                        () => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle, expectedErrorDetails));

                Assert.AreEqual(expectedErrorDetails, userPrompt.ViewModel.ErrorDetails);
            }

            [TestMethod]
            public async Task TestErrorActionPrompt_SetsCancelCaptionToNo()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));

                Assert.AreEqual(Resources.UiNoButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
            }

            [TestMethod]
            public async Task TestErrorActionPrompt_SetsActionCaptionToYes()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));

                Assert.AreEqual(Resources.UiYesButtonCaption, userPrompt.ViewModel.ActionButtonCaption);
            }

            [TestMethod]
            public async Task TestErrorActionPrompt_SetsIconToErrorIcon()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ErrorActionPrompt(DefaultPrompt, DefaultTitle));

                var bitmapImage = (BitmapImage)userPrompt.ViewModel.Icon;
                StringAssert.EndsWith(bitmapImage.UriSource.AbsolutePath, UserPromptService.ErrorIconPath);
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
            public async Task TestExceptionPrompt_PromptsWithConstantTitle()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ExceptionPrompt(new Exception()));
                Assert.AreEqual(Resources.ExceptionPromptTitle, userPrompt.Title);
            }

            [TestMethod]
            public async Task TestExceptionPrompt_PromptsWithGivenExceptionMessage()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ExceptionPrompt(new Exception(ExpectedPrompt)));

                Assert.AreEqual(s_expectedErrorPrompt, userPrompt.ViewModel.Prompt);
            }

            [TestMethod]
            public async Task TestExceptionPrompt_PromptsWithAggregateInnerExceptionMessage()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(
                    () => _objectUnderTest.ExceptionPrompt(new AggregateException(new Exception(ExpectedPrompt))));

                Assert.AreEqual(s_expectedErrorPrompt, userPrompt.ViewModel.Prompt);
            }

            [TestMethod]
            public async Task TestExceptionPrompt_PromptsAggregateExceptionFirstInnerExceptionMessage()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(
                    () => _objectUnderTest.ExceptionPrompt(
                        new AggregateException(new ExceptionWithNullMessage(), new Exception(ExpectedPrompt))));

                Assert.AreEqual(s_expectedErrorPrompt, userPrompt.ViewModel.Prompt);
            }

            [TestMethod]
            public async Task TestExceptionPrompt_PromptsWithAggregateMessageWhenNoInnerExceptions()
            {
                UserPromptWindow userPrompt =
                    await GetWindowAsync(() => _objectUnderTest.ExceptionPrompt(new AggregateException(ExpectedPrompt)));

                Assert.AreEqual(s_expectedErrorPrompt, userPrompt.ViewModel.Prompt);
            }

            [TestMethod]
            public async Task TestExceptionPrompt_ShowsStacktraceAsErrorDetails()
            {
                var exception = new Exception();
                UserPromptWindow userPrompt = await GetWindowAsync(() => _objectUnderTest.ExceptionPrompt(exception));

                Assert.AreEqual(exception.StackTrace, userPrompt.ViewModel.ErrorDetails);
            }

            [TestMethod]
            public async Task TestExceptionPrompt_SetsCancelCaptionToOk()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(() => _objectUnderTest.ExceptionPrompt(new Exception()));

                Assert.AreEqual(Resources.UiOkButtonCaption, userPrompt.ViewModel.CancelButtonCaption);
            }

            [TestMethod]
            public async Task TestExceptionPrompt_HasActionButtonFalse()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(() => _objectUnderTest.ExceptionPrompt(new Exception()));

                Assert.IsFalse(userPrompt.ViewModel.HasActionButton);
            }

            [TestMethod]
            public async Task TestExceptionPrompt_SetsIconToErrorIcon()
            {
                UserPromptWindow userPrompt = await GetWindowAsync(() => _objectUnderTest.ExceptionPrompt(new Exception()));

                var bitmapImage = (BitmapImage)userPrompt.ViewModel.Icon;
                StringAssert.EndsWith(bitmapImage.UriSource.AbsolutePath, UserPromptService.ErrorIconPath);
            }

            protected override void RegisterActivatedEvent(EventHandler handler)
            {
                UserPromptService.UserPromptActivated += handler;
            }

            protected override void UnregisterActivatedEvent(EventHandler handler)
            {
                UserPromptService.UserPromptActivated -= handler;
            }
        }

        [TestClass]
        public class CommonDialogWindowTests : WpfTestBase<CommonDialogWindowBase>
        {
            private UserPromptService _objectUnderTest;

            [TestInitialize]
            public void BeforeEach()
            {
                _objectUnderTest = new UserPromptService();
            }

            [TestMethod]
            public async Task TestPromptUser0_SetsWindowContent()
            {
                var mockedContent = Mock.Of<ICommonWindowContent<ICloseSource>>(
                    c => c.Title == "DefaultTitle" && c.ViewModel == Mock.Of<ICloseSource>());

                CommonDialogWindowBase window = await GetWindowAsync(() => _objectUnderTest.PromptUser(mockedContent));

                Assert.AreEqual(mockedContent, window.Content);
            }

            [TestMethod]
            public async Task TestPromptUser1_SetsWindowContent()
            {
                var mockedContent =
                    Mock.Of<ICommonWindowContent<IViewModelBase<string>>>(
                        c => c.Title == "WindowTitle" && c.ViewModel.Result == "DefaultResult");

                CommonDialogWindowBase window = await GetWindowAsync(() => _objectUnderTest.PromptUser(mockedContent));

                Assert.AreEqual(mockedContent, window.Content);
            }

            [TestMethod]
            public async Task TestPromptUser1_ReturnsResult()
            {
                const string expectedResult = "expected result";

                string result = await GetResult(
                    w => w.Close(),
                    () => _objectUnderTest.PromptUser(
                        Mock.Of<ICommonWindowContent<IViewModelBase<string>>>(
                            c => c.ViewModel.Result == expectedResult && c.Title == "WindowTitle")));

                Assert.AreEqual(expectedResult, result);
            }

            protected override void RegisterActivatedEvent(EventHandler handler) =>
                UserPromptService.UserPromptActivated += handler;

            protected override void UnregisterActivatedEvent(EventHandler handler) =>
                UserPromptService.UserPromptActivated -= handler;
        }
    }
}