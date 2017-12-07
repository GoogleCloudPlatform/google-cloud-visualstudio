using GoogleCloudExtension.CloudExplorer.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.CloudExplorer.Options
{
    [TestClass]
    public class SerializableCloudExplorerOptionsTests
    {
        private Mock<ICloudExplorerOptions> _cloudExplorerOptionsMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _cloudExplorerOptionsMock = new Mock<ICloudExplorerOptions>();
        }

        [TestMethod]
        public void TestNullFiltersInput()
        {
            _cloudExplorerOptionsMock.SetupGet(o => o.PubSubTopicFilters).Returns(() => null);
            var objectUnderTest = new SerializableCloudExplorerOptions(_cloudExplorerOptionsMock.Object);

            Assert.AreEqual("null", objectUnderTest.PubSubTopicFiltersJsonString);
        }

        [TestMethod]
        public void TestEmptyFiltersInput()
        {
            _cloudExplorerOptionsMock.SetupGet(o => o.PubSubTopicFilters).Returns(new string[] { });
            var objectUnderTest = new SerializableCloudExplorerOptions(_cloudExplorerOptionsMock.Object);

            Assert.AreEqual("[]", objectUnderTest.PubSubTopicFiltersJsonString);
        }

        [TestMethod]
        public void TestFullFiltersInput()
        {
            _cloudExplorerOptionsMock.SetupGet(o => o.PubSubTopicFilters).Returns(new[] { "value1", "value2" });
            var objectUnderTest = new SerializableCloudExplorerOptions(_cloudExplorerOptionsMock.Object);

            Assert.AreEqual("[\"value1\",\"value2\"]", objectUnderTest.PubSubTopicFiltersJsonString);
        }

        [TestMethod]
        public void TestNullFiltersOutput()
        {
            var objectUnderTest = new SerializableCloudExplorerOptions(_cloudExplorerOptionsMock.Object);

            objectUnderTest.PubSubTopicFiltersJsonString = "null";

            _cloudExplorerOptionsMock.VerifySet(o => o.PubSubTopicFilters = null, Times.Once);
        }

        [TestMethod]
        public void TestEmptyFiltersOutput()
        {
            var objectUnderTest = new SerializableCloudExplorerOptions(_cloudExplorerOptionsMock.Object);

            objectUnderTest.PubSubTopicFiltersJsonString = "[]";
            _cloudExplorerOptionsMock.VerifySet(
                o => o.PubSubTopicFilters = It.Is<IEnumerable<string>>(e => !e.Any()), Times.Once);
        }

        [TestMethod]
        public void TestFullFiltersOutput()
        {
            var objectUnderTest = new SerializableCloudExplorerOptions(_cloudExplorerOptionsMock.Object);

            objectUnderTest.PubSubTopicFiltersJsonString = "[\"value1\", \"value2\"]";

            _cloudExplorerOptionsMock.VerifySet(
                o => o.PubSubTopicFilters =
                    It.Is<IEnumerable<string>>(e => e.SequenceEqual(new[] { "value1", "value2" })), Times.Once);
        }
    }
}
