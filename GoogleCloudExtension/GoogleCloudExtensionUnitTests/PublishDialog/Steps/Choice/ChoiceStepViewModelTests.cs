﻿// Copyright 2017 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps;
using GoogleCloudExtension.PublishDialog.Steps.Choice;
using GoogleCloudExtension.PublishDialog.Steps.CoreGceWarning;
using GoogleCloudExtension.PublishDialog.Steps.Flex;
using GoogleCloudExtension.PublishDialog.Steps.Gce;
using GoogleCloudExtension.PublishDialog.Steps.Gke;
using GoogleCloudExtension.Services.VsProject;
using GoogleCloudExtensionUnitTests.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.PublishDialog.Steps.Choice
{
    [TestClass]
    public class ChoiceStepViewModelTests : ExtensionTestBase
    {
        private const string VisualStudioProjectName = "VisualStudioProjectName";

        private ChoiceStepViewModel _objectUnderTest;
        private FakeParsedProject _parsedProject;
        private Mock<IVsProjectPropertyService> _propertyServiceMock;
        private Mock<IPublishDialog> _publishDialogMock;

        protected override void BeforeEach()
        {
            _parsedProject = new FakeParsedProject
            {
                Name = VisualStudioProjectName,
                ProjectType = KnownProjectTypes.WebApplication
            };
            _publishDialogMock = new Mock<IPublishDialog>();
            _publishDialogMock.Setup(pd => pd.Project).Returns(_parsedProject);

            _propertyServiceMock = new Mock<IVsProjectPropertyService>();
            PackageMock.Setup(p => p.GetMefService<IVsProjectPropertyService>()).Returns(_propertyServiceMock.Object);

            _objectUnderTest = new ChoiceStepViewModel(_publishDialogMock.Object);
        }

        [TestMethod]
        public void TestInitialState()
        {
            Assert.IsFalse(_objectUnderTest.ActionCommand.CanExecuteCommand);
            CollectionAssert.That.IsEmpty(_objectUnderTest.Choices);
            Assert.AreEqual(Resources.PublishDialogPublishButtonCaption, _objectUnderTest.ActionCaption);
        }

        [TestMethod]
        public void TestChoices_WebApplication()
        {
            _parsedProject.ProjectType = KnownProjectTypes.WebApplication;
            _objectUnderTest.OnVisible();

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
        public void TestChoices_DotnetCore()
        {
            _parsedProject.ProjectType = KnownProjectTypes.NetCoreWebApplication;
            _objectUnderTest.OnVisible();

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
                        true)
                },
                _objectUnderTest.Choices.Select(c => Tuple.Create(c.Name, c.ToolTip, c.Command.CanExecute(null)))
                    .ToList());
        }

        [TestMethod]
        public void TestChoices_None()
        {
            _parsedProject.ProjectType = KnownProjectTypes.None;
            _objectUnderTest.OnVisible();

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
        public void TestPublishCommand_Throws()
        {
            _objectUnderTest.ActionCommand.Execute(null);

            PackageMock.Verify(p => p.UserPromptService.ExceptionPrompt(It.IsAny<NotSupportedException>()));
        }

        [TestMethod]
        public void TestOnFlowFinished_RemovesProperty()
        {
            _objectUnderTest.OnVisible();

            _publishDialogMock.Raise(pd => pd.FlowFinished += null, EventArgs.Empty);

            _propertyServiceMock.Verify(
                s => s.DeleteUserProperty(
                    _parsedProject.Project, ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName));
        }

        [TestMethod]
        public void TestOnFlowFinished_RemovesHandlers()
        {
            _objectUnderTest.OnVisible();
            _publishDialogMock.Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
            _propertyServiceMock.ResetCalls();

            _publishDialogMock.Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            _propertyServiceMock.Verify(
                s => s.DeleteUserProperty(
                    _parsedProject.Project, ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName), Times.Never);
        }

        [TestMethod]
        public void TestOnNotVisible_RemovesHandlers()
        {
            _objectUnderTest.OnVisible();
            _objectUnderTest.OnNotVisible();
            _propertyServiceMock.ResetCalls();

            _publishDialogMock.Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            _propertyServiceMock.Verify(
                s => s.DeleteUserProperty(It.IsAny<Project>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestOnAppEngineChoiceCommand_PushesFlexStep()
        {
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepAppEngineFlexName).Command
                .Execute(null);

            _publishDialogMock.Verify(pd => pd.NavigateToStep(It.IsAny<FlexStepContent>()));
        }

        [TestMethod]
        public void TestOnAppEngineChoiceCommand_SavesProjectProperty()
        {
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepAppEngineFlexName).Command
                .Execute(null);

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName,
                    ChoiceType.Gae.ToString()));
        }

        [TestMethod]
        public void TestOnGkeChoiceCommand_PushesGkeStep()

        {
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGkeName).Command
                .Execute(null);

            _publishDialogMock.Verify(pd => pd.NavigateToStep(It.IsAny<GkeStepContent>()));
        }

        [TestMethod]
        public void TestOnGkeChoiceCommand_SavesProjectProperty()
        {
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGkeName).Command
                .Execute(null);

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName,
                    ChoiceType.Gke.ToString()));
        }

        [TestMethod]
        public void TestOnGceChoiceCommand_FrameworkProjectPushesGceStep()
        {
            _parsedProject.ProjectType = KnownProjectTypes.WebApplication;
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGceName).Command
                .Execute(null);

            _publishDialogMock.Verify(pd => pd.NavigateToStep(It.IsAny<GceStepContent>()));
        }

        [TestMethod]
        public void TestOnGceChoiceCommand_FrameworkProjectSavesProjectProperty()
        {
            _parsedProject.ProjectType = KnownProjectTypes.WebApplication;
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGceName).Command
                .Execute(null);

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project, ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName,
                    ChoiceType.Gce.ToString()));
        }

        [TestMethod]
        public void TestOnGceChoiceCommand_CoreProjectPushesCoreGceWarningStep()
        {
            _parsedProject.ProjectType = KnownProjectTypes.NetCoreWebApplication;
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGceName)
                .Command.Execute(null);

            _publishDialogMock.Verify(pd => pd.NavigateToStep(It.IsAny<CoreGceWarningStepContent>()));
        }

        [TestMethod]
        public void TestOnGceChoiceCommand_CoreProjectDoesNotSaveProjectProperty()
        {
            _parsedProject.ProjectType = KnownProjectTypes.NetCoreWebApplication;
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGceName)
                .Command
                .Execute(null);

            _propertyServiceMock.Verify(
                s => s.SaveUserProperty(
                    _parsedProject.Project,
                    ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName,
                    ChoiceType.Gce.ToString()), Times.Never);
        }

        [TestMethod]
        public void TestExecutePreviousChoice_DoesNothingForNoPreviousChoice()
        {
            _objectUnderTest.OnVisible();

            _publishDialogMock.Verify(
                pd => pd.NavigateToStep(It.IsAny<IStepContent<IPublishDialogStep>>()), Times.Never);
        }

        [TestMethod]
        public void TestExecutePreviousChoice_DoesNothingForInvalidPreviousChoice()
        {
            _propertyServiceMock.Setup(
                    s => s.GetUserProperty(
                        _parsedProject.Project, ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName))
                .Returns("InvalidChoice");

            _objectUnderTest.OnVisible();

            _publishDialogMock.Verify(
                pd => pd.NavigateToStep(It.IsAny<IStepContent<IPublishDialogStep>>()), Times.Never);
        }

        [TestMethod]
        public void TestExecutePreviousChoice_DoesNothingForPreviousChoiceNone()
        {
            _propertyServiceMock.Setup(
                    s => s.GetUserProperty(
                        _parsedProject.Project, ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName))
                .Returns(ChoiceType.None.ToString());

            _objectUnderTest.OnVisible();

            _publishDialogMock.Verify(
                pd => pd.NavigateToStep(It.IsAny<IStepContent<IPublishDialogStep>>()), Times.Never);
        }

        [TestMethod]
        public void TestExecutePreviousChoice_ExecutesPreviousValidChoice()
        {
            _propertyServiceMock.Setup(
                    s => s.GetUserProperty(
                        _parsedProject.Project, ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName))
                .Returns(ChoiceType.Gke.ToString());

            _objectUnderTest.OnVisible();

            _publishDialogMock.Verify(pd => pd.NavigateToStep(It.IsAny<GkeStepContent>()));
        }

        [TestMethod]
        public void TestOnVisible_CommingFromStepSkipsExecutePreviousValidChoice()
        {
            _propertyServiceMock.Setup(
                    s => s.GetUserProperty(
                        _parsedProject.Project,
                        ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName))
                .Returns(ChoiceType.Gke.ToString());

            _objectUnderTest.OnVisible(Mock.Of<IPublishDialogStep>());

            _publishDialogMock.Verify(pd => pd.NavigateToStep(It.IsAny<GkeStepContent>()), Times.Never);
        }
    }
}
