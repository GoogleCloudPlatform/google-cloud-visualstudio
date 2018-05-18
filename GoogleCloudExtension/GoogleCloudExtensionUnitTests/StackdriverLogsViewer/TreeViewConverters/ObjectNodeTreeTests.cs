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
using GoogleCloudExtension.StackdriverLogsViewer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GoogleCloudExtensionUnitTests.StackdriverLogsViewer.TreeViewConverters
{
    /// <summary>
    /// Summary description for ObjectNodeTreeTests
    /// </summary>
    [TestClass]
    public class ObjectNodeTreeTests
    {
        private const string NodeName = "test name";
        private const string NodeNameWithColon = "test name :";

        [TestInitialize]
        public void BeforeEach()
        {
            Clipboard.Clear();
        }

        [TestMethod]
        public void TestStringValueInitialConditions()
        {
            const string nodeStringValue = "test value";
            var objectUnderTest = new ObjectNodeTree(NodeName, nodeStringValue, null);

            Assert.AreEqual(NodeNameWithColon, objectUnderTest.Name);
            Assert.AreEqual(NodeName, objectUnderTest.FilterLabel);
            Assert.AreEqual(nodeStringValue, objectUnderTest.NodeValue);
            Assert.IsNull(objectUnderTest.Parent);
            Assert.AreEqual(nodeStringValue, objectUnderTest.FilterValue);
            CollectionAssert.AreEqual(new ObjectNodeTree[0], objectUnderTest.Children);
        }

        [TestMethod]
        public void TestNullValueInitialConditions()
        {
            var objectUnderTest = new ObjectNodeTree(NodeName, null, null);

            Assert.AreEqual(NodeName, objectUnderTest.Name);
            Assert.AreEqual(NodeName, objectUnderTest.FilterLabel);
            Assert.IsNull(objectUnderTest.NodeValue);
            Assert.IsNull(objectUnderTest.FilterValue);
            Assert.IsNull(objectUnderTest.Parent);
            CollectionAssert.AreEqual(new ObjectNodeTree[0], objectUnderTest.Children);
        }

        [TestMethod]
        public void TestNumberValueInitialConditions()
        {
            const ushort nodeValue = 3;
            const string expectedNodeValue = "3";

            var objectUnderTest = new ObjectNodeTree(NodeName, nodeValue, null);

            Assert.AreEqual(NodeNameWithColon, objectUnderTest.Name);
            Assert.AreEqual(NodeName, objectUnderTest.FilterLabel);
            Assert.AreEqual(expectedNodeValue, objectUnderTest.NodeValue);
            Assert.AreEqual(expectedNodeValue, objectUnderTest.FilterValue);
            Assert.IsNull(objectUnderTest.Parent);
            CollectionAssert.AreEqual(new ObjectNodeTree[0], objectUnderTest.Children);
        }

        [TestMethod]
        public void TestDateTimeValueInitialConditions()
        {
            const string expectedNodeValue = "12-13-1999 00:00:00.000";
            const string expectedNodeFilterValue = "1999-12-13T00:00:00.0000000Z";
            var nodeValue = new DateTime(1999, 12, 13, 0, 0, 0, DateTimeKind.Utc);

            var objectUnderTest = new ObjectNodeTree(NodeName, nodeValue, null);

            Assert.AreEqual(NodeNameWithColon, objectUnderTest.Name);
            Assert.AreEqual(NodeName, objectUnderTest.FilterLabel);
            Assert.AreEqual(expectedNodeValue, objectUnderTest.NodeValue);
            Assert.AreEqual(expectedNodeFilterValue, objectUnderTest.FilterValue);
            Assert.IsNull(objectUnderTest.Parent);
            CollectionAssert.AreEqual(new ObjectNodeTree[0], objectUnderTest.Children);
        }

        [TestMethod]
        public void TestArrayValueInitialConditions()
        {
            var nodeValue = new[] { "a", "b", "c" };

            var objectUnderTest = new ObjectNodeTree(NodeName, nodeValue, null);

            Assert.AreEqual(NodeName, objectUnderTest.Name);
            Assert.AreEqual(NodeName, objectUnderTest.FilterLabel);
            Assert.IsNull(objectUnderTest.NodeValue);
            Assert.IsNull(objectUnderTest.FilterValue);
            Assert.IsNull(objectUnderTest.Parent);
            Assert.AreEqual(3, objectUnderTest.Children.Count);
            Assert.AreEqual(1, objectUnderTest.Children.Count(c => c.FilterValue == "a"));
            Assert.AreEqual(1, objectUnderTest.Children.Count(c => c.FilterValue == "b"));
            Assert.AreEqual(1, objectUnderTest.Children.Count(c => c.FilterValue == "c"));
        }

        [TestMethod]
        public void TestDictionaryValueInitialConditions()
        {
            var nodeValue = new Dictionary<string, string> { { "a", "1" }, { "b", "2" }, { "c", "3" } };

            var objectUnderTest = new ObjectNodeTree(NodeName, nodeValue, null);

            Assert.AreEqual(NodeName, objectUnderTest.FilterLabel);
            Assert.AreEqual(NodeName, objectUnderTest.Name);
            Assert.IsNull(objectUnderTest.NodeValue);
            Assert.IsNull(objectUnderTest.FilterValue);
            Assert.IsNull(objectUnderTest.Parent);
            Assert.AreEqual(3, objectUnderTest.Children.Count);
            Assert.AreEqual(1, objectUnderTest.Children.Count(c => c.FilterLabel == "a" && c.FilterValue == "1"));
            Assert.AreEqual(1, objectUnderTest.Children.Count(c => c.FilterLabel == "b" && c.FilterValue == "2"));
            Assert.AreEqual(1, objectUnderTest.Children.Count(c => c.FilterLabel == "c" && c.FilterValue == "3"));
        }

        [TestMethod]
        public void TestMonitoredResourceValueInitialConditions()
        {
            var nodeValue =
                new MonitoredResource { Labels = new Dictionary<string, string> { { "a", "1" } }, Type = "test type" };

            var objectUnderTest = new ObjectNodeTree(NodeName, nodeValue, null);

            Assert.AreEqual(NodeName, objectUnderTest.Name);
            Assert.AreEqual(NodeName, objectUnderTest.FilterLabel);
            Assert.IsNull(objectUnderTest.NodeValue);
            Assert.IsNull(objectUnderTest.FilterValue);
            Assert.IsNull(objectUnderTest.Parent);
            Assert.AreEqual(2, objectUnderTest.Children.Count);
            Assert.AreEqual(1, objectUnderTest.Children.Count(c => c.FilterLabel == "Labels" && c.Children.Count == 1));
            Assert.AreEqual(
                1, objectUnderTest.Children.Count(c => c.FilterLabel == "Type" && c.FilterValue == "test type"));
        }

        [TestMethod]
        public void TestCopyLeafToClipboard()
        {
            const ushort nodeValue = 3;
            const string expectedNodeClipboard = "3";
            var objectUnderTest = new ObjectNodeTree(NodeName, nodeValue, null);

            objectUnderTest.CopyCommand.Execute(null);

            Assert.AreEqual(expectedNodeClipboard, Clipboard.GetText());
        }

        [TestMethod]
        public void TestCopyBranchToClipboard()
        {
            const string expectedNodeClipboard = "[\"a\",\"b\",\"c\"]";
            var nodeValue = new[] { "a", "b", "c" };
            var objectUnderTest = new ObjectNodeTree(NodeName, nodeValue, null);

            objectUnderTest.CopyCommand.Execute(null);

            Assert.AreEqual(expectedNodeClipboard, Clipboard.GetText());
        }
    }
}
