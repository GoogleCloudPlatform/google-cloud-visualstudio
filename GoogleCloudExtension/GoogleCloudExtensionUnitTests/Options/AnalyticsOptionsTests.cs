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
using System.Collections.Generic;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.Options
{
    [TestClass]
    public class AnalyticsOptionsTests
    {
        private AnalyticsOptions _objectUnderTest;

        [TestInitialize]
        public void BeforeEach() => _objectUnderTest = new AnalyticsOptions();

        [TestMethod]
        public void TestInitialConditions()
        {
            Assert.IsFalse(_objectUnderTest.OptIn);
            Assert.IsFalse(_objectUnderTest.DialogShown);
            Assert.IsNull(_objectUnderTest.ClientId);
            Assert.IsNull(_objectUnderTest.InstalledVersion);
            Assert.IsFalse(_objectUnderTest.HideUserProjectControl);
        }

        [TestMethod]
        public void TestSetOptIn_SetsProperty()
        {
            _objectUnderTest.OptIn = true;

            Assert.IsTrue(_objectUnderTest.OptIn);
        }

        [TestMethod]
        public void TestSetDialogShown_SetsProperty()
        {
            _objectUnderTest.DialogShown = true;

            Assert.IsTrue(_objectUnderTest.DialogShown);
        }

        [TestMethod]
        public void TestSetClientId_SetsProperty()
        {
            const string testClientId = "test-client-id-string";

            _objectUnderTest.ClientId = testClientId;

            Assert.AreEqual(testClientId, _objectUnderTest.ClientId);
        }

        [TestMethod]
        public void TestSetInstalledVersion_SetsProperty()
        {
            const string testVersionString = "test-version-string";

            _objectUnderTest.InstalledVersion = testVersionString;

            Assert.AreEqual(testVersionString, _objectUnderTest.InstalledVersion);
        }

        [TestMethod]
        public void TestResetSettings_ResetsProperties()
        {
            const string testClientId = "test-client-id-string";
            _objectUnderTest.ClientId = testClientId;
            _objectUnderTest.DialogShown = true;
            _objectUnderTest.OptIn = true;
            _objectUnderTest.HideUserProjectControl = true;

            _objectUnderTest.ResetSettings();

            Assert.IsFalse(_objectUnderTest.OptIn);
            Assert.IsFalse(_objectUnderTest.DialogShown);
            Assert.IsNull(_objectUnderTest.ClientId);
            Assert.IsFalse(_objectUnderTest.HideUserProjectControl);
        }

        [TestMethod]
        public void TestSaveSettingsSettings_WithOptInInitalizesClientId()
        {
            _objectUnderTest.OptIn = true;
            _objectUnderTest.ClientId = null;

            _objectUnderTest.SaveSettingsToStorage();

            Assert.IsNotNull(_objectUnderTest.ClientId);
        }

        [TestMethod]
        public void TestSaveSettingsSettings_WithoutOptInClearsClientId()
        {
            _objectUnderTest.ClientId = "test-client-id-string";
            _objectUnderTest.OptIn = false;

            _objectUnderTest.SaveSettingsToStorage();

            Assert.IsNull(_objectUnderTest.ClientId);
        }

        [TestMethod]
        public void TestHideUserProjectControl_SetsProperty()
        {
            _objectUnderTest.HideUserProjectControl = true;

            Assert.IsTrue(_objectUnderTest.HideUserProjectControl);
        }

        [TestMethod]
        public void TestHideUserProjectControl_RaisesPropertyChanged()
        {
            var senders = new List<object>();
            var changedProperties = new List<string>();
            _objectUnderTest.PropertyChanged += (sender, args) =>
            {
                senders.Add(sender);
                changedProperties.Add(args.PropertyName);
            };

            _objectUnderTest.HideUserProjectControl = true;

            CollectionAssert.Contains(changedProperties, nameof(_objectUnderTest.HideUserProjectControl));
            CollectionAssert.That.All(senders).AreEqualTo(_objectUnderTest);
        }
    }
}
