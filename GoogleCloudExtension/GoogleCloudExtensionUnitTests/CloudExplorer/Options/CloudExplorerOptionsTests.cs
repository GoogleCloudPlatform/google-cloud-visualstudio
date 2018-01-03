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
