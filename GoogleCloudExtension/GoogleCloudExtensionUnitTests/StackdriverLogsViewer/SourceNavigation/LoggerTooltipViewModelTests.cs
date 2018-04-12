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

using Google.Apis.Logging.v2.Data;
using GoogleCloudExtension;
using GoogleCloudExtension.Options;
using GoogleCloudExtension.StackdriverLogsViewer;
using GoogleCloudExtension.StackdriverLogsViewer.SourceNavigation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace GoogleCloudExtensionUnitTests.StackdriverLogsViewer.SourceNavigation
{
    [TestClass]
    public class LoggerTooltipViewModelTests
    {
        private Mock<IGoogleCloudExtensionPackage> _packageMock;
        private IGoogleCloudExtensionPackage _packageToRestore;

        [TestInitialize]
        public void BeforeEach()
        {
            _packageToRestore = GoogleCloudExtensionPackage.Instance;
            _packageMock = new Mock<IGoogleCloudExtensionPackage>();
            _packageMock.Setup(p => p.AnalyticsSettings).Returns(Mock.Of<AnalyticsOptions>(o => o.OptIn == false));
            GoogleCloudExtensionPackage.Instance = _packageMock.Object;
        }

        [TestCleanup]
        public void AfterEach()
        {
            GoogleCloudExtensionPackage.Instance = _packageToRestore;
        }

        [TestMethod]
        public void TestBackToLogsViewerCommand()
        {
            const int parentToolWindowId = 2;
            var logsToolWindowMock = new Mock<LogsViewerToolWindow> { CallBase = true };
            logsToolWindowMock.Object.Frame = LogsViewerToolWindowTests.GetMockedWindowFrame();
            _packageMock.Setup(p => p.FindToolWindow<LogsViewerToolWindow>(It.IsAny<bool>(), parentToolWindowId))
                .Returns(logsToolWindowMock.Object);
            string filter = null;
            logsToolWindowMock.Setup(w => w.ViewModel.FilterLog(It.IsAny<string>())).Callback((string s) => filter = s);
            var objectUnderTest =
                new LoggerTooltipViewModel(
                    new LogItem(
                        new LogEntry
                        {
                            Resource = new MonitoredResource { Type = "ResourceTypeId" },
                            LogName = "LogNameString",
                            SourceLocation = new LogEntrySourceLocation
                            {
                                File = @"source\file\path",
                                Function = "FunctionName",
                                Line = 1138
                            },
                            Timestamp = new DateTime(2011, 1, 1, 1, 1, 1, DateTimeKind.Utc)
                        }, TimeZoneInfo.Utc,
                        parentToolWindowId));
            objectUnderTest.BackToLogsViewerCommand.Execute(null);

            StringAssert.Contains(filter, "resource.type=\"ResourceTypeId\"");
            StringAssert.Contains(filter, "logName=\"LogNameString\"");
            StringAssert.Contains(filter, @"sourceLocation.File=""source\\file\\path""");
            StringAssert.Contains(filter, "sourceLocation.Function=\"FunctionName\"");
            StringAssert.Contains(filter, "sourceLocation.Line=\"1138\"");
        }
    }
}
