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

using GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace GoogleCloudExtensionUnitTests.TemplateWizards.Dialogs
{
    /// <summary>
    /// Class for testing <see cref="AspNetCoreTemplateChooserViewModel"/>.
    /// </summary>
    [TestClass]
    public class TemplateChooserViewModelBaseTests : ExtensionTestBase
    {
        private Mock<Action> _closeWindowMock;
        private TemplateChooserViewModelBase _objectUnderTest;

        protected override void BeforeEach()
        {
            _closeWindowMock = new Mock<Action>();
            _objectUnderTest = new TestTemplateChooserViewModelBase(_closeWindowMock);
        }

        [TestMethod]
        public void TestInitialConditionsForAspNet()
        {
            const string testProjectId = "test-project-id";
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(testProjectId);

            var objectUnderTest = new TestTemplateChooserViewModelBase(_closeWindowMock);

            Assert.AreEqual(AppType.Mvc, objectUnderTest.AppType);
            Assert.IsTrue(objectUnderTest.OkCommand.CanExecuteCommand);
            Assert.AreEqual(testProjectId, objectUnderTest.GcpProjectId);
            Assert.IsNull(objectUnderTest.Result);
        }

        [TestMethod]
        public void TestSetMvc()
        {
            _objectUnderTest.IsMvc = true;

            Assert.IsTrue(_objectUnderTest.IsMvc);
            Assert.IsFalse(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.Mvc, _objectUnderTest.AppType);
        }

        [TestMethod]
        public void TestChangeToMvc()
        {
            _objectUnderTest.IsWebApi = true;

            _objectUnderTest.IsMvc = true;

            Assert.IsTrue(_objectUnderTest.IsMvc);
            Assert.IsFalse(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.Mvc, _objectUnderTest.AppType);
        }

        [TestMethod]
        public void TestUnsetMvc()
        {
            _objectUnderTest.IsMvc = true;

            _objectUnderTest.IsMvc = false;

            Assert.IsFalse(_objectUnderTest.IsMvc);
            Assert.IsFalse(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.None, _objectUnderTest.AppType);
        }

        [TestMethod]
        public void TestSetWebApi()
        {
            _objectUnderTest.IsWebApi = true;

            Assert.IsFalse(_objectUnderTest.IsMvc);
            Assert.IsTrue(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.WebApi, _objectUnderTest.AppType);
        }

        [TestMethod]
        public void TestChangeToWebApi()
        {
            _objectUnderTest.IsMvc = true;

            _objectUnderTest.IsWebApi = true;

            Assert.IsFalse(_objectUnderTest.IsMvc);
            Assert.IsTrue(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.WebApi, _objectUnderTest.AppType);
        }

        [TestMethod]
        public void TestUnsetWebApi()
        {
            _objectUnderTest.IsWebApi = true;

            _objectUnderTest.IsWebApi = false;

            Assert.IsFalse(_objectUnderTest.IsMvc);
            Assert.IsFalse(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.None, _objectUnderTest.AppType);
        }

        [TestMethod]
        public void TestOkCommand()
        {
            _objectUnderTest.OkCommand.Execute(null);

            _closeWindowMock.Verify(f => f(), Times.Once);
            Assert.IsNotNull(_objectUnderTest.Result);
        }

        private class TestTemplateChooserViewModelBase : TemplateChooserViewModelBase
        {
            public TestTemplateChooserViewModelBase(IMock<Action> closeWindow) : base(closeWindow.Object)
            {
            }

            public override FrameworkType GetSelectedFramework()
            {
                return default(FrameworkType);
            }

            public override AspNetVersion GetSelectedVersion()
            {
                return default(AspNetVersion);
            }
        }
    }
}
