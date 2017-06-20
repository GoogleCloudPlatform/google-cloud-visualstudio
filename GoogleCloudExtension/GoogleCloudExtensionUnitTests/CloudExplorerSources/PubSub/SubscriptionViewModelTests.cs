﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.CloudExplorerSources.PubSub;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.UserPrompt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace GoogleCloudExtensionUnitTests.CloudExplorerSources.PubSub
{
    [TestClass]
    public class SubscriptionViewModelTests
    {
        private const string MockSubscriptionLeafName = TopicViewModelTests.MockSubscriptionLeafName;
        private const string MockSubscriptionFullName = TopicViewModelTests.MockSubscriptionFullName;
        private const string MockTopicFullName = TopicViewModelTests.MockTopicFullName;
        private const string MockExceptionMessage = TopicViewModelTests.MockExceptionMessage;

        private SubscriptionViewModel _objectUnderTest;
        private Mock<ITopicViewModelBase> _ownerMock;
        private Mock<Subscription> _subscriptionItemMock;
        private List<UserPromptWindow.Options> _promptOptions;
        private bool _promptReturnValue;
        private TaskCompletionSource<object> _deleteSubscriptionSource;
        private TaskCompletionSource<object> _refreshSource;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _promptOptions = new List<UserPromptWindow.Options>();
            _promptReturnValue = true;
            UserPromptWindow.PromptUserFunction = options =>
            {
                _promptOptions.Add(options);
                return _promptReturnValue;
            };

            _deleteSubscriptionSource = new TaskCompletionSource<object>();
            _refreshSource = new TaskCompletionSource<object>();

            _ownerMock = new Mock<ITopicViewModelBase>();
            _ownerMock.Setup(o => o.Context.ShowPropertiesWindow(It.IsAny<object>()));
            _ownerMock.Setup(o => o.DataSource.DeleteSubscriptionAsync(It.IsAny<string>()))
                .Returns(_deleteSubscriptionSource.Task);
            _ownerMock.Setup(o => o.Refresh()).Returns(_refreshSource.Task);

            _subscriptionItemMock = new Mock<Subscription>();
            _subscriptionItemMock.Setup(s => s.Name).Returns(MockSubscriptionFullName);
            _subscriptionItemMock.Setup(s => s.Topic).Returns(MockTopicFullName);

            _objectUnderTest = new SubscriptionViewModel(_ownerMock.Object, _subscriptionItemMock.Object);
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            Assert.IsInstanceOfType(((ICloudExplorerItemSource)_objectUnderTest).Item, typeof(SubscriptionItem));
            Assert.AreEqual(MockSubscriptionLeafName, _objectUnderTest.Caption);
            var icon = _objectUnderTest.Icon as BitmapImage;
            Assert.IsNotNull(icon);
            Assert.IsTrue(icon.UriSource.ToString().EndsWith("subscription_icon.png"));
            var menuItems = _objectUnderTest.ContextMenu.ItemsSource.Cast<MenuItem>().ToList();
            Assert.AreEqual(2, menuItems.Count);
            Assert.AreEqual(2, menuItems.Select(mi => mi.Command).Distinct().Count());
            Assert.AreEqual(
                2,
                menuItems.Select(mi => mi.Header).Intersect(
                        new List<object>
                        {
                            Resources.CloudExplorerPubSubDeleteSubscriptionMenuHeader,
                            Resources.UiPropertiesMenuHeader
                        })
                    .Distinct()
                    .Count());
            Assert.IsFalse(_objectUnderTest.IsLoading);
            Assert.AreEqual(0, _promptOptions.Count);
            _ownerMock.Verify(o => o.Context.ShowPropertiesWindow(It.IsAny<object>()), Times.Never);
        }

        [TestMethod]
        public void TestDeleteSubscriptionCanceled()
        {
            _promptReturnValue = false;

            _objectUnderTest.OnDeleteSubscriptionCommand();

            Assert.IsFalse(_objectUnderTest.IsLoading);
            Assert.AreEqual(1, _promptOptions.Count);
            var deletePromptOption = _promptOptions.Single();
            Assert.AreEqual(Resources.PubSubDeleteSubscriptionWindowHeader, deletePromptOption.Title);
            Assert.AreEqual(Resources.UiDeleteButtonCaption, deletePromptOption.ActionButtonCaption);
            Assert.AreEqual(
                string.Format(Resources.PubSubDeleteSubscriptionWindowMessage, MockSubscriptionLeafName),
                deletePromptOption.Prompt);
            _ownerMock.Verify(o => o.DataSource.DeleteSubscriptionAsync(It.IsAny<string>()), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Never);
        }

        [TestMethod]
        public void TestDeleteSubscriptionDeleting()
        {
            _objectUnderTest.OnDeleteSubscriptionCommand();

            Assert.IsTrue(_objectUnderTest.IsLoading);
            Assert.AreEqual(1, _promptOptions.Count);
            _ownerMock.Verify(o => o.DataSource.DeleteSubscriptionAsync(MockSubscriptionFullName), Times.Once);
            _ownerMock.Verify(o => o.DataSource.DeleteSubscriptionAsync(It.IsNotIn(MockSubscriptionFullName)), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Never);
        }

        [TestMethod]
        public void TestDeleteSubscriptionError()
        {
            _deleteSubscriptionSource.SetException(new DataSourceException(MockExceptionMessage));

            _objectUnderTest.OnDeleteSubscriptionCommand();

            Assert.IsTrue(_objectUnderTest.IsLoading);
            Assert.AreEqual(2, _promptOptions.Count);
            var errorPromptOptions = _promptOptions.Skip(1).Single();
            Assert.AreEqual(MockExceptionMessage, errorPromptOptions.ErrorDetails);
            Assert.AreEqual(Resources.PubSubDeleteSubscriptionErrorHeader, errorPromptOptions.Title);
            Assert.AreEqual(Resources.PubSubDeleteSubscriptionErrorMessage, errorPromptOptions.Prompt);
            _ownerMock.Verify(o => o.DataSource.DeleteSubscriptionAsync(MockSubscriptionFullName), Times.Once);
            _ownerMock.Verify(o => o.DataSource.DeleteSubscriptionAsync(It.IsNotIn(MockSubscriptionFullName)), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Once);
        }

        [TestMethod]
        public void TestDeleteSubscriptionErrorRefreshed()
        {
            _deleteSubscriptionSource.SetException(new DataSourceException(MockExceptionMessage));
            _refreshSource.SetResult(null);

            _objectUnderTest.OnDeleteSubscriptionCommand();

            Assert.IsFalse(_objectUnderTest.IsLoading);
            Assert.AreEqual(2, _promptOptions.Count);
            var errorPromptOptions = _promptOptions.Skip(1).Single();
            Assert.AreEqual(MockExceptionMessage, errorPromptOptions.ErrorDetails);
            Assert.AreEqual(Resources.PubSubDeleteSubscriptionErrorHeader, errorPromptOptions.Title);
            Assert.AreEqual(Resources.PubSubDeleteSubscriptionErrorMessage, errorPromptOptions.Prompt);
            _ownerMock.Verify(o => o.DataSource.DeleteSubscriptionAsync(MockSubscriptionFullName), Times.Once);
            _ownerMock.Verify(o => o.DataSource.DeleteSubscriptionAsync(It.IsNotIn(MockSubscriptionFullName)), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Once);
        }

        [TestMethod]
        public void TestDeleteSubscriptionRefreshing()
        {
            _deleteSubscriptionSource.SetResult(null);

            _objectUnderTest.OnDeleteSubscriptionCommand();

            Assert.IsTrue(_objectUnderTest.IsLoading);
            Assert.AreEqual(1, _promptOptions.Count);
            _ownerMock.Verify(o => o.DataSource.DeleteSubscriptionAsync(MockSubscriptionFullName), Times.Once);
            _ownerMock.Verify(o => o.DataSource.DeleteSubscriptionAsync(It.IsNotIn(MockSubscriptionFullName)), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Once);
        }

        [TestMethod]
        public void TestDeleteSubscriptionSuccess()
        {
            _deleteSubscriptionSource.SetResult(null);
            _refreshSource.SetResult(null);

            _objectUnderTest.OnDeleteSubscriptionCommand();

            Assert.IsFalse(_objectUnderTest.IsLoading);
            Assert.AreEqual(1, _promptOptions.Count);
            _ownerMock.Verify(o => o.DataSource.DeleteSubscriptionAsync(MockSubscriptionFullName), Times.Once);
            _ownerMock.Verify(o => o.DataSource.DeleteSubscriptionAsync(It.IsNotIn(MockSubscriptionFullName)), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Once);
        }

        [TestMethod]
        public void TestPropertiesWindowCommand()
        {
            _objectUnderTest.OnPropertiesWindowCommand();

            ICloudExplorerItemSource sourceUnderTest = _objectUnderTest;
            _ownerMock.Verify(o => o.Context.ShowPropertiesWindow(sourceUnderTest.Item), Times.Once);
            _ownerMock.Verify(o => o.Context.ShowPropertiesWindow(It.IsNotIn(sourceUnderTest.Item)), Times.Never);
        }
    }
}
