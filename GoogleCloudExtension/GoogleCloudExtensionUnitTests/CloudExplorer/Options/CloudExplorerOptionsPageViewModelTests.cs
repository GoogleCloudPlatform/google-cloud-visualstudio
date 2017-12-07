using GoogleCloudExtension.CloudExplorer.Options;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ComponentModel;

namespace GoogleCloudExtensionUnitTests.CloudExplorer.Options
{
    [TestClass]
    public class CloudExplorerOptionsPageViewModelTests
    {
        private Mock<Action> _resetFieldsMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _resetFieldsMock = new Mock<Action>();
        }

        [TestMethod]
        public void TestNotifyPropertyChanged()
        {
            var objectUnderTest = new CloudExplorerOptionsPageViewModel(_resetFieldsMock.Object);
            var propertyChangedHandlerMock = new Mock<Action<object, PropertyChangedEventArgs>>();
            objectUnderTest.PropertyChanged += new PropertyChangedEventHandler(propertyChangedHandlerMock.Object);

            objectUnderTest.PubSubTopicFilters = new EditableModel<string>[] { };

            propertyChangedHandlerMock.Verify(
                a => a(
                    objectUnderTest,
                    It.Is<PropertyChangedEventArgs>(
                        args => args.PropertyName == nameof(objectUnderTest.PubSubTopicFilters))),
                Times.Once);
        }

        [TestMethod]
        public void TestRestFieldsTriggers()
        {
            var objectUnderTest = new CloudExplorerOptionsPageViewModel(_resetFieldsMock.Object);

            objectUnderTest.ResetToDefaults.Execute(null);

            _resetFieldsMock.Verify(a => a(), Times.Once);
        }
    }
}
