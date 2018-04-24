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

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleAnalyticsUtils;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.AnalyticsOptInDialog;
using GoogleCloudExtension.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;

namespace GoogleCloudExtensionUnitTests.Analytics
{
    [TestClass]
    public class EventsReporterWrapperTests : ExtensionTestBase
    {
        private AnalyticsOptions _analyticsOptions;
        private Mock<Func<bool>> _promptAnalyticsMock;

        protected override void BeforeEach()
        {
            _analyticsOptions = new AnalyticsOptions();
            PackageMock.Setup(p => p.AnalyticsSettings).Returns(_analyticsOptions);
            _promptAnalyticsMock = new Mock<Func<bool>>();
            EventsReporterWrapper.PromptAnalyticsOptIn = _promptAnalyticsMock.Object;
        }

        protected override void AfterEach()
        {
            EventsReporterWrapper.PromptAnalyticsOptIn = AnalyticsOptInWindow.PromptUser;
        }

        [TestMethod]
        public void TestEnsureAnalyticsOptInShowsPrompt()
        {
            _analyticsOptions.DialogShown = false;
            _analyticsOptions.OptIn = false;
            _promptAnalyticsMock.Setup(f => f()).Returns(true);

            EventsReporterWrapper.EnsureAnalyticsOptIn();

            _promptAnalyticsMock.Verify(f => f(), Times.Once);
            Assert.IsTrue(_analyticsOptions.OptIn);
            Assert.IsTrue(_analyticsOptions.DialogShown);
        }

        [TestMethod]
        public void TestEnsureAnalyticsOptInSkipsPrompt()
        {
            _analyticsOptions.DialogShown = true;
            _analyticsOptions.OptIn = false;

            EventsReporterWrapper.EnsureAnalyticsOptIn();

            _promptAnalyticsMock.Verify(f => f(), Times.Never);
            Assert.IsFalse(_analyticsOptions.OptIn);
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
            EventsReporterWrapper.ReporterLazy = new Lazy<IEventsReporter>(() => eventsReporterMock.Object);
            CredentialsStore.Default.UpdateCurrentAccount(new UserAccount { AccountName = "new-account" });
            CredentialsStore.Default.UpdateCurrentProject(
                new Project { ProjectId = "new-project-id", ProjectNumber = projectNumber });

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
