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

using Google.Apis.Pubsub.v1;
using Google.Apis.Pubsub.v1.Data;
using GoogleCloudExtension.CloudExplorerSources.PubSub;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources.UnitTests
{
    /// <summary>
    /// Test class for <see cref="PubsubDataSource"/>
    /// </summary>
    [TestClass]
    public class PubSubDataSourceUnitTests : DataSourceUnitTestsBase
    {
        private const string DummyString = "DummyString";
        private const string MockedTopicName = "MockedTopicName";
        private const string MockedSubscriptionName = "MockedSubscriptionName";
        private const string FirstName = "FirstName";
        private const string SecondName = "SecondName";
        private const string ProjectName = "Project";
        private const string ProjectResourceName = "projects/" + ProjectName;
        private const string TopicName = "NewTopicName";
        private const string TopicFullName = "projects/" + ProjectName + "/topics/" + TopicName;
        private const string SubscriptionName = "NewSubscriptionName";
        private const string SubscriptionFullName = "projects/" + ProjectName + "/subscriptions/" + SubscriptionName;

        private static readonly Subscription s_newSubscription =
            new Subscription { Name = SubscriptionName, Topic = TopicName };

        [TestMethod]
        public async Task TestGetTopicListAsyncSinglePage()
        {
            var responses = new[]
            {
                new ListTopicsResponse
                {
                    Topics = new List<Topic> {new Topic {Name = FirstName}}
                },
                new ListTopicsResponse
                {
                    Topics = new List<Topic> {new Topic {Name = SecondName}}
                }
            };
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Topics,
                t => t.List(It.IsAny<string>()),
                new[] { DummyString },
                responses);

            var sourceUnderTest = new PubsubDataSource(service, ProjectName);
            IList<Topic> topics = await sourceUnderTest.GetTopicListAsync();

            Assert.AreEqual(1, topics.Count);
            Assert.AreEqual(FirstName, topics[0].Name);
            var topicsMock = Mock.Get(service.Projects.Topics);
            topicsMock.Verify(t => t.List(ProjectResourceName), Times.AtLeastOnce);
            topicsMock.Verify(t => t.List(It.IsNotIn(ProjectResourceName)), Times.Never);
        }

        [TestMethod]
        public async Task TestGetTopicListAsyncMultiPage()
        {
            var responses = new[]
            {
                new ListTopicsResponse
                {
                    NextPageToken = "Token",
                    Topics = new List<Topic> {new Topic {Name = FirstName}}
                },
                new ListTopicsResponse
                {
                    Topics = new List<Topic> {new Topic {Name = SecondName}}
                }
            };
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Topics,
                t => t.List(It.IsAny<string>()),
                new[] { DummyString }, responses);

            var sourceUnderTest = new PubsubDataSource(service, ProjectName);
            IList<Topic> topics = await sourceUnderTest.GetTopicListAsync();

            Assert.AreEqual(2, topics.Count);
            Assert.AreEqual(FirstName, topics[0].Name);
            Assert.AreEqual(SecondName, topics[1].Name);
            var topicsMock = Mock.Get(service.Projects.Topics);
            topicsMock.Verify(t => t.List(ProjectResourceName), Times.AtLeastOnce);
            topicsMock.Verify(t => t.List(It.IsNotIn(ProjectResourceName)), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(DataSourceException), MockExceptionMessage)]
        public async Task TestGetTopicListAsyncException()
        {
            var responses = new ListTopicsResponse[0];
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Topics,
                t => t.List(It.IsAny<string>()),
                new[] { DummyString }, responses);
            var sourceUnderTest = new PubsubDataSource(service, ProjectName);

            try
            {
                await sourceUnderTest.GetTopicListAsync();
                Assert.Fail();
            }
            finally
            {
                var topicsMock = Mock.Get(service.Projects.Topics);
                topicsMock.Verify(t => t.List(ProjectResourceName), Times.AtLeastOnce);
                topicsMock.Verify(t => t.List(It.IsNotIn(ProjectResourceName)), Times.Never);
            }
        }

        [TestMethod]
        public async Task TestGetSubscriptionListAsyncSinglePage()
        {
            var responses = new[]
            {
                new ListSubscriptionsResponse
                {
                    Subscriptions = new List<Subscription> {new Subscription {Name = FirstName}}
                },
                new ListSubscriptionsResponse
                {
                    Subscriptions = new List<Subscription> {new Subscription {Name = SecondName}}
                }
            };
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Subscriptions,
                s => s.List(It.IsAny<string>()),
                new[] { DummyString }, responses);
            var sourceUnderTest = new PubsubDataSource(service, ProjectName);

            IList<Subscription> subscriptions = await sourceUnderTest.GetSubscriptionListAsync();

            Assert.AreEqual(1, subscriptions.Count);
            Assert.AreEqual(FirstName, subscriptions[0].Name);
            var subscriptionsMock = Mock.Get(service.Projects.Subscriptions);
            subscriptionsMock.Verify(s => s.List(ProjectResourceName), Times.AtLeastOnce);
            subscriptionsMock.Verify(s => s.List(It.IsNotIn(ProjectResourceName)), Times.Never);
        }

        [TestMethod]
        public async Task TestGetSubscriptionListAsyncPaged()
        {
            var responses = new[]
            {
                new ListSubscriptionsResponse
                {
                    NextPageToken = "Token",
                    Subscriptions = new List<Subscription> {new Subscription {Name = FirstName}}
                },
                new ListSubscriptionsResponse
                {
                    Subscriptions = new List<Subscription> {new Subscription {Name = SecondName}}
                }
            };
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Subscriptions,
                s => s.List(It.IsAny<string>()),
                new[] { DummyString }, responses);
            var sourceUnderTest = new PubsubDataSource(service, ProjectName);

            IList<Subscription> subscriptions = await sourceUnderTest.GetSubscriptionListAsync();

            Assert.AreEqual(2, subscriptions.Count);
            Assert.AreEqual(FirstName, subscriptions[0].Name);
            Assert.AreEqual(SecondName, subscriptions[1].Name);
            var subscriptionsMock = Mock.Get(service.Projects.Subscriptions);
            subscriptionsMock.Verify(s => s.List(ProjectResourceName), Times.AtLeastOnce);
            subscriptionsMock.Verify(s => s.List(It.IsNotIn(ProjectResourceName)), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(DataSourceException), MockExceptionMessage)]
        public async Task TestGetSubscriptionListAsyncException()
        {
            var responses = new ListSubscriptionsResponse[0];
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Subscriptions,
                s => s.List(It.IsAny<string>()),
                new[] { DummyString }, responses);
            var sourceUnderTest = new PubsubDataSource(service, ProjectName);

            try
            {
                await sourceUnderTest.GetSubscriptionListAsync();
                Assert.Fail();
            }
            finally
            {
                var subscriptionsMock = Mock.Get(service.Projects.Subscriptions);
                subscriptionsMock.Verify(s => s.List(ProjectResourceName), Times.AtLeastOnce);
                subscriptionsMock.Verify(s => s.List(It.IsNotIn(ProjectResourceName)), Times.Never);
            }
        }

        [TestMethod]
        public async Task TestNewTopicAsyncSuccess()
        {
            var responses = new[] { new Topic { Name = MockedTopicName } };
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Topics,
                t => t.Create(It.IsAny<Topic>(), It.IsAny<string>()),
                new object[] { new Topic(), DummyString }, responses);
            var sourceUnderTest = new PubsubDataSource(service, ProjectName);

            var topic = await sourceUnderTest.NewTopicAsync(TopicName);

            Assert.AreEqual(MockedTopicName, topic.Name);
            var topicMock = Mock.Get(service.Projects.Topics);
            topicMock.Verify(t => t.Create(It.IsAny<Topic>(), TopicFullName), Times.Once);
            topicMock.Verify(t => t.Create(It.IsAny<Topic>(), It.IsNotIn(TopicFullName)), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(DataSourceException), MockExceptionMessage)]
        public async Task TestNewTopicAsyncException()
        {
            var responses = new Topic[0];
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Topics,
                t => t.Create(It.IsAny<Topic>(), It.IsAny<string>()),
                new object[] { new Topic(), DummyString }, responses);
            var sourceUnderTest = new PubsubDataSource(service, ProjectName);

            try
            {
                await sourceUnderTest.NewTopicAsync(TopicName);
                Assert.Fail();
            }
            finally
            {
                var topicMock = Mock.Get(service.Projects.Topics);
                topicMock.Verify(t => t.Create(It.IsAny<Topic>(), TopicFullName), Times.Once);
                topicMock.Verify(t => t.Create(It.IsAny<Topic>(), It.IsNotIn(TopicFullName)), Times.Never);
            }
        }

        [TestMethod]
        public async Task TestDeleteTopicAsyncSuccess()
        {
            var responses = new[] { new Empty() };
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Topics,
                t => t.Delete(It.IsAny<string>()),
                new[] { DummyString }, responses);
            var sourceUnderTest = new PubsubDataSource(service, ProjectName);

            await sourceUnderTest.DeleteTopicAsync(TopicFullName);

            var topicMock = Mock.Get(service.Projects.Topics);
            topicMock.Verify(t => t.Delete(TopicFullName), Times.Once);
            topicMock.Verify(t => t.Delete(It.IsNotIn(TopicFullName)), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(DataSourceException), MockExceptionMessage)]
        public async Task TestDeleteTopicAsyncException()
        {
            var responses = new Empty[0];
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Topics,
                t => t.Delete(It.IsAny<string>()),
                new[] { DummyString }, responses);
            var sourceUnderTest = new PubsubDataSource(service, ProjectName);

            try
            {
                await sourceUnderTest.DeleteTopicAsync(TopicFullName);
                Assert.Fail();
            }
            finally
            {
                var topicMock = Mock.Get(service.Projects.Topics);
                topicMock.Verify(t => t.Delete(TopicFullName), Times.Once);
                topicMock.Verify(t => t.Delete(It.IsNotIn(TopicFullName)), Times.Never);
            }
        }

        [TestMethod]
        public async Task TestNewSubscriptionAsyncSuccess()
        {
            var responses = new[] { new Subscription { Name = MockedSubscriptionName, Topic = MockedTopicName } };
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Subscriptions,
                s => s.Create(It.IsAny<Subscription>(), It.IsAny<string>()),
                new object[] { new Subscription(), DummyString }, responses);
            var sourceUnderTest = new PubsubDataSource(service, ProjectName);

            var subscription = await sourceUnderTest.NewSubscriptionAsync(s_newSubscription);

            Assert.AreEqual(MockedSubscriptionName, subscription.Name);
            Assert.AreEqual(MockedTopicName, subscription.Topic);
            var subscriptionMock = Mock.Get(service.Projects.Subscriptions);
            subscriptionMock.Verify(s => s.Create(It.IsAny<Subscription>(), SubscriptionFullName), Times.Once);
            subscriptionMock.Verify(s => s.Create(It.IsAny<Subscription>(), It.IsNotIn(SubscriptionFullName)), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(DataSourceException), MockExceptionMessage)]
        public async Task TestNewSubscriptionAsyncException()
        {
            var responses = new Subscription[0];
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Subscriptions,
                s => s.Create(It.IsAny<Subscription>(), It.IsAny<string>()),
                new object[] { new Subscription(), DummyString }, responses);
            var sourceUnderTest = new PubsubDataSource(service, ProjectName);

            try
            {
                await sourceUnderTest.NewSubscriptionAsync(s_newSubscription);
                Assert.Fail();
            }
            finally
            {
                var subscriptionMock = Mock.Get(service.Projects.Subscriptions);
                subscriptionMock.Verify(s => s.Create(It.IsAny<Subscription>(), SubscriptionFullName), Times.Once);
                subscriptionMock.Verify(s => s.Create(It.IsAny<Subscription>(), It.IsNotIn(SubscriptionFullName)), Times.Never);
            }
        }

        [TestMethod]
        public async Task TestDeleteSubscriptionAsyncSuccess()
        {
            var responses = new[] { new Empty() };
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Subscriptions,
                s => s.Delete(It.IsAny<string>()),
                new[] { DummyString }, responses);
            var sourceUnderTest = new PubsubDataSource(service, ProjectName);

            await sourceUnderTest.DeleteSubscriptionAsync(SubscriptionFullName);

            var subscriptionsMock = Mock.Get(service.Projects.Subscriptions);
            subscriptionsMock.Verify(s => s.Delete(SubscriptionFullName), Times.Once);
            subscriptionsMock.Verify(s => s.Delete(It.IsNotIn(SubscriptionFullName)), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(DataSourceException), MockedSubscriptionName)]
        public async Task TestDeleteSubscriptionAsyncException()
        {
            var responses = new Empty[0];
            PubsubService service = GetMockedService(
                (PubsubService s) => s.Projects,
                p => p.Subscriptions,
                s => s.Delete(It.IsAny<string>()),
                new[] { DummyString }, responses);
            var sourceUnderTest = new PubsubDataSource(service, ProjectName);

            try
            {
                await sourceUnderTest.DeleteSubscriptionAsync(SubscriptionFullName);
                Assert.Fail();
            }
            finally
            {
                var subscriptionMock = Mock.Get(service.Projects.Subscriptions);
                subscriptionMock.Verify(s => s.Delete(SubscriptionFullName), Times.Once);
                subscriptionMock.Verify(s => s.Delete(It.IsNotIn(SubscriptionFullName)), Times.Never);
            }
        }
    }
}
