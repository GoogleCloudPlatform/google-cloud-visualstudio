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

using GoogleCloudExtension.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.Options
{
    [TestClass]
    public class AnalyticsOptionsTests : ExtensionTestBase
    {
        [TestMethod]
        public void TestInitialConditions()
        {
            var objectUnderTest = new AnalyticsOptions();

            Assert.IsFalse(objectUnderTest.OptIn);
            Assert.IsFalse(objectUnderTest.DialogShown);
            Assert.IsNull(objectUnderTest.ClientId);
            Assert.IsNull(objectUnderTest.InstalledVersion);
        }

        [TestMethod]
        public void TestSetOptIn()
        {
            var objectUnderTest = new AnalyticsOptions();

            objectUnderTest.OptIn = true;

            Assert.IsTrue(objectUnderTest.OptIn);
        }

        [TestMethod]
        public void TestSetDialogShown()
        {
            var objectUnderTest = new AnalyticsOptions();

            objectUnderTest.DialogShown = true;

            Assert.IsTrue(objectUnderTest.DialogShown);
        }

        [TestMethod]
        public void TestSetClientId()
        {
            const string testClientId = "test-client-id-string";
            var objectUnderTest = new AnalyticsOptions();

            objectUnderTest.ClientId = testClientId;

            Assert.AreEqual(testClientId, objectUnderTest.ClientId);
        }

        [TestMethod]
        public void TestSetInstalledVersion()
        {
            const string testVersionString = "test-version-string";
            var objectUnderTest = new AnalyticsOptions();

            objectUnderTest.InstalledVersion = testVersionString;

            Assert.AreEqual(testVersionString, objectUnderTest.InstalledVersion);
        }

        [TestMethod]
        public void TestResetSettings()
        {
            const string testClientId = "test-client-id-string";
            var objectUnderTest = new AnalyticsOptions
            {
                ClientId = testClientId,
                DialogShown = true,
                OptIn = true
            };

            objectUnderTest.ResetSettings();

            Assert.IsFalse(objectUnderTest.OptIn);
            Assert.IsFalse(objectUnderTest.DialogShown);
            Assert.IsNull(objectUnderTest.ClientId);
        }

        [TestMethod]
        public void TestSaveSettingsSettingsInitalizesClientId()
        {
            var objectUnderTest = new AnalyticsOptions
            {
                OptIn = true,
                ClientId = null
            };

            objectUnderTest.SaveSettingsToStorage();

            Assert.IsNotNull(objectUnderTest.ClientId);
        }

        [TestMethod]
        public void TestSaveSettingsSettingsDiablesClientId()
        {
            var objectUnderTest = new AnalyticsOptions
            {
                ClientId = "test-client-id-string",
                OptIn = false
            };

            objectUnderTest.SaveSettingsToStorage();

            Assert.IsNull(objectUnderTest.ClientId);
        }
    }
}
