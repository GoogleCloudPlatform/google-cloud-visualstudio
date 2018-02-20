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
using GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace GoogleCloudExtensionUnitTests.TemplateWizards.Dialogs
{
    /// <summary>
    /// Class for testing <see cref="AspNetCoreTemplateChooserViewModel"/>.
    /// </summary>
    [TestClass]
    public class TemplateChooserViewModelBaseTests
    {
        private const string DefaultProjectId = "default-project-id";
        private Mock<Action> _closeWindowMock;
        private TemplateChooserViewModelBase _objectUnderTest;

        private class TestTemplateChooserViewModelBase : TemplateChooserViewModelBase
        {

            public TestTemplateChooserViewModelBase(Mock<Action> closeWindow) : base(closeWindow.Object)
            {
            }

            protected override TemplateChooserViewModelResult CreateResult()
            {
                return null;
            }
        }

        [TestInitialize]
        public void BeforeEach()
        {
            CredentialsStore.Default.UpdateCurrentProject(Mock.Of<Project>(p => p.ProjectId == DefaultProjectId));
            _closeWindowMock = new Mock<Action>();
            GoogleCloudExtensionPackageTests.InitPackageMock(
                dteMock => dteMock.Setup(dte => dte.Version).Returns(VsVersionUtils.VisualStudio2017Version));
        }

        [TestMethod]
        public void TestInitialConditionsForAspNet()
        {
            _objectUnderTest = new TestTemplateChooserViewModelBase(_closeWindowMock);

            Assert.AreEqual(AppType.Mvc, _objectUnderTest.AppType);
            Assert.AreEqual(true, _objectUnderTest.OkCommand.CanExecuteCommand);
            Assert.AreEqual(DefaultProjectId, _objectUnderTest.GcpProjectId);
            Assert.IsNull(_objectUnderTest.Result);
        }

        [TestMethod]
        public void TestSetMvc()
        {
            _objectUnderTest = new TestTemplateChooserViewModelBase(_closeWindowMock);

            _objectUnderTest.IsMvc = true;

            Assert.IsTrue(_objectUnderTest.IsMvc);
            Assert.IsFalse(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.Mvc, _objectUnderTest.AppType);
            Assert.IsTrue(_objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestChangeToMvc()
        {
            _objectUnderTest = new TestTemplateChooserViewModelBase(_closeWindowMock);
            _objectUnderTest.IsWebApi = true;

            _objectUnderTest.IsMvc = true;

            Assert.IsTrue(_objectUnderTest.IsMvc);
            Assert.IsFalse(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.Mvc, _objectUnderTest.AppType);
            Assert.IsTrue(_objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestUnsetMvc()
        {
            _objectUnderTest = new TestTemplateChooserViewModelBase(_closeWindowMock);
            _objectUnderTest.IsMvc = true;

            _objectUnderTest.IsMvc = false;

            Assert.IsFalse(_objectUnderTest.IsMvc);
            Assert.IsFalse(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.None, _objectUnderTest.AppType);
            Assert.IsFalse(_objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestSetWebApi()
        {
            _objectUnderTest = new TestTemplateChooserViewModelBase(_closeWindowMock);
            _objectUnderTest.IsWebApi = true;

            Assert.IsFalse(_objectUnderTest.IsMvc);
            Assert.IsTrue(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.WebApi, _objectUnderTest.AppType);
            Assert.IsTrue(_objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestChangeToWebApi()
        {
            _objectUnderTest = new TestTemplateChooserViewModelBase(_closeWindowMock);
            _objectUnderTest.IsMvc = true;

            _objectUnderTest.IsWebApi = true;

            Assert.IsFalse(_objectUnderTest.IsMvc);
            Assert.IsTrue(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.WebApi, _objectUnderTest.AppType);
            Assert.IsTrue(_objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestUnsetWebApi()
        {
            _objectUnderTest = new TestTemplateChooserViewModelBase(_closeWindowMock);
            _objectUnderTest.IsWebApi = true;

            _objectUnderTest.IsWebApi = false;

            Assert.IsFalse(_objectUnderTest.IsMvc);
            Assert.IsFalse(_objectUnderTest.IsWebApi);
            Assert.AreEqual(AppType.None, _objectUnderTest.AppType);
            Assert.IsFalse(_objectUnderTest.OkCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestOkCommand()
        {
            _objectUnderTest = new TestTemplateChooserViewModelBase(_closeWindowMock);
            _objectUnderTest.OkCommand.Execute(null);

            _closeWindowMock.Verify(f => f(), Times.Once);
        }
    }
}
