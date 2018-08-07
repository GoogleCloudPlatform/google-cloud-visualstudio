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

using GoogleCloudExtension.Analytics.AnalyticsOptInDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.Analytics.AnalyticsOptInDialog
{
    [TestClass]
    public class AnalyticsOptInWindowContentTests : ExtensionTestBase
    {
        private AnalyticsOptInWindowContent _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new AnalyticsOptInWindowContent();
        }

        [TestMethod]
        public void TestConstructor_CreatesViewModel()
        {
            Assert.IsNotNull(_objectUnderTest.ViewModel);
        }

        [TestMethod]
        public void TestConstructor_SetsTitle()
        {
            Assert.AreEqual(GoogleCloudExtension.Resources.AnalyticsPromptTitle, _objectUnderTest.Title);
        }

        [TestMethod]
        public void TestConstructor_InitalizesComponent()
        {
            Assert.IsTrue(_objectUnderTest.IsInitialized);
        }
    }
}
