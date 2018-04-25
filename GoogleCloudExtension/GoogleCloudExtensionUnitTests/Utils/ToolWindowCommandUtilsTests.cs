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

using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Utils;
using GoogleCloudExtensionUnitTests.StackdriverLogsViewer;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Project = Google.Apis.CloudResourceManager.v1.Data.Project;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class ToolWindowCommandUtilsTests : ExtensionTestBase
    {
        private Mock<IGoogleCloudExtensionPackage> _packageMock;
        private IVsWindowFrame _defaultFrame;

        protected override void BeforeEach()
        {
            _packageMock = new Mock<IGoogleCloudExtensionPackage>();
            _defaultFrame = LogsViewerToolWindowTests.GetMockedWindowFrame();
            GoogleCloudExtensionPackage.Instance = _packageMock.Object;
        }

        [TestMethod]
        public void TestShowToolWindowNoPackage()
        {
            GoogleCloudExtensionPackage.Instance = null;

            var result = ToolWindowCommandUtils.ShowToolWindow<TestToolWindowPane>();

            Assert.IsNull(result);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestShowToolWindowNoWindow()
        {
            _packageMock.Setup(p => p.FindToolWindow<TestToolWindowPane>(true, 0)).Returns(() => null);

            ToolWindowCommandUtils.ShowToolWindow<TestToolWindowPane>();
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestShowToolWindowNoWindowFrame()
        {
            _packageMock.Setup(p => p.FindToolWindow<TestToolWindowPane>(true, 0))
                .Returns(() => new TestToolWindowPane { Frame = null });

            ToolWindowCommandUtils.ShowToolWindow<TestToolWindowPane>();
        }

        [TestMethod]
        [ExpectedException(typeof(COMException))]
        public void TestShowToolWindowShowError()
        {
            var mockedFrame = Mock.Of<IVsWindowFrame>(f => f.Show() == VSConstants.E_UNEXPECTED);
            _packageMock.Setup(p => p.FindToolWindow<TestToolWindowPane>(true, 0))
                .Returns(() => new TestToolWindowPane { Frame = mockedFrame });

            ToolWindowCommandUtils.ShowToolWindow<TestToolWindowPane>();
        }

        [TestMethod]
        public void TestShowToolWindowSuccess()
        {
            var mockedFrame = Mock.Of<IVsWindowFrame>(f => f.Show() == VSConstants.S_OK);
            var expectedResult = new TestToolWindowPane { Frame = mockedFrame };
            _packageMock.Setup(p => p.FindToolWindow<TestToolWindowPane>(true, 0)).Returns(() => expectedResult);

            var actualResult = ToolWindowCommandUtils.ShowToolWindow<TestToolWindowPane>();

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestShowToolWindowSpecificSuccess()
        {
            var expectedResult = new TestToolWindowPane { Frame = _defaultFrame };
            const int toolWindowId = 2;
            _packageMock.Setup(p => p.FindToolWindow<TestToolWindowPane>(true, toolWindowId))
                .Returns(() => expectedResult);

            var actualResult = ToolWindowCommandUtils.ShowToolWindow<TestToolWindowPane>(toolWindowId);

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestAddToolWindow()
        {
            var expectedResult = new TestToolWindowPane { Frame = _defaultFrame };
            _packageMock.Setup(p => p.FindToolWindow<TestToolWindowPane>(false, 0))
                .Returns(() => null);
            _packageMock.Setup(p => p.FindToolWindow<TestToolWindowPane>(true, 0))
                .Returns(() => expectedResult);

            var actualResult = ToolWindowCommandUtils.AddToolWindow<TestToolWindowPane>();

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestAddToolWindowAdditional()
        {
            var existingWindow = new TestToolWindowPane { Frame = _defaultFrame };
            var expectedResult = new TestToolWindowPane { Frame = _defaultFrame };
            _packageMock.Setup(p => p.FindToolWindow<TestToolWindowPane>(false, 0))
                .Returns(() => existingWindow);
            _packageMock.Setup(p => p.FindToolWindow<TestToolWindowPane>(false, 1))
                .Returns(() => null);
            _packageMock.Setup(p => p.FindToolWindow<TestToolWindowPane>(true, 1))
                .Returns(() => expectedResult);

            var actualResult = ToolWindowCommandUtils.AddToolWindow<TestToolWindowPane>();

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEnableMenuItemOnValidProjectIdNullSender()
        {
            ToolWindowCommandUtils.EnableMenuItemOnValidProjectId(null, null);
        }

        [TestMethod]
        public void TestEnableMenuItemOnValidProjectIdInvalidSender()
        {
            ToolWindowCommandUtils.EnableMenuItemOnValidProjectId(Mock.Of<object>(), null);
        }

        [TestMethod]
        public void TestEnableMenuItemOnValidProjectIdInvalidProjectId()
        {
            CredentialsStore.Default.UpdateCurrentProject(null);
            var menuCommand = new OleMenuCommand((sender, args) => { }, (sender, args) => { },
                (sender, args) => { }, new CommandID(Guid.Empty, 0));

            ToolWindowCommandUtils.EnableMenuItemOnValidProjectId(menuCommand, null);

            Assert.IsFalse(menuCommand.Enabled);
        }

        [TestMethod]
        public void TestEnableMenuItemOnValidProjectIdValidProjectId()
        {
            CredentialsStore.Default.UpdateCurrentProject(
                new Project { ProjectId = "project-id" });
            var menuCommand = new OleMenuCommand(
                (sender, args) => { }, (sender, args) => { },
                (sender, args) => { }, new CommandID(Guid.Empty, 0));

            ToolWindowCommandUtils.EnableMenuItemOnValidProjectId(menuCommand, null);

            Assert.IsTrue(menuCommand.Enabled);
        }

        private class TestToolWindowPane : ToolWindowPane { }
    }
}
