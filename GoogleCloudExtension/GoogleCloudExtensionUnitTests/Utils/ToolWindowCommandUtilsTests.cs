﻿// Copyright 2018 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class ToolWindowCommandUtilsTests : ExtensionTestBase
    {
        private IVsWindowFrame _defaultFrame;

        [TestInitialize]
        public void BeforeEach() => _defaultFrame = VsWindowFrameMocks.GetMockedWindowFrame();

        [TestMethod]
        public void TestShowToolWindowNoPackage()
        {
            GoogleCloudExtensionPackage.Instance = null;

            var result = ToolWindowCommandUtils.ShowToolWindow<ToolWindowPane>();

            Assert.IsNull(result);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestShowToolWindowNoWindow()
        {
            PackageMock.Setup(p => p.FindToolWindow<ToolWindowPane>(true, 0)).Returns(() => null);

            ToolWindowCommandUtils.ShowToolWindow<ToolWindowPane>();
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestShowToolWindowNoWindowFrame()
        {
            PackageMock.Setup(p => p.FindToolWindow<ToolWindowPane>(true, 0))
                .Returns(() => new ToolWindowPane { Frame = null });

            ToolWindowCommandUtils.ShowToolWindow<ToolWindowPane>();
        }

        [TestMethod]
        [ExpectedException(typeof(COMException))]
        public void TestShowToolWindowShowError()
        {
            var mockedFrame = Mock.Of<IVsWindowFrame>(f => f.Show() == VSConstants.E_UNEXPECTED);
            PackageMock.Setup(p => p.FindToolWindow<ToolWindowPane>(true, 0))
                .Returns(() => new ToolWindowPane { Frame = mockedFrame });

            ToolWindowCommandUtils.ShowToolWindow<ToolWindowPane>();
        }

        [TestMethod]
        public void TestShowToolWindowSuccess()
        {
            var mockedFrame = Mock.Of<IVsWindowFrame>(f => f.Show() == VSConstants.S_OK);
            var expectedResult = new ToolWindowPane { Frame = mockedFrame };
            PackageMock.Setup(p => p.FindToolWindow<ToolWindowPane>(true, 0)).Returns(() => expectedResult);

            var actualResult = ToolWindowCommandUtils.ShowToolWindow<ToolWindowPane>();

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestShowToolWindowSpecificSuccess()
        {
            var expectedResult = new ToolWindowPane { Frame = _defaultFrame };
            const int toolWindowId = 2;
            PackageMock.Setup(p => p.FindToolWindow<ToolWindowPane>(true, toolWindowId))
                .Returns(() => expectedResult);

            var actualResult = ToolWindowCommandUtils.ShowToolWindow<ToolWindowPane>(toolWindowId);

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestAddToolWindow()
        {
            var expectedResult = new ToolWindowPane { Frame = _defaultFrame };
            PackageMock.Setup(p => p.FindToolWindow<ToolWindowPane>(false, 0))
                .Returns(() => null);
            PackageMock.Setup(p => p.FindToolWindow<ToolWindowPane>(true, 0))
                .Returns(() => expectedResult);

            var actualResult = ToolWindowCommandUtils.AddToolWindow<ToolWindowPane>();

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestAddToolWindowAdditional()
        {
            var existingWindow = new ToolWindowPane { Frame = _defaultFrame };
            var expectedResult = new ToolWindowPane { Frame = _defaultFrame };
            PackageMock.Setup(p => p.FindToolWindow<ToolWindowPane>(false, 0))
                .Returns(() => existingWindow);
            PackageMock.Setup(p => p.FindToolWindow<ToolWindowPane>(false, 1))
                .Returns(() => null);
            PackageMock.Setup(p => p.FindToolWindow<ToolWindowPane>(true, 1))
                .Returns(() => expectedResult);

            var actualResult = ToolWindowCommandUtils.AddToolWindow<ToolWindowPane>();

            Assert.AreEqual(expectedResult, actualResult);
        }

        [TestMethod]
        public void TestEnableMenuItemOnValidProjectIdNullSender() => ToolWindowCommandUtils.EnableMenuItemOnValidProjectId(null, null);

        [TestMethod]
        public void TestEnableMenuItemOnValidProjectIdInvalidSender() => ToolWindowCommandUtils.EnableMenuItemOnValidProjectId(Mock.Of<object>(), null);

        [TestMethod]
        public void TestEnableMenuItemOnValidProjectIdInvalidProjectId()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(() => null);
            var menuCommand = new OleMenuCommand((sender, args) => { }, (sender, args) => { },
                (sender, args) => { }, new CommandID(Guid.Empty, 0));

            ToolWindowCommandUtils.EnableMenuItemOnValidProjectId(menuCommand, null);

            Assert.IsFalse(menuCommand.Enabled);
        }

        [TestMethod]
        public void TestEnableMenuItemOnValidProjectIdValidProjectId()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns("valid-project-id");
            var menuCommand = new OleMenuCommand(
                (sender, args) => { }, (sender, args) => { },
                (sender, args) => { }, new CommandID(Guid.Empty, 0));

            ToolWindowCommandUtils.EnableMenuItemOnValidProjectId(menuCommand, null);

            Assert.IsTrue(menuCommand.Enabled);
        }
    }
}
