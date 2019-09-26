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

using System;
using GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.TemplateWizards.Dialogs
{
    [TestClass]
    public class AspNetTemplateChooserViewModelTests : ExtensionTestBase
    {
        private Mock<Action> _closeWindowMock;

        [TestInitialize]
        public void BeforeEach() => _closeWindowMock = new Mock<Action>();

        [TestMethod]
        public void TestCreateResult()
        {
            const string resultProjectId = "result-project-id";

            var objectUnderTest = new AspNetTemplateChooserViewModel(_closeWindowMock.Object);
            objectUnderTest.GcpProjectId = resultProjectId;
            objectUnderTest.IsWebApi = true;

            objectUnderTest.OkCommand.Execute(null);

            Assert.IsNotNull(objectUnderTest.Result);
            Assert.AreEqual(resultProjectId, objectUnderTest.Result.GcpProjectId);
            Assert.AreEqual(FrameworkType.NetFramework, objectUnderTest.Result.SelectedFramework);
            Assert.AreEqual(AspNetVersion.AspNet4, objectUnderTest.Result.SelectedVersion);
            Assert.AreEqual(AppType.WebApi, objectUnderTest.Result.AppType);
        }
    }
}
