using GoogleCloudExtension.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.ComponentModel;

namespace GoogleCloudExtensionUnitTests.Options
{
    [TestClass]
    public class AnalyticsOptionsPageViewModelTests
    {
        [TestMethod]
        public void TestInitialConditions()
        {
            var objectUnderTest = new AnalyticsOptionsPageViewModel();

            Assert.IsFalse(objectUnderTest.OptIn);
        }

        [TestMethod]
        public void TestSetOptIn()
        {
            var objectUnderTest = new AnalyticsOptionsPageViewModel();

            objectUnderTest.OptIn = true;

            Assert.IsTrue(objectUnderTest.OptIn);
        }

        [TestMethod]
        public void TestSetOptInRaisesPropertyChanged()
        {
            var objectUnderTest = new AnalyticsOptionsPageViewModel();
            var propertyChangedHandler = new Mock<PropertyChangedEventHandler>();
            objectUnderTest.PropertyChanged += propertyChangedHandler.Object;

            objectUnderTest.OptIn = true;

            propertyChangedHandler.Verify(
                h => h(
                    objectUnderTest,
                    It.Is<PropertyChangedEventArgs>(args => args.PropertyName == nameof(objectUnderTest.OptIn))),
                Times.Once);
        }
    }
}
