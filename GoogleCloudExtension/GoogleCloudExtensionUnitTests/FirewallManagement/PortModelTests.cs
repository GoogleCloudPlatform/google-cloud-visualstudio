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
using System.Collections.Generic;
using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension;
using GoogleCloudExtension.FirewallManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.FirewallManagement
{
    [TestClass]
    public class PortModelTests
    {
        private static readonly PortInfo s_defaultPortInfo = new PortInfo("default-port-name", 0);
        private PortModel _defaultObjectUnderTest;
        private Instance _defaultInstance;
        private List<string> _changedProperties;

        [TestInitialize]
        public void BeforeEach()
        {
            _defaultInstance = new Instance();
            _defaultObjectUnderTest = new PortModel(s_defaultPortInfo, _defaultInstance);
            _changedProperties = new List<string>();
            _defaultObjectUnderTest.PropertyChanged += (sender, args) => _changedProperties.Add(args.PropertyName);
        }

        [TestMethod]
        public void TestConstructor_ThrowsArgumentNullForNullPortInfo()
        {
            var e = Assert.ThrowsException<ArgumentNullException>(() => new PortModel(null, _defaultInstance));

            Assert.AreEqual("port", e.ParamName);
        }

        [TestMethod]
        public void TestConstructor_ThrowsArgumentNullForNullInstance()
        {
            var e = Assert.ThrowsException<ArgumentNullException>(() => new PortModel(s_defaultPortInfo, null));

            Assert.AreEqual("instance", e.ParamName);
        }

        [TestMethod]
        public void TestConstructor_InitializesPortInfo()
        {
            var expectedPortInfo = new PortInfo("port-name", 32);

            var objectUnderTest = new PortModel(expectedPortInfo, _defaultInstance);

            Assert.AreEqual(expectedPortInfo, objectUnderTest.PortInfo);
        }

        [TestMethod]
        public void TestConstructor_InitializesInstance()
        {
            var expectedInstance = new Instance();

            var objectUnderTest = new PortModel(s_defaultPortInfo, expectedInstance);

            Assert.AreEqual(expectedInstance, objectUnderTest.Instance);
        }

        [TestMethod]
        public void TestConstructor_InitializesIsEnabledFalse()
        {
            Assert.IsFalse(_defaultObjectUnderTest.IsEnabled);
        }

        [TestMethod]
        public void TestConstructor_InitializesIsEnabledTrue()
        {
            var objectUnderTest = new PortModel(
                s_defaultPortInfo,
                PortTestHelpers.GetInstanceWithEnabledPort(s_defaultPortInfo));

            Assert.IsTrue(objectUnderTest.IsEnabled);
        }

        [TestMethod]
        public void TestGetDisplayString_FormattedFromPortInfo()
        {
            const string expectedPortName = "expected-port-name";
            const int expectedPortNumber = 15;
            const string expectedDescription = "Expected Description";
            var portInfo = new PortInfo(expectedPortName, expectedPortNumber, expectedDescription);

            var objectUnderTest = new PortModel(portInfo, _defaultInstance);

            Assert.AreEqual(
                string.Format(
                    Resources.PortManagerDisplayStringFormat,
                    expectedDescription,
                    expectedPortName,
                    expectedPortNumber),
                objectUnderTest.DisplayString);
        }

        [TestMethod]
        public void TestSetIsEnabled_UpdatesPropertyTrue()
        {
            var objectUnderTest = new PortModel(s_defaultPortInfo, _defaultInstance) { IsEnabled = false };

            objectUnderTest.IsEnabled = true;

            Assert.IsTrue(objectUnderTest.IsEnabled);
        }

        [TestMethod]
        public void TestSetIsEnabled_UpdatesPropertyFalse()
        {
            var objectUnderTest = new PortModel(s_defaultPortInfo, _defaultInstance) { IsEnabled = true };

            objectUnderTest.IsEnabled = false;

            Assert.IsFalse(objectUnderTest.IsEnabled);
        }

        [TestMethod]
        public void TestSetIsEnabled_NotifiesPropertyChanged()
        {
            _defaultObjectUnderTest.IsEnabled = true;

            CollectionAssert.Contains(_changedProperties, nameof(PortModel.IsEnabled));
        }

        [TestMethod]
        public void TestSetIsEnabled_NotifiesChangedPropertyChanged()
        {
            _defaultObjectUnderTest.IsEnabled = true;

            CollectionAssert.Contains(_changedProperties, nameof(PortModel.Changed));
        }

        [TestMethod]
        public void TestGetChanged_StartsFalse()
        {
            Assert.IsFalse(_defaultObjectUnderTest.Changed);
        }

        [TestMethod]
        public void TestGetChanged_TrueWhenIsEnabledStartFalseChangedToTrue()
        {
            _defaultObjectUnderTest.IsEnabled = true;

            Assert.IsTrue(_defaultObjectUnderTest.Changed);
        }

        [TestMethod]
        public void TestGetChanged_TrueWhenIsEnabledStartTrueChangedToFalse()
        {
            var objectUnderTest = new PortModel(s_defaultPortInfo, PortTestHelpers.GetInstanceWithEnabledPort(s_defaultPortInfo));

            objectUnderTest.IsEnabled = false;

            Assert.IsTrue(objectUnderTest.Changed);
        }

        [TestMethod]
        public void TestGetChanged_FalseWhenIsEnabledChangedBack()
        {
            _defaultObjectUnderTest.IsEnabled = true;
            _defaultObjectUnderTest.IsEnabled = false;

            Assert.IsFalse(_defaultObjectUnderTest.Changed);
        }
    }
}
