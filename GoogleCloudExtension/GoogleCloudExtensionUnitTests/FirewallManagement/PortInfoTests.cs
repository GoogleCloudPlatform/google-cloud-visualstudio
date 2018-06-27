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

using GoogleCloudExtension.FirewallManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.FirewallManagement
{
    [TestClass]
    public class PortInfoTests
    {
        private const int DefaultPortNumber = 0;
        private const string DefaultName = "Default-Name";

        [TestMethod]
        public void TestConstructor_SetsName()
        {
            const string expectedName = "expected-name";
            var objectUnderTest = new PortInfo(expectedName, DefaultPortNumber);

            Assert.AreEqual(expectedName, objectUnderTest.Name);
        }

        [TestMethod]
        public void TestConstructor_SetsPort()
        {
            const int expectedPort = 32;
            var objectUnderTest = new PortInfo(DefaultName, expectedPort);

            Assert.AreEqual(expectedPort, objectUnderTest.Port);
        }

        [TestMethod]
        public void TestConstructor_DefaultsDescriptionToNull()
        {
            var objectUnderTest = new PortInfo(DefaultName, DefaultPortNumber);

            Assert.IsNull(objectUnderTest.Description);
        }

        [TestMethod]
        public void TestConstructor_SetsDescription()
        {
            const string expectedDescription = "expected description";
            var objectUnderTest = new PortInfo(DefaultName, DefaultPortNumber, expectedDescription);

            Assert.AreEqual(expectedDescription, objectUnderTest.Description);
        }

        [TestMethod]
        public void TestGetTag_ReturnsStringFromInstanceNameAndPort()
        {
            const int expectedPort = 32;
            var objectUnderTest = new PortInfo(DefaultName, expectedPort);

            Assert.AreEqual("instance-name-tcp-32", objectUnderTest.GetTag("instance-name"));
        }
    }
}
