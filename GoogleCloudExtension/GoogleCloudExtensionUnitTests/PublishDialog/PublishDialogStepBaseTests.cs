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

namespace GoogleCloudExtensionUnitTests.PublishDialog
{
    [TestClass]
    public class PublishDialogStepBaseTests
    {
        private const string TargetProjectId = "TargetProjectId";
        private const string VisualStudioProjectName = "VisualStudioProjectName";

        private PublishDialogStepBase _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            var mockImpl = new Mock<PublishDialogStepBase> { CallBase = true };
            _objectUnderTest = mockImpl.Object;
            var mockedProject = Mock.Of<IParsedProject>(p => p.Name == VisualStudioProjectName);
            var mockedPublishDialog = Mock.Of<IPublishDialog>(d => d.Project == mockedProject);
            _objectUnderTest.OnPushedToDialog(mockedPublishDialog);
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
    }
}
