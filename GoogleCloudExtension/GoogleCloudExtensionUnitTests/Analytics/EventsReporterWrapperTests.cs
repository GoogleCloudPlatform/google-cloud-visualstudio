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

using GoogleAnalyticsUtils;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.AnalyticsOptInDialog;
using GoogleCloudExtension.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.Analytics
{
    [TestClass]
    public class EventsReporterWrapperTests : ExtensionTestBase
    {
        private AnalyticsOptions _generalOptions;

        [TestInitialize]
        public void BeforeEach()
        {
            _generalOptions = new AnalyticsOptions();
            PackageMock.Setup(p => p.GeneralSettings).Returns(_generalOptions);
        }

        [TestCleanup]
        public void AfterEach() => EventsReporterWrapper.DisableReporting();

        [TestMethod]
        public void TestEnsureAnalyticsOptIn_ShowsPromptWhenDialogNotShown()
        {
            _generalOptions.DialogShown = false;
            _generalOptions.OptIn = false;
            PackageMock.Setup(p => p.UserPromptService.PromptUser(It.IsAny<AnalyticsOptInWindowContent>()))
                .Returns(true);

            EventsReporterWrapper.EnsureAnalyticsOptIn();

            Assert.IsTrue(_generalOptions.OptIn);
            Assert.IsTrue(_generalOptions.DialogShown);
        }

        [TestMethod]
        public void TestEnsureAnalyticsOptIn_SkipsPromptWhenDialogShown()
        {
            _generalOptions.DialogShown = true;
            _generalOptions.OptIn = false;

            EventsReporterWrapper.EnsureAnalyticsOptIn();

            PackageMock.Verify(
                p => p.UserPromptService.PromptUser(It.IsAny<AnalyticsOptInWindowContent>()),
                Times.Never);
            Assert.IsTrue(_generalOptions.DialogShown);
            Assert.IsFalse(_generalOptions.OptIn);
        }

        [TestMethod]
        public void TestReportEvent()
        {
            const string eventName = "event name";
            const long projectNumber = 1138;
            const string metadataKey1 = "key 1";
            const string metadataVal1 = "val 1";
            const string metadataKey2 = "key 2";
            const string metadataVal2 = "val 2";
            var eventsReporterMock = new Mock<IEventsReporter>();
            EventsReporterWrapper.ReporterLazy = eventsReporterMock.ToLazy();
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectNumericId).Returns(projectNumber.ToString);

            EventsReporterWrapper.ReportEvent(new AnalyticsEvent(eventName, metadataKey1, metadataVal1, metadataKey2, metadataVal2));

            eventsReporterMock.Verify(
                r => r.ReportEvent(
                    EventsReporterWrapper.ExtensionEventSource, EventsReporterWrapper.ExtensionEventType, eventName,
                    true, projectNumber.ToString(),
                    It.Is((Dictionary<string, string> d) => d[metadataKey1] == metadataVal1 && d[metadataKey2] == metadataVal2)),
                Times.Once);
        }
    }
}
