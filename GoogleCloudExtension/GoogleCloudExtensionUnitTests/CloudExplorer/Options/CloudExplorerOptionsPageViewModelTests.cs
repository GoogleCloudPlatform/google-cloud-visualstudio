using GoogleCloudExtension.CloudExplorer.Options;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ComponentModel;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.CloudExplorer.Options
{
    [TestClass]
    public class CloudExplorerOptionsPageViewModelTests
    {
        private Mock<Action> _resetFieldsMock;
        private CloudExplorerOptionsPageViewModel _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _resetFieldsMock = new Mock<Action>();
            _objectUnderTest = new CloudExplorerOptionsPageViewModel(_resetFieldsMock.Object);
        }

        [TestMethod]
        public void TestNotifyPropertyChanged()
        {
            var propertyChangedHandlerMock = new Mock<Action<object, PropertyChangedEventArgs>>();
            _objectUnderTest.PropertyChanged += new PropertyChangedEventHandler(propertyChangedHandlerMock.Object);

            _objectUnderTest.PubSubTopicFilters = new EditableModel<string>[] { };

            propertyChangedHandlerMock.Verify(
                a => a(
                    _objectUnderTest,
                    It.Is<PropertyChangedEventArgs>(
                        args => args.PropertyName == nameof(_objectUnderTest.PubSubTopicFilters))),
                Times.Once);
        }

        [TestMethod]
        public void TestRestFieldsTriggersAction()
        {
            _objectUnderTest.ResetToDefaults.Execute(null);

            _resetFieldsMock.Verify(a => a(), Times.Once);
        }

        [TestMethod]
        public void TestSetNullFilters()
        {
            _objectUnderTest.PubSubTopicFilters = null;

            Assert.IsNotNull(_objectUnderTest.PubSubTopicFilters);
            Assert.AreEqual(0, _objectUnderTest.PubSubTopicFilters.Count());
        }

        [TestMethod]
        public void TestSetEmptyFilters()
        {
            _objectUnderTest.PubSubTopicFilters = Enumerable.Empty<EditableModel<string>>();

            Assert.IsNotNull(_objectUnderTest.PubSubTopicFilters);
            Assert.AreEqual(0, _objectUnderTest.PubSubTopicFilters.Count());
        }
    }
}
