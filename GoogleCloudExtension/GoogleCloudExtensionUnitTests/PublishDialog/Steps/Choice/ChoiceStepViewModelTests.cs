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
using GoogleCloudExtension.Deployment.UnitTests;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialog.Steps.Choice;
using GoogleCloudExtension.PublishDialog.Steps.Flex;
using GoogleCloudExtension.PublishDialog.Steps.Gce;
using GoogleCloudExtension.PublishDialog.Steps.Gke;
using GoogleCloudExtension.UserPrompt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
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

        protected override void BeforeEach()
        {

            _parsedProject = new FakeParsedProject { Name = VisualStudioProjectName };
            _mockedPublishDialog = Mock.Of<IPublishDialog>(pd => pd.Project == _parsedProject);

            _objectUnderTest = new ChoiceStepViewModel(_mockedPublishDialog);
        }

        protected override void AfterEach()
        {
            Mock.Get(_mockedPublishDialog).Raise(d => d.FlowFinished += null, EventArgs.Empty);
        }

        [TestMethod]
        public void TestInitialState()
        {
            Assert.IsFalse(_objectUnderTest.PublishCommand.CanExecuteCommand);
            CollectionAssert.That.IsEmpty(_objectUnderTest.Choices);
        }

        [TestMethod]
        public async Task TestChoices_WebApplication()
        {
            _parsedProject.ProjectType = KnownProjectTypes.WebApplication;
            await _objectUnderTest.OnVisibleAsync();

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
        public async Task TestChoices_DotnetCore()
        {
            _parsedProject.ProjectType = KnownProjectTypes.NetCoreWebApplication1_0;
            await _objectUnderTest.OnVisibleAsync();

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
        public async Task TestChoices_None()
        {
            _parsedProject.ProjectType = KnownProjectTypes.None;
            await _objectUnderTest.OnVisibleAsync();

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
        public async Task TestOnFlowFinished_ResetsChoicesAndPublishDialog()
        {
            _parsedProject.ProjectType = KnownProjectTypes.WebApplication;
            await _objectUnderTest.OnVisibleAsync();
            _objectUnderTest.Choices = null;

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            CollectionAssert.That.IsEmpty(_objectUnderTest.Choices);
        }

        [TestMethod]
        public async Task TestOnFlowFinished_RemovesHandlers()
        {
            _parsedProject.ProjectType = KnownProjectTypes.WebApplication;
            await _objectUnderTest.OnVisibleAsync();
            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
            _objectUnderTest.Choices = null;

            Mock.Get(_mockedPublishDialog).Raise(dg => dg.FlowFinished += null, EventArgs.Empty);

            Assert.IsNull(_objectUnderTest.Choices);
        }

        [TestMethod]
        public async Task TestOnAppEngineChoiceCommand()
        {
            _parsedProject.ProjectType = KnownProjectTypes.NetCoreWebApplication1_0;
            await _objectUnderTest.OnVisibleAsync();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepAppEngineFlexName).Command
                .Execute(null);

            Mock.Get(_mockedPublishDialog).Verify(pd => pd.NavigateToStep(It.IsAny<FlexStepContent>()));
        }

        [TestMethod]
        public async Task TestOnGkeChoiceCommand()

        {
            _parsedProject.ProjectType = KnownProjectTypes.NetCoreWebApplication1_0;
            await _objectUnderTest.OnVisibleAsync();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGkeName).Command
                .Execute(null);

            Mock.Get(_mockedPublishDialog).Verify(pd => pd.NavigateToStep(It.IsAny<GkeStepContent>()));
        }

        [TestMethod]
        public async Task TestOnGceChoiceCommand()

        {
            _parsedProject.ProjectType = KnownProjectTypes.WebApplication;
            await _objectUnderTest.OnVisibleAsync();

            _objectUnderTest.Choices.Single(c => c.Name == Resources.PublishDialogChoiceStepGceName).Command
                .Execute(null);

            Mock.Get(_mockedPublishDialog).Verify(pd => pd.NavigateToStep(It.IsAny<GceStepContent>()));
        }
    }
}
