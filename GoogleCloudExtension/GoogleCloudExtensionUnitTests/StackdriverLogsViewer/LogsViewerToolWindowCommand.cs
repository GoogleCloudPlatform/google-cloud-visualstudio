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
using GoogleCloudExtension.Options;
using GoogleCloudExtension.StackdriverLogsViewer;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.ComponentModel.Design;

namespace GoogleCloudExtensionUnitTests.StackdriverLogsViewer
{
    [TestClass]
    public class LogsViewerToolWindowCommandTests
    {
        private IGoogleCloudExtensionPackage _packageToRestore;
        private Mock<IGoogleCloudExtensionPackage> _packageMock;
        private Mock<IMenuCommandService> _menuCommandServiceMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _menuCommandServiceMock = new Mock<IMenuCommandService>();
            _packageMock = new Mock<IGoogleCloudExtensionPackage>();
            _packageMock.Setup(p => p.AnalyticsSettings).Returns(Mock.Of<AnalyticsOptions>(o => o.OptIn == false));
            _packageMock.Setup(p => p.GetService(typeof(IMenuCommandService)))
                .Returns(_menuCommandServiceMock.Object);

            _packageToRestore = GoogleCloudExtensionPackage.Instance;
            GoogleCloudExtensionPackage.Instance = _packageMock.Object;
        }

        [TestCleanup]
        public void AfterEach()
        {
            GoogleCloudExtensionPackage.Instance = _packageToRestore;
        }

        [TestMethod]
        public void TestRegisterCommand()
        {
            LogsViewerToolWindowCommand.Initialize(_packageMock.Object);

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
            Mock<IVsWindowFrame> frameMock = LogsViewerToolWindowTests.GetWindowFrameMock();
            _packageMock.Setup(p => p.FindToolWindow<LogsViewerToolWindow>(false, It.IsAny<int>()))
                .Returns(() => null);
            _packageMock.Setup(p => p.FindToolWindow<LogsViewerToolWindow>(true, It.IsAny<int>()))
                .Returns(new LogsViewerToolWindow { Frame = frameMock.Object });

            LogsViewerToolWindowCommand.Initialize(_packageMock.Object);
            command.Invoke();

            frameMock.Verify(f => f.Show());
        }
    }
}
