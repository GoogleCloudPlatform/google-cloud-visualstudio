// Copyright 2017 Google Inc. All Rights Reserved.
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.CloudExplorerSources.PubSub
{
    [TestClass]
    public class TopicViewModelBaseTests
    {
        private const string MockTopicFullName = PubSubSourceRootViewModelTests.MockTopicFullName;
        private const string MockSubscriptionFullName = PubSubSourceRootViewModelTests.MockSubscriptionFullName;
        private const string MockSubscriptionFullName2 = MockSubscriptionFullName + "2";
        private const string DeletedTopicName = PubSubSourceRootViewModelTests.DeletedTopicName;
        private const string MockTopicLeafName = PubSubSourceRootViewModelTests.MockTopicLeafName;
        private const string MockSubscriptionLeafName = PubSubSourceRootViewModelTests.MockSubscriptionLeafName;
        private const string MockExceptionMessage = PubSubSourceRootViewModelTests.MockExceptionMessage;
        private Mock<IPubsubSourceRootViewModel> _ownerMock;
        private Mock<ITopicItem> _itemMock;
        private readonly Subscription _childSubscription = new Subscription { Name = MockSubscriptionFullName, Topic = MockTopicFullName };
        private readonly Subscription _orphanSubscription = new Subscription { Name = MockSubscriptionFullName2, Topic = DeletedTopicName };
        private TaskCompletionSource<IList<Subscription>> _getSubscriptionListSource;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _ownerMock = new Mock<IPubsubSourceRootViewModel>();
            _getSubscriptionListSource = new TaskCompletionSource<IList<Subscription>>();
            _ownerMock.Setup(o => o.DataSource.GetSubscriptionListAsync()).Returns(_getSubscriptionListSource.Task);
            _itemMock = new Mock<ITopicItem>();
            _itemMock.Setup(i => i.DisplayName).Returns(MockTopicLeafName);
            _itemMock.Setup(i => i.FullName).Returns(MockTopicFullName);
        }

        [TestMethod]
        public void TestConstrutorEmptySubscriptions()
        {
            var objectUnderTest =
                new TestTopicViewModelBase(_ownerMock.Object, _itemMock.Object, new List<Subscription>());

            Assert.AreEqual(_ownerMock.Object.DataSource, objectUnderTest.DataSource);
            Assert.AreEqual(_ownerMock.Object.Context, objectUnderTest.Context);
            Assert.AreEqual((object)_itemMock.Object, objectUnderTest.Item);
            Assert.AreEqual(((ICloudExplorerItemSource)objectUnderTest).Item, objectUnderTest.Item);
            objectUnderTest.AssetOwnerEquals(_ownerMock.Object);
            Assert.AreEqual(0, objectUnderTest.Children.Count);
        }

        [TestMethod]
        public void TestConstructorFilteredSubscription()
        {
            var objectUnderTest =
                new TestTopicViewModelBase(
                    _ownerMock.Object, _itemMock.Object, new List<Subscription> { _orphanSubscription });

            Assert.AreEqual(0, objectUnderTest.Children.Count);
        }

        [TestMethod]
        public void TestConstructorChildSubscription()
        {
            var objectUnderTest =
                new TestTopicViewModelBase(
                    _ownerMock.Object, _itemMock.Object, new List<Subscription> { _childSubscription });

            Assert.AreEqual(1, objectUnderTest.Children.Count);
            var subscriptions = objectUnderTest.Children.OfType<SubscriptionViewModel>().ToList();
            Assert.AreEqual(1, subscriptions.Count);
            Assert.IsTrue(subscriptions.Single().Caption == MockSubscriptionLeafName);
        }

        [TestMethod]
        public void TestRefreshLoading()
        {
            var objectUnderTest =
                new TestTopicViewModelBase(
                    _ownerMock.Object, _itemMock.Object, new List<Subscription> { _childSubscription });

            var refreshTask = objectUnderTest.Refresh();

            Assert.IsFalse(refreshTask.IsCompleted);
            Assert.AreEqual(1, objectUnderTest.Children.Count);
            var loadingChild = objectUnderTest.Children.Single() as TreeLeaf;
            Assert.IsNotNull(loadingChild);
            Assert.IsTrue(loadingChild.IsLoading);
            Assert.AreEqual(Resources.CloudExplorerPubSubLoadingSubscriptionsCaption, loadingChild.Caption);
        }

        [TestMethod]
        public async Task TestRefreshError()
        {
            var objectUnderTest =
                new TestTopicViewModelBase(
                    _ownerMock.Object, _itemMock.Object, new List<Subscription> { _childSubscription });
            _getSubscriptionListSource.SetException(new DataSourceException(MockExceptionMessage));

            await objectUnderTest.Refresh();

            Assert.AreEqual(1, objectUnderTest.Children.Count);
            var errorChild = objectUnderTest.Children.Single() as TreeLeaf;
            Assert.IsNotNull(errorChild);
            Assert.IsTrue(errorChild.IsError);
            Assert.AreEqual(Resources.CloudExplorerPubSubListSubscriptionsErrorCaption, errorChild.Caption);
        }

        [TestMethod]
        public async Task TestRefreshEmpty()
        {
            var objectUnderTest =
                new TestTopicViewModelBase(
                    _ownerMock.Object, _itemMock.Object, new List<Subscription> { _childSubscription });
            _getSubscriptionListSource.SetResult(new List<Subscription>());

            await objectUnderTest.Refresh();

            Assert.AreEqual(0, objectUnderTest.Children.Count);
        }

        [TestMethod]
        public async Task TestRefreshFiltered()
        {
            var objectUnderTest =
                new TestTopicViewModelBase(
                    _ownerMock.Object, _itemMock.Object, new List<Subscription> { _childSubscription });
            _getSubscriptionListSource.SetResult(new List<Subscription> { _orphanSubscription });

            await objectUnderTest.Refresh();

            Assert.AreEqual(0, objectUnderTest.Children.Count);
        }

        [TestMethod]
        public async Task TestRefreshChild()
        {
            var objectUnderTest =
                new TestTopicViewModelBase(
                    _ownerMock.Object, _itemMock.Object, new List<Subscription>());
            _getSubscriptionListSource.SetResult(new List<Subscription> { _childSubscription });

            await objectUnderTest.Refresh();

            Assert.AreEqual(1, objectUnderTest.Children.Count);
            var subscriptions = objectUnderTest.Children.OfType<SubscriptionViewModel>().ToList();
            Assert.AreEqual(1, subscriptions.Count);
            Assert.IsTrue(subscriptions.Single().Caption == MockSubscriptionLeafName);
        }

        [TestMethod]
        public async Task TestDoubleRefresh()
        {
            var objectUnderTest =
                new TestTopicViewModelBase(
                    _ownerMock.Object, _itemMock.Object, new List<Subscription>());

            var firstTask = objectUnderTest.Refresh();
            var secondTask = objectUnderTest.Refresh();
            _getSubscriptionListSource.SetResult(new List<Subscription>());
            await firstTask;
            await secondTask;

            _ownerMock.Verify(o => o.DataSource.GetSubscriptionListAsync(), Times.Once);
        }

        private class TestTopicViewModelBase : TopicViewModelBase
        {
            public TestTopicViewModelBase(
                IPubsubSourceRootViewModel owner,
                ITopicItem item,
                IEnumerable<Subscription> subscriptions) : base(owner, item, subscriptions)
            { }

            public void AssetOwnerEquals(IPubsubSourceRootViewModel ownerMockObject)
            {
                Assert.AreEqual(ownerMockObject, Owner);
            }
        }
    }
}
