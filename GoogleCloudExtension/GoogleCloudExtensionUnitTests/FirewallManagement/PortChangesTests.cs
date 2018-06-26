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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.FirewallManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.FirewallManagement
{
    [TestClass]
    public class PortChangesTests
    {
        [TestMethod]
        public void TestConstructor_AddsNothingFromNull()
        {
            var objectUnderTest = new PortChanges(null);

            CollectionAssert.That.IsEmpty(objectUnderTest.PortsToDisable);
            CollectionAssert.That.IsEmpty(objectUnderTest.PortsToEnable);
        }

        [TestMethod]
        public void TestConstructor_AddsNothingFromEmpty()
        {
            var objectUnderTest = new PortChanges(Enumerable.Empty<PortModel>());

            CollectionAssert.That.IsEmpty(objectUnderTest.PortsToDisable);
            CollectionAssert.That.IsEmpty(objectUnderTest.PortsToEnable);
        }

        [TestMethod]
        public void TestConstructor_AddsEnabledPorts()
        {
            var portToEnable = new PortModel(new PortInfo("port", 32), new Instance()) { IsEnabled = true };
            var objectUnderTest = new PortChanges(new[] { portToEnable });

            FirewallPort firewallPortToEnable = objectUnderTest.PortsToEnable.Single();
            Assert.AreEqual(portToEnable.PortInfo.Port, firewallPortToEnable.Port);
            Assert.AreEqual(portToEnable.GetPortInfoTag(), firewallPortToEnable.Name);
        }

        [TestMethod]
        public void TestConstructor_AddsDisabledPorts()
        {
            var portToDisable = new PortModel(new PortInfo("port", 32), new Instance()) { IsEnabled = false };
            var objectUnderTest = new PortChanges(new[] { portToDisable });

            FirewallPort firewallPortToDisable = objectUnderTest.PortsToDisable.Single();
            Assert.AreEqual(portToDisable.PortInfo.Port, firewallPortToDisable.Port);
            Assert.AreEqual(portToDisable.GetPortInfoTag(), firewallPortToDisable.Name);
        }

        [TestMethod]
        public void TestHasChanges_FalseForEmptyLists()
        {
            var objectUnderTest = new PortChanges(Enumerable.Empty<PortModel>());

            Assert.IsFalse(objectUnderTest.HasChanges);
        }

        [TestMethod]
        public void TestHasChanges_TrueWhenHasPortToEnable()
        {
            var objectUnderTest =
                new PortChanges(Enumerable.Empty<PortModel>()) { PortsToEnable = { new FirewallPort("", 0) } };

            Assert.IsTrue(objectUnderTest.HasChanges);
        }

        [TestMethod]
        public void TestHasChanges_TrueWhenHasPortToDisable()
        {
            var objectUnderTest =
                new PortChanges(Enumerable.Empty<PortModel>()) { PortsToDisable = { new FirewallPort("", 0) } };

            Assert.IsTrue(objectUnderTest.HasChanges);
        }
    }
}
