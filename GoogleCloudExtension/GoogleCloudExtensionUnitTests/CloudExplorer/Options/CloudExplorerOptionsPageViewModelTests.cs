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
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

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

            _objectUnderTest.PubSubTopicFilters = new ObservableCollection<EditableModel<string>>();

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

            Assert.IsNull(_objectUnderTest.PubSubTopicFilters);
        }

        [TestMethod]
        public void TestSetEmptyFilters()
        {
            _objectUnderTest.PubSubTopicFilters = new ObservableCollection<EditableModel<string>>();

            Assert.IsNotNull(_objectUnderTest.PubSubTopicFilters);
            Assert.AreEqual(0, _objectUnderTest.PubSubTopicFilters.Count);
        }
    }
}
