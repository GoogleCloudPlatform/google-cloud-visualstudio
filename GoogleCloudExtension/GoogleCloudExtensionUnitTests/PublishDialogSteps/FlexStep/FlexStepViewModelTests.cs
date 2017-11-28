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
using GoogleCloudExtension.ApiManagement;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.FlexStep;
using GoogleCloudExtension.UserPrompt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.FlexStep
{
    /// <summary>
    /// Unit tests for <seealso cref="FlexStepViewModel"/>.
    /// </summary>
    [TestClass]
    public class FlexStepViewModelTests
    {
        private FlexStepViewModel _objectUnderTest;
        private Mock<Func<UserPromptWindow.Options, bool>> _promptUserFunctionMock;
        private TaskCompletionSource<bool> _areServicesEnabledTaskSource;
        private TaskCompletionSource<Google.Apis.Appengine.v1.Data.Application> _appTaskSource;
        private Mock<IApiManager> _mockedApiManager;
        private Mock<IGaeDataSource> _mockedGaeDataSource;
        private Mock<IPublishDialog> _mockedPublishDialog;

        [TestInitialize]
        public void Initialize()
        {
            _promptUserFunctionMock = new Mock<Func<UserPromptWindow.Options, bool>>();
            UserPromptWindow.PromptUserFunction = _promptUserFunctionMock.Object;

            _mockedApiManager = new Mock<IApiManager>();
            _mockedGaeDataSource = new Mock<IGaeDataSource>();
            _mockedPublishDialog = new Mock<IPublishDialog>();

            _areServicesEnabledTaskSource = new TaskCompletionSource<bool>();
            _appTaskSource = new TaskCompletionSource<Google.Apis.Appengine.v1.Data.Application>();
            
            _mockedApiManager.Setup(x => x.AreServicesEnabledAsync(It.IsAny<IList<string>>())).Returns(() => _areServicesEnabledTaskSource.Task);
            _mockedGaeDataSource.Setup(x => x.GetApplicationAsync()).Returns(() => _appTaskSource.Task);
            _mockedPublishDialog.Setup(x => x.TrackTask(It.IsAny<Task>()));

            _objectUnderTest = FlexStepViewModel.CreateStep(dataSource: _mockedGaeDataSource.Object, apiManager: _mockedApiManager.Object);
        }

        [TestMethod]
        public void TestInitialState()
        {
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.GeneralError);
        }

        [TestMethod]
        public void TestStateAfterOnPushedToDialog()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);

            Assert.IsNotNull(_objectUnderTest.LoadingProjectTask);
            Assert.IsTrue(_objectUnderTest.LoadingProject);
            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.GeneralError);
        }

        [TestMethod]
        public async Task TestPositiveProjectValidation()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);
            _areServicesEnabledTaskSource.SetResult(true);
            _appTaskSource.SetResult(new Google.Apis.Appengine.v1.Data.Application());

            await _objectUnderTest.LoadingProjectTask;

            Assert.IsTrue(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.GeneralError);
        }

        [TestMethod]
        public async Task TestErrorCheckingServices()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);
            _areServicesEnabledTaskSource.SetException(new DataSourceException());

            await _objectUnderTest.LoadingProjectTask;

            Assert.IsTrue(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public async Task TestErrorObtainingApp()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);
            _areServicesEnabledTaskSource.SetResult(true);
            _appTaskSource.SetException(new DataSourceException());

            await _objectUnderTest.LoadingProjectTask;

            Assert.IsTrue(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
        }

        [TestMethod]
        public async Task TestNeedsApiState()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);
            _areServicesEnabledTaskSource.SetResult(false);

            await _objectUnderTest.LoadingProjectTask;

            Assert.IsTrue(_objectUnderTest.NeedsApiEnabled);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.NeedsAppCreated);
        }

        [TestMethod]
        public async Task TestNeedsAppState()
        {
            _objectUnderTest.OnPushedToDialog(_mockedPublishDialog.Object);
            _areServicesEnabledTaskSource.SetResult(true);
            _appTaskSource.SetResult(null);

            await _objectUnderTest.LoadingProjectTask;

            Assert.IsTrue(_objectUnderTest.NeedsAppCreated);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.GeneralError);
            Assert.IsFalse(_objectUnderTest.NeedsApiEnabled);
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
