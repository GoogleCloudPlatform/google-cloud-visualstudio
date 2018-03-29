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
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.PublishDialog;
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

        private Mock<PublishDialogStepBase> _objectUnderTestImpl;
        private PublishDialogStepBase _objectUnderTest;
        private IPublishDialog _mockedPublishDialog;
        private Mock<Func<Project>> _pickProjectPromptMock;
        private List<string> _changedProperties;

        [TestInitialize]
        public void BeforeEach()
        {
            var mockedProject = Mock.Of<IParsedProject>(p => p.Name == VisualStudioProjectName);
            _mockedPublishDialog = Mock.Of<IPublishDialog>(d => d.Project == mockedProject);

            _pickProjectPromptMock = new Mock<Func<Project>>();
            _changedProperties = new List<string>();

            _objectUnderTestImpl = new Mock<PublishDialogStepBase> { CallBase = true };
            _objectUnderTest = _objectUnderTestImpl.Object;
            _objectUnderTest.PickProjectPrompt = _pickProjectPromptMock.Object;
            _objectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
        }

        [TestCleanup]
        public void AfterEach()
        {
            _objectUnderTest.OnFlowFinished();
        }

        [TestMethod]
        public void TestInitialStateProjectSelected()
        {
            Assert.IsNull(_objectUnderTest.PublishDialog);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.GeneralError);

            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            CollectionAssert.DoesNotContain(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
        }

        [TestMethod]
        public void TestOnVisibleNoGCPProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);

            _objectUnderTest.OnVisible(_mockedPublishDialog);

            Assert.AreEqual(_mockedPublishDialog, _objectUnderTest.PublishDialog);
            AssertChangedToNoProject();
        }

        [TestMethod]
        public void TestOnVisibleWithGCPProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);

            _objectUnderTest.OnVisible(_mockedPublishDialog);

            Assert.AreEqual(_mockedPublishDialog, _objectUnderTest.PublishDialog);
            AssertChangedToTargetProject();
        }

        [TestMethod]
        public void TestFromNoProjectToNoProjectExternal() =>
            TestExternalProjectTransitions(InitWithNoProject, null, AssertUnchangedNoProject);

        [TestMethod]
        public void TestFromProjectToNoProjectExternal() =>
            TestExternalProjectTransitions(InitWithDefaultProject, null, AssertChangedToNoProject);

        [TestMethod]
        public void TestFromNoProjectToProjectExternal() =>
            TestExternalProjectTransitions(InitWithNoProject, s_targetProject, AssertChangedToTargetProject);

        [TestMethod]
        public void TestFromProjectToProjectExternal() =>
            TestExternalProjectTransitions(InitWithDefaultProject, s_targetProject, AssertChangedToTargetProject);

        private void TestExternalProjectTransitions(Action setupInitialState, Project transitionTo, Action assertFinalState)
        {
            setupInitialState();
            CredentialsStore.Default.UpdateCurrentProject(transitionTo);
            assertFinalState();
        }

        [TestMethod]
        public void TestFromNoProjectToNoProjectSelect() =>
            TestSelectCommandProjectTransitions(InitWithNoProject, null, AssertUnchangedNoProject);

        [TestMethod]
        public void TestFromProjectToNoProjectSelect() =>
            TestSelectCommandProjectTransitions(InitWithDefaultProject, null, AssertUnchangedDefaultProject);

        [TestMethod]
        public void TestFromNoProjectToProjectSelect() =>
            TestSelectCommandProjectTransitions(InitWithNoProject, s_targetProject, AssertChangedToTargetProject);

        [TestMethod]
        public void TestFromProjectToProjectSelect() =>
            TestSelectCommandProjectTransitions(InitWithDefaultProject, s_targetProject, AssertChangedToTargetProject);

        private void TestSelectCommandProjectTransitions(Action setupInitialState, Project transitionTo, Action assertFinalState)
        {
            setupInitialState();
            _pickProjectPromptMock.Setup(f => f()).Returns(transitionTo);
            _objectUnderTest.SelectProjectCommand.Execute(null);
            assertFinalState();
        }

        [TestMethod]
        public void TestOnFlowFinished()
        {
            var publishDialogMock = Mock.Get(_mockedPublishDialog);
            InitWithDefaultProject();

            publishDialogMock.Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
            _objectUnderTestImpl.Verify(m => m.OnFlowFinished(), Times.Once());

            Assert.IsNull(_objectUnderTest.PublishDialog);
            Assert.IsFalse(_objectUnderTest.CanGoNext);
            Assert.IsFalse(_objectUnderTest.CanPublish);
            Assert.IsFalse(_objectUnderTest.LoadingProject);
            Assert.IsFalse(_objectUnderTest.GeneralError);

            CredentialsStore.Default.UpdateCurrentProject(s_targetProject);
            CollectionAssert.DoesNotContain(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));


            publishDialogMock.Raise(dg => dg.FlowFinished += null, EventArgs.Empty);
            _objectUnderTestImpl.Verify(m => m.OnFlowFinished(), Times.Once());
        }

        private void InitWithNoProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            _changedProperties.Clear();
        }

        private void InitWithDefaultProject()
        {
            CredentialsStore.Default.UpdateCurrentProject(s_defaultProject);
            _objectUnderTest.OnVisible(_mockedPublishDialog);
            _changedProperties.Clear();
        }

        private void AssertUnchangedNoProject()
        {
            CollectionAssert.DoesNotContain(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
            Assert.IsNull(_objectUnderTest.GcpProjectId);
        }

        private void AssertUnchangedDefaultProject()
        {
            CollectionAssert.DoesNotContain(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
            Assert.AreEqual(DefaultProjectId, _objectUnderTest.GcpProjectId);
        }

        private void AssertChangedToTargetProject()
        {
            CollectionAssert.Contains(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
            Assert.AreEqual(TargetProjectId, _objectUnderTest.GcpProjectId);
        }

        private void AssertChangedToNoProject()
        {
            Assert.IsNull(_objectUnderTest.GcpProjectId);
            CollectionAssert.Contains(_changedProperties, nameof(PublishDialogStepBase.GcpProjectId));
        }
    }
}
