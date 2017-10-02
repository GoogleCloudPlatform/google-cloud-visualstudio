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

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.PublishDialogSteps.FlexStep;
using GoogleCloudExtension.UserPrompt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.FlexStep
{
    [TestClass]
    public class FlexStepViewModelTests
    {
        private const string TargetProjectId = "TargetProjectId";
        private const string VisualStudioProjectName = "VisualStudioProjectName";

        private static readonly string s_pickProjectDialogTitle =
            string.Format(Resources.PublishDialogFlexSelectGcpProjectTitle, VisualStudioProjectName);

        private FlexStepViewModel _objectUnderTest;
        private Mock<Func<string, string>> _pickProjectPromptMock;
        private List<string> _changedProperties;
        private Mock<Func<UserPromptWindow.Options, bool>> _promptUserFunctionMock;



        [TestInitialize]
        public void BeforeEach()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            _promptUserFunctionMock = new Mock<Func<UserPromptWindow.Options, bool>>();
            UserPromptWindow.PromptUserFunction = _promptUserFunctionMock.Object;
            _objectUnderTest = FlexStepViewModel.CreateStep(VisualStudioProjectName);
            _pickProjectPromptMock = new Mock<Func<string, string>>();
            _objectUnderTest.PickProjectPrompt = _pickProjectPromptMock.Object;
            _changedProperties = new List<string>();
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
        }

        [TestMethod]
        public void TestSelectProjectCommandCanceled()
        {
            _pickProjectPromptMock.Setup(f => f(It.IsAny<string>())).Returns((string)null);

            _objectUnderTest.SelectProjectCommand.Execute(null);

            CollectionAssert.DoesNotContain(_changedProperties, nameof(FlexStepViewModel.GcpProjectId));
            _pickProjectPromptMock.Verify(f => f(s_pickProjectDialogTitle), Times.Once);
        }

        [TestMethod]
        public void TestSelectProjectCommand()
        {
            _pickProjectPromptMock.Setup(f => f(It.IsAny<string>())).Returns(TargetProjectId);

            _objectUnderTest.SelectProjectCommand.Execute(null);

            CollectionAssert.Contains(_changedProperties, nameof(FlexStepViewModel.GcpProjectId));
            _pickProjectPromptMock.Verify(f => f(s_pickProjectDialogTitle), Times.Once);
        }

        [TestMethod]
        public void TestGcpProjectIdWithProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(new Project { ProjectId = TargetProjectId });

            Assert.AreEqual(TargetProjectId, _objectUnderTest.GcpProjectId);
        }

        [TestMethod]
        public void TestGcpProjectIdWithNoProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);

            Assert.IsNull(_objectUnderTest.GcpProjectId);
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
