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
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.ChoiceStep;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.PublishDialogSteps.ChoiceStep
{
    [TestClass]
    public class ChoiceStepViewModelTests
    {
        private const string VisualStudioProjectName = "VisualStudioProjectName";

        private ChoiceStepViewModel _objectUnderTest;
        private IPublishDialog _mockedPublishDialog;
        private Mock<IParsedProject> _vsProjectMock;

        [TestInitialize]
        public virtual void BeforeEach()
        {
            _vsProjectMock = new Mock<IParsedProject>(MockBehavior.Strict);
            _vsProjectMock.SetupGet(p => p.Name).Returns(VisualStudioProjectName);

            Mock<IPublishDialog> publishDialogMock = new Mock<IPublishDialog>(MockBehavior.Strict);
            publishDialogMock.Setup(pd => pd.Project).Returns(_vsProjectMock.Object);
            _mockedPublishDialog = publishDialogMock.Object;

            _objectUnderTest = ChoiceStepViewModel.CreateStep();
        }

        [TestCleanup]
        public void AfterEach()
        {
            _objectUnderTest.OnFlowFinished();
        }

        [TestMethod]
        public void TestInitialState()
        {
            AssertInvariants();
            AssertInitialState();
        }

        [TestMethod]
        public void TestChoicesWebApplication()
        {
            OnVisible(KnownProjectTypes.WebApplication);

            AssertInvariants();
            AssertWebApplicationChoices();
        }

        [TestMethod]
        public void TestChoicesCore_1_0()
        {
            OnVisible(KnownProjectTypes.NetCoreWebApplication1_0);

            AssertInvariants();
            AssertCoreChoices();
        }

        [TestMethod]
        public void TestChoicesCore_1_1()
        {
            OnVisible(KnownProjectTypes.NetCoreWebApplication1_1);

            AssertInvariants();
            AssertCoreChoices();
        }

        [TestMethod]
        public void TestChoicesCore_2_0()
        {
            OnVisible(KnownProjectTypes.NetCoreWebApplication2_0);

            AssertInvariants();
            AssertCoreChoices();
        }

        [TestMethod]
        public void TestChoicesNone()
        {
            OnVisible(KnownProjectTypes.None);

            AssertInvariants();
            AssertNoneChoices();
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestNextThrows()
        {
            OnVisible(KnownProjectTypes.WebApplication);

            _objectUnderTest.Next();
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestPublishThrows()
        {
            OnVisible(KnownProjectTypes.WebApplication);

            _objectUnderTest.Publish();
        }

        [TestMethod]
        public void TestOnFlowFinished()
        {
            OnVisible(KnownProjectTypes.WebApplication);

            RaiseFlowFinished();

            AssertInvariants();
            AssertInitialState();
        }

        private void OnVisible(KnownProjectTypes projectType)
        {
            _vsProjectMock.SetupGet(p => p.ProjectType).Returns(projectType);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
        }

        private void RaiseFlowFinished()
        {
            Mock<IPublishDialog> publishDialogMock = Mock.Get(_mockedPublishDialog);
            publishDialogMock.Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
        }

        private void AssertInitialState()
        {
            Assert.IsNull(_objectUnderTest.PublishDialog);
            CollectionAssert.AreEquivalent(new List<Choice>(), _objectUnderTest.Choices.ToList());
        }

        private void AssertWebApplicationChoices()
        {
            AssertChoices();

            Assert.IsFalse(_objectUnderTest.Choices.ElementAt(0).Command.CanExecute(null));
            Assert.IsFalse(_objectUnderTest.Choices.ElementAt(1).Command.CanExecute(null));
            Assert.IsTrue(_objectUnderTest.Choices.ElementAt(2).Command.CanExecute(null));
        }

        private void AssertCoreChoices()
        {
            AssertChoices();

            Assert.IsTrue(_objectUnderTest.Choices.ElementAt(0).Command.CanExecute(null));
            Assert.IsTrue(_objectUnderTest.Choices.ElementAt(1).Command.CanExecute(null));
            Assert.IsFalse(_objectUnderTest.Choices.ElementAt(2).Command.CanExecute(null));
        }

        private void AssertNoneChoices()
        {
            AssertChoices();

            Assert.IsFalse(_objectUnderTest.Choices.ElementAt(0).Command.CanExecute(null));
            Assert.IsFalse(_objectUnderTest.Choices.ElementAt(1).Command.CanExecute(null));
            Assert.IsFalse(_objectUnderTest.Choices.ElementAt(2).Command.CanExecute(null));
        }

        private void AssertChoices()
        {
            Assert.AreEqual(3, _objectUnderTest.Choices.Count());

            Assert.AreEqual(Resources.PublishDialogChoiceStepAppEngineFlexName, _objectUnderTest.Choices.ElementAt(0).Name);
            Assert.AreEqual(Resources.PublishDialogChoiceStepAppEngineToolTip, _objectUnderTest.Choices.ElementAt(0).ToolTip);

            Assert.AreEqual(Resources.PublishDialogChoiceStepGkeName, _objectUnderTest.Choices.ElementAt(1).Name);
            Assert.AreEqual(Resources.PublishDialogChoiceStepGkeToolTip, _objectUnderTest.Choices.ElementAt(1).ToolTip);

            Assert.AreEqual(Resources.PublishDialogChoiceStepGceName, _objectUnderTest.Choices.ElementAt(2).Name);
            Assert.AreEqual(Resources.PublishDialogChoiceStepGceToolTip, _objectUnderTest.Choices.ElementAt(2).ToolTip);
        }

        private void AssertInvariants()
        {
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsFalse(_objectUnderTest.CanPublish);
        }
    }
}
