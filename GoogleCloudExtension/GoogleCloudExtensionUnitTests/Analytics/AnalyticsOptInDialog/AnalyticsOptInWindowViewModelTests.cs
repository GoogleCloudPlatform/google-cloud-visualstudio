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
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.AnalyticsOptInDialog;
using GoogleCloudExtension.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.Analytics.AnalyticsOptInDialog
{
    [TestClass]
    public class AnalyticsOptInWindowViewModelTests : ExtensionTestBase
    {
        private AnalyticsOptInWindowViewModel _objectUnderTest;
        private Mock<IBrowserService> _browserServiceMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _browserServiceMock = new Mock<IBrowserService>();

            PackageMock.Setup(p => p.GetMefServiceLazy<IBrowserService>())
                .Returns(_browserServiceMock.ToLazy());

            _objectUnderTest = new AnalyticsOptInWindowViewModel();
        }

        [TestMethod]
        public void TestConstructor_SetsOptInCommand()
        {
            _objectUnderTest = new AnalyticsOptInWindowViewModel();

            Assert.IsNotNull(_objectUnderTest.OptInCommand);
        }

        [TestMethod]
        public void TestConstructor_SetsAnalyticsLearnMoreLinkCommand()
        {
            _objectUnderTest = new AnalyticsOptInWindowViewModel();

            Assert.IsNotNull(_objectUnderTest.AnalyticsLearnMoreLinkCommand);
        }

        [TestMethod]
        public void TestConstructor_LeavesResultDefault()
        {
            _objectUnderTest = new AnalyticsOptInWindowViewModel();

            Assert.IsFalse(_objectUnderTest.Result);
        }

        [TestMethod]
        public void TestAnalyticsLearnMoreLinkCommand_OpensBrowser()
        {
            _objectUnderTest.AnalyticsLearnMoreLinkCommand.Execute(null);

            _browserServiceMock.Verify(s => s.OpenBrowser(AnalyticsLearnMoreConstants.AnalyticsLearnMoreLink));
        }

        [TestMethod]
        public void TestOptInCommand_SetsResult()
        {
            _objectUnderTest.OptInCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.Result);
        }

        [TestMethod]
        public void TestOptInCommand_InvokesClose()
        {
            var closeHandler = new Mock<Action>();
            _objectUnderTest.Close += closeHandler.Object;

            _objectUnderTest.OptInCommand.Execute(null);

            closeHandler.Verify(c => c());
        }
    }
}
