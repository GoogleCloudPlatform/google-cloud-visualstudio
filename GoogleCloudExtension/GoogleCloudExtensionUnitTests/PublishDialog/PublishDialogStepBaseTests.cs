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
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
using GoogleCloudExtension.PublishDialogSteps.FlexStep;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace GoogleCloudExtensionUnitTests.PublishDialog
{
    [TestClass]
    public class PublishDialogStepBaseTests
    {
        private const string TargetProjectId = "TargetProjectId";
        private const string VisualStudioProjectName = "VisualStudioProjectName";
        private const string DefaultProjectId = "DefaultProjectId";

        private static readonly Project s_targetProject = new Project { ProjectId = TargetProjectId };
        private static readonly Project s_defaultProject = new Project { ProjectId = DefaultProjectId };

        private PublishDialogStepBase _objectUnderTest;
        private Mock<Func<Project>> _pickProjectPromptMock;
        private List<string> _changedProperties;

        [TestInitialize]
        public void BeforeEach()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            var mockImpl = new Mock<PublishDialogStepBase> { CallBase = true };
            _objectUnderTest = mockImpl.Object;
            var mockedProject = Mock.Of<IParsedProject>(p => p.Name == VisualStudioProjectName);
            var mockedPublishDialog = Mock.Of<IPublishDialog>(d => d.Project == mockedProject);
            _objectUnderTest.OnPushedToDialog(mockedPublishDialog);
            _pickProjectPromptMock = new Mock<Func<Project>>();
            _objectUnderTest.PickProjectPrompt = _pickProjectPromptMock.Object;
            _changedProperties = new List<string>();
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
        }

        [TestMethod]
        public void TestSelectProjectCommandCanceled()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _changedProperties.Clear();
            _pickProjectPromptMock.Setup(f => f()).Returns((Project) null);

            _objectUnderTest.SelectProjectCommand.Execute(null);

            CollectionAssert.DoesNotContain(_changedProperties, nameof(FlexStepViewModel.GcpProjectId));
            Assert.AreEqual(DefaultProjectId, _objectUnderTest.GcpProjectId);
            _pickProjectPromptMock.Verify(f => f(), Times.Once);
        }

        [TestMethod]
        public void TestSelectProjectCommand()
        {
            _pickProjectPromptMock.Setup(f => f()).Returns(s_targetProject);

            _objectUnderTest.SelectProjectCommand.Execute(null);

            CollectionAssert.Contains(_changedProperties, nameof(FlexStepViewModel.GcpProjectId));
            Assert.AreEqual(TargetProjectId, _objectUnderTest.GcpProjectId);
            _pickProjectPromptMock.Verify(f => f(), Times.Once);
        }

        [TestMethod]
        public void TestGcpProjectIdWithProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            Assert.AreEqual(TargetProjectId, _objectUnderTest.GcpProjectId);
        }

        [TestMethod]
        public void TestGcpProjectIdWithNoProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);

            Assert.IsNull(_objectUnderTest.GcpProjectId);
        }

        [TestMethod]
        public void TestOnPushedToDialog()
        {
            var dialogMock = new Mock<IPublishDialog>();

            _objectUnderTest.OnPushedToDialog(dialogMock.Object);

            Assert.AreEqual(dialogMock.Object, _objectUnderTest.PublishDialog);
        }
    }
}
