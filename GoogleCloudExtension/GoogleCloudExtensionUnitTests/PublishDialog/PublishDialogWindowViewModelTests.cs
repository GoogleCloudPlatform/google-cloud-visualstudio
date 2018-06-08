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

using EnvDTE;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps;
using GoogleCloudExtension.PublishDialog.Steps.Choice;
using GoogleCloudExtension.PublishDialog.Steps.Gke;
using GoogleCloudExtension.Services.VsProject;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace GoogleCloudExtensionUnitTests.PublishDialog
{
    [TestClass]
    public class PublishDialogWindowViewModelTests : ExtensionTestBase
    {
        private IParsedDteProject _mockedParsedProject;
        private PublishDialogWindowViewModel _objectUnderTest;
        private Action _mockedCloseWindowAction;
        private Mock<IStepContent<IPublishDialogStep>> _stepContentMock;
        private List<string> _changedProperties;
        private Mock<IVsProjectPropertyService> _propertyServiceMock;

        protected override void BeforeEach()
        {
            _mockedCloseWindowAction = Mock.Of<Action>();
            _mockedParsedProject = Mock.Of<IParsedDteProject>(p => p.Project == Mock.Of<Project>());
            _changedProperties = new List<string>();

            _propertyServiceMock = new Mock<IVsProjectPropertyService>();
            PackageMock.Setup(p => p.GetMefService<IVsProjectPropertyService>()).Returns(_propertyServiceMock.Object);

            _objectUnderTest = new PublishDialogWindowViewModel(_mockedParsedProject, _mockedCloseWindowAction);
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);

            _stepContentMock = new Mock<FrameworkElement>().As<IStepContent<IPublishDialogStep>>();
            _stepContentMock.DefaultValueProvider = DefaultValueProvider.Mock;
        }
        [TestMethod]
        public void TestConstructor_SetsProject()
        {
            var objectUnderTest = new PublishDialogWindowViewModel(_mockedParsedProject, _mockedCloseWindowAction);

            Assert.AreEqual(_mockedParsedProject, objectUnderTest.Project);
        }

        [TestMethod]
        public void TestConstructor_SetsPreviousCommand()
        {
            var objectUnderTest = new PublishDialogWindowViewModel(_mockedParsedProject, _mockedCloseWindowAction);

            Assert.IsFalse(objectUnderTest.PrevCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestConstructor_SetsContent()
        {
            var objectUnderTest = new PublishDialogWindowViewModel(_mockedParsedProject, _mockedCloseWindowAction);

            Assert.IsInstanceOfType(objectUnderTest.Content, typeof(ChoiceStepContent));
        }

        [TestMethod]
        public void TestConstructor_NavigatesToPreviousChoice()
        {
            _propertyServiceMock
                .Setup(
                    s => s.GetUserProperty(
                        It.IsAny<Project>(), ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName))
                .Returns(ChoiceType.Gke.ToString);

            var objectUnderTest = new PublishDialogWindowViewModel(_mockedParsedProject, _mockedCloseWindowAction);

            Assert.IsInstanceOfType(objectUnderTest.Content, typeof(GkeStepContent));
        }

        [TestMethod]
        public void TestConstructor_SetsPublishCommand()
        {
            var objectUnderTest = new PublishDialogWindowViewModel(_mockedParsedProject, _mockedCloseWindowAction);

            Assert.AreEqual(
                ((ChoiceStepContent)objectUnderTest.Content).ViewModel.PublishCommand,
                objectUnderTest.PublishCommand);
        }

        [TestMethod]
        public void TestNavigateToStep_UpdatesContent()
        {
            _objectUnderTest.NavigateToStep(_stepContentMock.Object);

            Assert.AreEqual(_stepContentMock.Object, _objectUnderTest.Content);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.Content));
        }

        [TestMethod]
        public void TestNavigateToStep_UpdatesPreviousCommand()
        {
            _objectUnderTest.NavigateToStep(_stepContentMock.Object);

            Assert.IsTrue(_objectUnderTest.PrevCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestNavigateToStep_UpdatesPublishCommand()
        {
            var mockedPublishCommand = Mock.Of<IProtectedCommand>();
            _stepContentMock.SetupGet(c => c.ViewModel.PublishCommand).Returns(mockedPublishCommand);

            _objectUnderTest.NavigateToStep(_stepContentMock.Object);

            Assert.AreEqual(mockedPublishCommand, _objectUnderTest.PublishCommand);
            CollectionAssert.Contains(_changedProperties, nameof(_objectUnderTest.PublishCommand));
        }

        [TestMethod]
        public void TestNavigateToStep_UpdatesHasErrors()
        {
            _stepContentMock.SetupGet(c => c.ViewModel.HasErrors).Returns(true);

            _objectUnderTest.NavigateToStep(_stepContentMock.Object);

            Assert.IsTrue(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public void TestNavigateToStep_DelegatesErrorsChangedEvents()
        {
            DataErrorsChangedEventArgs receivedArgs = null;
            _objectUnderTest.ErrorsChanged += (sender, args) => receivedArgs = args;

            _objectUnderTest.NavigateToStep(_stepContentMock.Object);
            var sentArgs = new DataErrorsChangedEventArgs("TestProperty");
            _stepContentMock.Raise(s => s.ViewModel.ErrorsChanged += null, sentArgs);

            Assert.AreEqual(sentArgs, receivedArgs);
        }

        [TestMethod]
        public void TestPreviousCommand_UpdatesContent()
        {
            _objectUnderTest.NavigateToStep(_stepContentMock.Object);
            _objectUnderTest.PrevCommand.Execute(null);

            Assert.IsInstanceOfType(_objectUnderTest.Content, typeof(ChoiceStepContent));
        }

        [TestMethod]
        public void TestPreviousCommand_UpdatesPreviousCommand()
        {
            _objectUnderTest.NavigateToStep(_stepContentMock.Object);
            _objectUnderTest.PrevCommand.Execute(null);

            Assert.AreEqual(
                ((ChoiceStepContent)_objectUnderTest.Content).ViewModel.PublishCommand,
                _objectUnderTest.PublishCommand);
        }

        [TestMethod]
        public void TestPreviousCommand_UpdatesPublishCommand()
        {
            var mockedPublishCommand = Mock.Of<IProtectedCommand>();
            _stepContentMock.SetupGet(c => c.ViewModel.PublishCommand).Returns(mockedPublishCommand);

            _objectUnderTest.NavigateToStep(_stepContentMock.Object);
            _objectUnderTest.PrevCommand.Execute(null);

            Assert.AreEqual(
                ((ChoiceStepContent)_objectUnderTest.Content).ViewModel.PublishCommand,
                _objectUnderTest.PublishCommand);
        }

        [TestMethod]
        public void TestPreviousCommand_UpdatesHasErrors()
        {
            _stepContentMock.SetupGet(c => c.ViewModel.HasErrors).Returns(true);

            _objectUnderTest.NavigateToStep(_stepContentMock.Object);
            _objectUnderTest.PrevCommand.Execute(null);

            Assert.IsFalse(_objectUnderTest.HasErrors);
        }

        [TestMethod]
        public void TestPreviousCommand_UpdatesErrorsChangedEvents()
        {
            DataErrorsChangedEventArgs receivedArgs = null;
            _objectUnderTest.ErrorsChanged += (sender, args) => receivedArgs = args;

            _objectUnderTest.NavigateToStep(_stepContentMock.Object);
            _objectUnderTest.PrevCommand.Execute(null);
            var sentArgs = new DataErrorsChangedEventArgs("TestProperty");
            _stepContentMock.Raise(s => s.ViewModel.ErrorsChanged += null, sentArgs);

            Assert.IsNull(receivedArgs);
        }

        [TestMethod]
        public void TestGetErrors_DelegatesToCurrentStep()
        {
            var returnValue = Mock.Of<IEnumerable>();
            const string propertyName = "TestPropertyName";
            _objectUnderTest.NavigateToStep(_stepContentMock.Object);
            _stepContentMock.Setup(c => c.ViewModel.GetErrors(propertyName)).Returns(() => returnValue);

            IEnumerable result = _objectUnderTest.GetErrors(propertyName);

            Assert.AreEqual(returnValue, result);
        }

        [TestMethod]
        public void TestFinishFlow_TriggersFlowFinishedEvent()
        {
            var isCalled = false;
            _objectUnderTest.FlowFinished += (sender, args) => isCalled = true;

            _objectUnderTest.FinishFlow();

            Assert.IsTrue(isCalled);
        }

        [TestMethod]
        public void TestFinishFlow_ClosesWindow()
        {
            _objectUnderTest.FinishFlow();

            Mock.Get(_mockedCloseWindowAction).Verify(f => f());
        }
    }
}
