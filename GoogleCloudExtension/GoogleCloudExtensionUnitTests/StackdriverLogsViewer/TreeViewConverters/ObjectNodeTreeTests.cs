using Google.Apis.Logging.v2.Data;
using GoogleCloudExtension.StackdriverLogsViewer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Windows;

namespace GoogleCloudExtensionUnitTests.StackdriverLogsViewer.TreeViewConverters
{
    /// <summary>
    /// Summary description for ObjectNodeTreeTests
    /// </summary>
    [TestClass]
    public class ObjectNodeTreeTests
    {
        [TestInitialize]
        public void BeforeEach()
        {
            Clipboard.Clear();
        }

        [TestMethod]
        public void TestStringValueInitialConditions()
        {
            const string nodeName = "test name";
            const string expectedNodeName = "test name :";
            const string nodeValue = "test value";

            var objectUnderTest = new ObjectNodeTree(nodeName, nodeValue, null);

            Assert.AreEqual(expectedNodeName, objectUnderTest.Name);
            Assert.AreEqual(nodeValue, objectUnderTest.NodeValue);
            Assert.IsNull(objectUnderTest.Parent);
            Assert.AreEqual(nodeName, objectUnderTest.FilterLabel);
            Assert.AreEqual(nodeValue, objectUnderTest.FilterValue);
            CollectionAssert.AreEqual(new ObjectNodeTree[0], objectUnderTest.Children);
        }

        [TestMethod]
        public void TestNullValueInitialConditions()
        {
            const string nodeName = "test name";

            var objectUnderTest = new ObjectNodeTree(nodeName, null, null);

            Assert.AreEqual(nodeName, objectUnderTest.Name);
            Assert.IsNull(objectUnderTest.NodeValue);
            Assert.AreEqual(nodeName, objectUnderTest.FilterLabel);
            Assert.IsNull(objectUnderTest.FilterValue);
            CollectionAssert.AreEqual(new ObjectNodeTree[0], objectUnderTest.Children);
        }

        [TestMethod]
        public void TestNumberValueInitialConditions()
        {
            const string nodeName = "test name";
            const string expectedNodeName = "test name :";
            const ushort nodeValue = 3;
            const string expectedNodeValue = "3";

            var objectUnderTest = new ObjectNodeTree(nodeName, nodeValue, null);

            Assert.AreEqual(expectedNodeName, objectUnderTest.Name);
            Assert.AreEqual(expectedNodeValue, objectUnderTest.NodeValue);
            Assert.IsNull(objectUnderTest.Parent);
            Assert.AreEqual(nodeName, objectUnderTest.FilterLabel);
            Assert.AreEqual(expectedNodeValue, objectUnderTest.FilterValue);
            CollectionAssert.AreEqual(new ObjectNodeTree[0], objectUnderTest.Children);
        }

        [TestMethod]
        public void TestDateTimeValueInitialConditions()
        {
            const string nodeName = "test name";
            const string expectedNodeName = "test name :";
            const string expectedNodeValue = "12-13-1999 00:00:00.000";
            const string expectedNodeFilterValue = "1999-12-13T00:00:00.0000000Z";
            var nodeValue = new DateTime(1999, 12, 13, 0, 0, 0, DateTimeKind.Utc);

            var objectUnderTest = new ObjectNodeTree(nodeName, nodeValue, null);

            Assert.AreEqual(expectedNodeName, objectUnderTest.Name);
            Assert.AreEqual(expectedNodeValue, objectUnderTest.NodeValue);
            Assert.IsNull(objectUnderTest.Parent);
            Assert.AreEqual(nodeName, objectUnderTest.FilterLabel);
            Assert.AreEqual(expectedNodeFilterValue, objectUnderTest.FilterValue);
            CollectionAssert.AreEqual(new ObjectNodeTree[0], objectUnderTest.Children);
        }

        [TestMethod]
        public void TestArrayValueInitialConditions()
        {
            const string nodeName = "test name";
            const string expectedNodeName = "test name";
            var nodeValue = new[] { "a", "b", "c" };

            var objectUnderTest = new ObjectNodeTree(nodeName, nodeValue, null);

            Assert.AreEqual(expectedNodeName, objectUnderTest.Name);
            Assert.IsNull(objectUnderTest.NodeValue);
            Assert.IsNull(objectUnderTest.Parent);
            Assert.AreEqual(nodeName, objectUnderTest.FilterLabel);
            Assert.IsNull(objectUnderTest.FilterValue);
            Assert.AreEqual(3, objectUnderTest.Children.Count);
        }

        [TestMethod]
        public void TestDictionaryValueInitialConditions()
        {
            const string nodeName = "test name";
            const string expectedNodeName = "test name";
            var nodeValue = new Dictionary<string, string> { { "a", "1" }, { "b", "2" }, { "c", "3" } };

            var objectUnderTest = new ObjectNodeTree(nodeName, nodeValue, null);

            Assert.AreEqual(expectedNodeName, objectUnderTest.Name);
            Assert.IsNull(objectUnderTest.NodeValue);
            Assert.IsNull(objectUnderTest.Parent);
            Assert.AreEqual(nodeName, objectUnderTest.FilterLabel);
            Assert.IsNull(objectUnderTest.FilterValue);
            Assert.AreEqual(3, objectUnderTest.Children.Count);
        }

        [TestMethod]
        public void TestMonitoredResourceValueInitialConditions()
        {
            const string nodeName = "test name";
            const string expectedNodeName = "test name";
            var nodeValue =
                new MonitoredResource { Labels = new Dictionary<string, string> { { "a", "1" } }, Type = "test type" };

            var objectUnderTest = new ObjectNodeTree(nodeName, nodeValue, null);

            Assert.AreEqual(expectedNodeName, objectUnderTest.Name);
            Assert.IsNull(objectUnderTest.NodeValue);
            Assert.IsNull(objectUnderTest.Parent);
            Assert.AreEqual(nodeName, objectUnderTest.FilterLabel);
            Assert.IsNull(objectUnderTest.FilterValue);
            Assert.AreEqual(2, objectUnderTest.Children.Count);
        }

        [TestMethod]
        public void TestCopyLeafToClipboard()
        {
            const string nodeName = "test name";
            const ushort nodeValue = 3;
            const string expectedNodeClipboard = "3";
            var objectUnderTest = new ObjectNodeTree(nodeName, nodeValue, null);

            objectUnderTest.CopyCommand.Execute(null);

            Assert.AreEqual(expectedNodeClipboard, Clipboard.GetText());
        }

        [TestMethod]
        public void TestCopyBranchToClipboard()
        {
            const string nodeName = "test name";
            const string expectedNodeClipboard = "[\"a\",\"b\",\"c\"]";
            var nodeValue = new[] { "a", "b", "c" };
            var objectUnderTest = new ObjectNodeTree(nodeName, nodeValue, null);

            objectUnderTest.CopyCommand.Execute(null);

            Assert.AreEqual(expectedNodeClipboard, Clipboard.GetText());
        }
    }
}
