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
using GoogleCloudExtension.PublishDialogSteps.FlexStep;
using GoogleCloudExtension.PublishDialogSteps.GceStep;
using GoogleCloudExtension.PublishDialogSteps.GkeStep;
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

            var publishDialogMock = new Mock<IPublishDialog>();
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
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsNull(_objectUnderTest.PublishDialog);
            CollectionAssert.AreEquivalent(new List<Choice>(), _objectUnderTest.Choices.ToList());
            Assert.IsInstanceOfType(_objectUnderTest.Content, typeof(ChoiceStepContent));
        }

        [TestMethod]
        public void TestChoicesWebApplication()
        {
            _vsProjectMock.SetupGet(p => p.ProjectType).Returns(KnownProjectTypes.WebApplication);
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            CollectionAssert.AreEqual(
                new[]
                {
                    Tuple.Create(
                        Resources.PublishDialogChoiceStepAppEngineFlexName,
                        Resources.PublishDialogChoiceStepAppEngineToolTip,
                        false),
                    Tuple.Create(
                        Resources.PublishDialogChoiceStepGkeName,
                        Resources.PublishDialogChoiceStepGkeToolTip,
                        false),
                    Tuple.Create(
                        Resources.PublishDialogChoiceStepGceName,
                        Resources.PublishDialogChoiceStepGceToolTip,
                        true)
                },
                _objectUnderTest.Choices.Select(c => Tuple.Create(c.Name, c.ToolTip, c.Command.CanExecute(null)))
                    .ToList());
        }

        [TestMethod]
        public void TestChoicesDotnetCore()
        {
            _vsProjectMock.SetupGet(p => p.ProjectType).Returns(KnownProjectTypes.NetCoreWebApplication1_0);
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            CollectionAssert.AreEqual(
                new[]
                {
                    Tuple.Create(
                        Resources.PublishDialogChoiceStepAppEngineFlexName,
                        Resources.PublishDialogChoiceStepAppEngineToolTip,
                        true),
                    Tuple.Create(
                        Resources.PublishDialogChoiceStepGkeName,
                        Resources.PublishDialogChoiceStepGkeToolTip,
                        true),
                    Tuple.Create(
                        Resources.PublishDialogChoiceStepGceName,
                        Resources.PublishDialogChoiceStepGceToolTip,
                        false)
                },
                _objectUnderTest.Choices.Select(c => Tuple.Create(c.Name, c.ToolTip, c.Command.CanExecute(null)))
                    .ToList());
        }

        [TestMethod]
        public void TestChoicesNone()
        {
            _vsProjectMock.SetupGet(p => p.ProjectType).Returns(KnownProjectTypes.None);
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            CollectionAssert.AreEqual(
                new[]
                {
                    Tuple.Create(
                        Resources.PublishDialogChoiceStepAppEngineFlexName,
                        Resources.PublishDialogChoiceStepAppEngineToolTip,
                        false),
                    Tuple.Create(
                        Resources.PublishDialogChoiceStepGkeName,
                        Resources.PublishDialogChoiceStepGkeToolTip,
                        false),
                    Tuple.Create(
                        Resources.PublishDialogChoiceStepGceName,
                        Resources.PublishDialogChoiceStepGceToolTip,
                        false)
                },
                _objectUnderTest.Choices.Select(c => Tuple.Create(c.Name, c.ToolTip, c.Command.CanExecute(null)))
                    .ToList());
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestNextThrows()
        {
            _objectUnderTest.Next();
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestPublishThrows()
        {
            _objectUnderTest.Publish();
        }

        [TestMethod]
        public void TestOnFlowFinishedResetsChoicesAndPublishDialog()
        {
            _vsProjectMock.SetupGet(p => p.ProjectType).Returns(KnownProjectTypes.WebApplication);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            _objectUnderTest.Choices = null;

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            Assert.IsNull(_objectUnderTest.PublishDialog);
            // ReSharper disable once AssignNullToNotNullAttribute
            CollectionAssert.AreEqual(new List<Choice>(), _objectUnderTest.Choices.ToList());
        }

        [TestMethod]
        public void TestOnFlowFinishedRemovesHandlers()
        {
            _vsProjectMock.SetupGet(p => p.ProjectType).Returns(KnownProjectTypes.WebApplication);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
            _objectUnderTest.Choices = null;

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            Assert.IsNull(_objectUnderTest.Choices);
        }

        [TestMethod]
        public void TestOnAppEngineChoiceCommand()

        {
            _vsProjectMock.SetupGet(p => p.ProjectType).Returns(KnownProjectTypes.None);
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepAppEngineFlexName).Command.Execute(null);

            Mock.Get(_mockedPublishDialog).Verify(pd => pd.NavigateToStep(It.IsAny<FlexStepViewModel>()));
        }

        [TestMethod]
        public void TestOnGkeChoiceCommand()

        {
            _vsProjectMock.SetupGet(p => p.ProjectType).Returns(KnownProjectTypes.None);
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGkeName).Command
                .Execute(null);

            Mock.Get(_mockedPublishDialog).Verify(pd => pd.NavigateToStep(It.IsAny<GkeStepViewModel>()));
        }

        [TestMethod]
        public void TestOnGceChoiceCommand()

        {
            _vsProjectMock.SetupGet(p => p.ProjectType).Returns(KnownProjectTypes.None);
            _objectUnderTest.OnVisible(_mockedPublishDialog);

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGceName).Command
                .Execute(null);

            Mock.Get(_mockedPublishDialog).Verify(pd => pd.NavigateToStep(It.IsAny<GceStepViewModel>()));
        }
    }
}
