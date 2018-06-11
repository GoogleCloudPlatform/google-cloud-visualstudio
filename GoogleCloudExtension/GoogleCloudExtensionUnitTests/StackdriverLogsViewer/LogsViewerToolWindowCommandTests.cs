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

using GoogleCloudExtension.StackdriverLogsViewer;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.ComponentModel.Design;

namespace GoogleCloudExtensionUnitTests.StackdriverLogsViewer
{
    [TestClass]
    public class LogsViewerToolWindowCommandTests : ExtensionTestBase
    {
        private Mock<IMenuCommandService> _menuCommandServiceMock;

        protected override void BeforeEach()
        {
            _menuCommandServiceMock = new Mock<IMenuCommandService>();
            PackageMock.Setup(p => p.GetService(typeof(IMenuCommandService)))
                .Returns(_menuCommandServiceMock.Object);
        }

        [TestMethod]
        public void TestRegisterCommand()
        {
            LogsViewerToolWindowCommand.Initialize(PackageMock.Object);

            _menuCommandServiceMock.Verify(
                s => s.AddCommand(
                    It.Is((MenuCommand c) => c.CommandID.Equals(LogsViewerToolWindowCommand.MenuCommandID))),
                Times.Once);
        }

        [TestMethod]
        public void TestExecuteCommand()
        {
            MenuCommand command = null;
            _menuCommandServiceMock.Setup(
                s => s.AddCommand(It.IsAny<MenuCommand>())).Callback((MenuCommand c) => command = c);
            Mock<IVsWindowFrame> frameMock = VsWindowFrameMocks.GetWindowFrameMock(MockBehavior.Loose);
            PackageMock.Setup(p => p.FindToolWindow<LogsViewerToolWindow>(false, It.IsAny<int>()))
                .Returns(() => null);
            var logsViewerToolWindow = Mock.Of<LogsViewerToolWindow>();
            logsViewerToolWindow.Frame = frameMock.Object;
            PackageMock.Setup(p => p.FindToolWindow<LogsViewerToolWindow>(true, It.IsAny<int>()))
                .Returns(logsViewerToolWindow);

            LogsViewerToolWindowCommand.Initialize(PackageMock.Object);
            command.Invoke();

            frameMock.Verify(f => f.Show());
        }
    }
}
