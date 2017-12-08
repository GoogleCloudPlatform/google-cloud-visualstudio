using GoogleCloudExtension.CloudExplorer.Options;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.CloudExplorer.Options
{
    [TestClass]
    public class CloudExplorerOptionsTests
    {
        private Mock<ISettingsManager> _settingsManagerMock;
        private CloudExplorerOptions _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _settingsManagerMock = new Mock<ISettingsManager>();
            _settingsManagerMock.Setup(m => m.GetSubset(It.IsAny<string>())).Returns(Mock.Of<ISettingsSubset>());
            _objectUnderTest = new CloudExplorerOptions
            {
                Site = Mock.Of<ISite>(s => s.GetService(It.IsAny<Type>()) == _settingsManagerMock.Object)
            };
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            Assert.IsInstanceOfType(_objectUnderTest.AutomationObject, typeof(SerializableCloudExplorerOptions));
            CollectionAssert.AreEqual(
                CloudExplorerOptions.DefaultPubSubTopicFilters.ToList(),
                _objectUnderTest.PubSubTopicFilters.ToList());
        }

        [TestMethod]
        public void TestSetPubSubTopicFilters()
        {
            var newFilters = new[] { "some filter", "some other filter" };

            _objectUnderTest.PubSubTopicFilters = newFilters;

            CollectionAssert.AreEqual(newFilters, _objectUnderTest.PubSubTopicFilters.ToList());
        }

        [TestMethod]
        public void TestResetSettings()
        {
            _objectUnderTest.PubSubTopicFilters = new[] { "some filter", "some other filter" };

            _objectUnderTest.ResetSettings();

            CollectionAssert.AreEqual(
                CloudExplorerOptions.DefaultPubSubTopicFilters.ToList(),
                _objectUnderTest.PubSubTopicFilters.ToList());
        }

        [TestMethod]
        public void TestLoadSettingsMissing()
        {
            _settingsManagerMock.Setup(m => m.GetValueOrDefault(It.IsAny<string>(), It.IsAny<object>())).Returns(null);
            _objectUnderTest.PubSubTopicFilters = new[] { "some filter", "some other filter" };

            _objectUnderTest.LoadSettingsFromStorage();

            CollectionAssert.AreEqual(
                CloudExplorerOptions.DefaultPubSubTopicFilters.ToList(),
                _objectUnderTest.PubSubTopicFilters.ToList());
        }

        [TestMethod]
        public void TestLoadEmptySettings()
        {
            _settingsManagerMock
                .Setup(m => m.GetValueOrDefault(It.IsAny<string>(), It.IsAny<object>()))
                .Returns(JArray.FromObject(new List<string>()).ToString());
            _objectUnderTest.PubSubTopicFilters = new[] { "some filter", "some other filter" };

            _objectUnderTest.LoadSettingsFromStorage();

            CollectionAssert.AreEqual(
                new List<string>(),
                _objectUnderTest.PubSubTopicFilters.ToList());
        }

        [TestMethod]
        public void TestSaveSettingsTriggersEvent()
        {
            _settingsManagerMock.Setup(m => m.GetValueOrDefault(It.IsAny<string>(), It.IsAny<object>())).Returns(null);
            var savingEventHandlerMock = new Mock<EventHandler>();
            _objectUnderTest.SavingSettings += savingEventHandlerMock.Object;

            _objectUnderTest.SaveSettingsToStorage();

            savingEventHandlerMock.Verify(eh => eh(_objectUnderTest, EventArgs.Empty), Times.Once);
        }
    }
}
