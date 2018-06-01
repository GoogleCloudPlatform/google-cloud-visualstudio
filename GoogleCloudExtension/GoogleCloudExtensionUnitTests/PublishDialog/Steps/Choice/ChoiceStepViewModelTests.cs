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
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps;
using GoogleCloudExtension.PublishDialog.Steps.Choice;
using GoogleCloudExtension.PublishDialog.Steps.Flex;
using GoogleCloudExtension.PublishDialog.Steps.Gce;
using GoogleCloudExtension.PublishDialog.Steps.Gke;
using GoogleCloudExtension.UserPrompt;
using GoogleCloudExtensionUnitTests.Projects;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.PublishDialog.Steps.Choice
{
    [TestClass]
    public class ChoiceStepViewModelTests : ExtensionTestBase
    {
        private const string VisualStudioProjectName = "VisualStudioProjectName";

        private ChoiceStepViewModel _objectUnderTest;
        private IPublishDialog _mockedPublishDialog;
        private FakeParsedProject _parsedProject;
        private Mock<IVsBuildPropertyStorage> _vsPropertyStoreMock;

        [SuppressMessage("ReSharper", "RedundantAssignment")]
        protected override void BeforeEach()
        {
            _parsedProject = new FakeParsedProject { Name = VisualStudioProjectName };
            _parsedProject.ProjectType = KnownProjectTypes.WebApplication;
            _mockedPublishDialog = Mock.Of<IPublishDialog>(pd => pd.Project == _parsedProject);

            var vsHierarchyMock = new Mock<IVsHierarchy>();
            _vsPropertyStoreMock = vsHierarchyMock.As<IVsBuildPropertyStorage>();
            IVsHierarchy vsProject = vsHierarchyMock.Object;
            PackageMock.Setup(
                    p => p.GetService<IVsSolution>().GetProjectOfUniqueName(It.IsAny<string>(), out vsProject))
                .Returns(VSConstants.S_OK);

            _objectUnderTest = new ChoiceStepViewModel(_mockedPublishDialog);
        }

        [TestMethod]
        public void TestInitialState()
        {
            Assert.IsFalse(_objectUnderTest.PublishCommand.CanExecuteCommand);
            CollectionAssert.That.IsEmpty(_objectUnderTest.Choices);
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
            _parsedProject.ProjectType = KnownProjectTypes.NetCoreWebApplication1_0;
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
                        false)
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
            _objectUnderTest.PublishCommand.Execute(null);

            PromptUserMock.Verify(p => p(It.IsAny<UserPromptWindow.Options>()));
        }

        [TestMethod]
        public void TestOnFlowFinished_RemovesProperty()
        {
            _objectUnderTest.OnVisible();

            Mock.Get(_mockedPublishDialog).Raise(pd => pd.FlowFinished += null, EventArgs.Empty);

            _vsPropertyStoreMock.Verify(
                s => s.RemoveProperty(
                    ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName, null,
                    ParsedDteProjectExtensions.UserFileFlag));
        }

        [TestMethod]
        public void TestOnFlowFinished_RemovesHandlers()
        {
            _objectUnderTest.OnVisible();
            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
            _vsPropertyStoreMock.ResetCalls();

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            _vsPropertyStoreMock.Verify(
                s => s.RemoveProperty(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<uint>()), Times.Never);
        }

        [TestMethod]
        public void TestOnNotVisible_RemovesHandlers()
        {
            _objectUnderTest.OnVisible();
            _objectUnderTest.OnNotVisible();
            _vsPropertyStoreMock.ResetCalls();

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            _vsPropertyStoreMock.Verify(
                s => s.RemoveProperty(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<uint>()), Times.Never);
        }

        [TestMethod]
        public void TestOnAppEngineChoiceCommand_PushesFlexStep()
        {
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepAppEngineFlexName).Command
                .Execute(null);

            Mock.Get(_mockedPublishDialog).Verify(pd => pd.NavigateToStep(It.IsAny<FlexStepContent>()));
        }

        [TestMethod]
        public void TestOnAppEngineChoiceCommand_SavesProjectProperty()
        {
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepAppEngineFlexName).Command
                .Execute(null);

            _vsPropertyStoreMock.Verify(
                s => s.SetPropertyValue(
                    ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName, null,
                    ParsedDteProjectExtensions.UserFileFlag, ChoiceStepViewModel.GaeChoiceId));
        }

        [TestMethod]
        public void TestOnGkeChoiceCommand_PushesGkeStep()

        {
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGkeName).Command
                .Execute(null);

            Mock.Get(_mockedPublishDialog).Verify(pd => pd.NavigateToStep(It.IsAny<GkeStepContent>()));
        }

        [TestMethod]
        public void TestOnGkeChoiceCommand_SavesProjectProperty()

        {
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGkeName).Command
                .Execute(null);

            _vsPropertyStoreMock.Verify(
                s => s.SetPropertyValue(
                    ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName, null,
                    ParsedDteProjectExtensions.UserFileFlag, ChoiceStepViewModel.GkeChoiceId));
        }

        [TestMethod]
        public void TestOnGceChoiceCommand_PushesGceStep()

        {
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGceName).Command
                .Execute(null);

            Mock.Get(_mockedPublishDialog).Verify(pd => pd.NavigateToStep(It.IsAny<GceStepContent>()));
        }

        [TestMethod]
        public void TestOnGceChoiceCommand_SavesProjectProperty()

        {
            _objectUnderTest.OnVisible();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGceName).Command
                .Execute(null);

            _vsPropertyStoreMock.Verify(
                s => s.SetPropertyValue(
                    ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName, null,
                    ParsedDteProjectExtensions.UserFileFlag, ChoiceStepViewModel.GceChoiceId));
        }

        [TestMethod]
        public void TestExecutePreviousChoice_DoesNothingForNoPreviousChoice()
        {
            _objectUnderTest.OnVisible();
            _objectUnderTest.ExecutePreviousChoice();

            Mock.Get(_mockedPublishDialog).Verify(
                pd => pd.NavigateToStep(It.IsAny<IStepContent<IPublishDialogStep>>()), Times.Never);
        }

        [TestMethod]
        public void TestExecutePreviousChoice_DoesNothingForInvalidPreviousChoice()
        {
            // ReSharper disable once RedundantAssignment
            var invalidChoiceId = "InvalidChoice";
            _vsPropertyStoreMock.Setup(
                s => s.GetPropertyValue(
                    ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName, null,
                    ParsedDteProjectExtensions.UserFileFlag, out invalidChoiceId)).Returns(VSConstants.S_OK);

            _objectUnderTest.OnVisible();
            _objectUnderTest.ExecutePreviousChoice();

            Mock.Get(_mockedPublishDialog).Verify(
                pd => pd.NavigateToStep(It.IsAny<IStepContent<IPublishDialogStep>>()), Times.Never);
        }

        [TestMethod]
        public void TestExecutePreviousChoice_ExecutesPreviousValidChoice()
        {
            // ReSharper disable once RedundantAssignment
            string gkeChoiceId = ChoiceStepViewModel.GkeChoiceId;
            _vsPropertyStoreMock.Setup(
                s => s.GetPropertyValue(
                    ChoiceStepViewModel.GoogleCloudPublishChoicePropertyName, null,
                    ParsedDteProjectExtensions.UserFileFlag, out gkeChoiceId)).Returns(VSConstants.S_OK);

            _objectUnderTest.OnVisible();
            _objectUnderTest.ExecutePreviousChoice();

            Mock.Get(_mockedPublishDialog).Verify(pd => pd.NavigateToStep(It.IsAny<GkeStepContent>()));
        }
    }
}
