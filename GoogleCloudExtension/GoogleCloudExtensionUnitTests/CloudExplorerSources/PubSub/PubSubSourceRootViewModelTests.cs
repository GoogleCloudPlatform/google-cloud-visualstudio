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
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoogleCloudExtensionUnitTests.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Tests for <see cref="PubsubSourceRootViewModel"/>
    /// </summary>
    [TestClass]
    public class PubSubSourceRootViewModelTests
    {
        private const string MockExceptionMessage = SourceRootViewModelBaseTests.MockExceptionMessage;
        private const string MockProjectId = SourceRootViewModelBaseTests.MockProjectId;
        private const string MockTopicName = "MockTopic";
        private const string MockSubscriptionName = "MockSubscription";
        private const string TopicPrefix = "projects/" + MockProjectId + "/topics/";

        /// <summary>
        /// Defined by the pub sub api.
        /// <see href="https://cloud.google.com/pubsub/docs/reference/rest/v1/projects.topics/delete"/>
        /// </summary>
        private const string DeletedTopicName = "_deleted-topic_";

        private static readonly Topic s_topic = new Topic { Name = MockTopicName };

        private static readonly Subscription s_childSubscription = new Subscription
        {
            Topic = MockTopicName,
            Name = MockSubscriptionName
        };

        private static readonly Subscription s_orphanedSubscription = new Subscription
        {
            Topic = DeletedTopicName,
            Name = MockSubscriptionName
        };

        private Mock<IPubsubDataSource> _dataSourceMock;
        private Mock<ICloudSourceContext> _contextMock;
        private Mock<Func<IPubsubDataSource>> _factoryMock;
        private TaskCompletionSource<IList<Topic>> _topicSource;
        private TaskCompletionSource<IList<Subscription>> _subscriptionSource;
        private TestablePubsubSourceRootViewModel _objectUnderTest;

        [TestInitialize]
        public void MyTestInitialize()
        {
            _dataSourceMock = new Mock<IPubsubDataSource>();
            _contextMock = new Mock<ICloudSourceContext>();
            _contextMock.Setup(c => c.CurrentProject.ProjectId).Returns(MockProjectId);
            _factoryMock = new Mock<Func<IPubsubDataSource>>();
            _factoryMock.Setup(f => f()).Returns(() => _dataSourceMock.Object);
            _objectUnderTest = new TestablePubsubSourceRootViewModel(_factoryMock.Object);

            _topicSource = new TaskCompletionSource<IList<Topic>>();
            _dataSourceMock.Setup(ds => ds.GetTopicListAsync()).Returns(() => _topicSource.Task);

            _subscriptionSource = new TaskCompletionSource<IList<Subscription>>();
            _dataSourceMock.Setup(ds => ds.GetSubscriptionListAsync()).Returns(() => _subscriptionSource.Task);
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            Assert.AreEqual(0, _objectUnderTest.Children.Count);
            _factoryMock.Verify(x => x(), Times.Never);

            var loadingPlaceholder = _objectUnderTest.LoadingPlaceholder;
            Assert.AreEqual(Resources.CloudExplorerPubSubLoadingTopicsCaption, loadingPlaceholder.Caption);
            Assert.IsTrue(loadingPlaceholder.IsLoading);
            Assert.IsFalse(loadingPlaceholder.IsWarning);
            Assert.IsFalse(loadingPlaceholder.IsError);
            var noItemsPlaceholder = _objectUnderTest.NoItemsPlaceholder;
            Assert.AreEqual(Resources.CloudExplorerPubSubNoTopicsFoundCaption, noItemsPlaceholder.Caption);
            Assert.IsFalse(noItemsPlaceholder.IsLoading);
            Assert.IsTrue(noItemsPlaceholder.IsWarning);
            Assert.IsFalse(noItemsPlaceholder.IsError);
            var errorPlaceholder = _objectUnderTest.ErrorPlaceholder;
            Assert.AreEqual(Resources.CloudExplorerPubSubListTopicsErrorCaption, errorPlaceholder.Caption);
            Assert.IsFalse(errorPlaceholder.IsLoading);
            Assert.IsFalse(errorPlaceholder.IsWarning);
            Assert.IsTrue(errorPlaceholder.IsError);
            Assert.AreEqual(Resources.CloudExplorerPubSubRootCaption, _objectUnderTest.RootCaption);
        }

        [TestMethod]
        public void TestInitialize()
        {
            _objectUnderTest.Initialize(_contextMock.Object);

            var menuItems = _objectUnderTest.ContextMenu.ItemsSource.Cast<MenuItem>().ToList();
            Assert.AreEqual(2, menuItems.Count);
            Assert.AreEqual(Resources.CloudExplorerPubSubNewTopicMenuHeader, menuItems[0].Header);
            Assert.IsTrue(((ProtectedCommand)menuItems[0].Command).CanExecuteCommand);
            Assert.AreEqual(Resources.UiOpenOnCloudConsoleMenuHeader, menuItems[1].Header);
            Assert.IsTrue(((ProtectedCommand)menuItems[1].Command).CanExecuteCommand);
        }

        [TestMethod]
        public void TestDataSource()
        {
            Assert.IsNotNull(_objectUnderTest.DataSource);
            _factoryMock.Verify(x => x(), Times.Once);
        }

        [TestMethod]
        public void TestInvalidateProjectOrAccount()
        {
            // Testing side effects of Property.
            var _ = _objectUnderTest.DataSource;

            _objectUnderTest.InvalidateProjectOrAccount();

            _factoryMock.Verify(x => x(), Times.Once);
            Assert.IsNotNull(_objectUnderTest.DataSource);
            _factoryMock.Verify(x => x(), Times.Exactly(2));
        }

        [TestMethod]
        [ExpectedException(typeof(CloudExplorerSourceException))]
        public async Task TestLoadingTopicsError()
        {
            // Test that the children are not cleared.
            _objectUnderTest.Children.Add(new TreeNode());
            _topicSource.SetException(new DataSourceException(MockExceptionMessage));
            _subscriptionSource.SetResult(new List<Subscription>());

            try
            {
                await _objectUnderTest.LoadData();
            }
            finally
            {
                Assert.AreEqual(1, _objectUnderTest.Children.Count);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(CloudExplorerSourceException))]
        public async Task TestLoadingSubscriptionsError()
        {
            // Test that the children are not cleared.
            _objectUnderTest.Children.Add(new TreeNode());
            _topicSource.SetResult(new List<Topic>());
            _subscriptionSource.SetException(new DataSourceException(MockExceptionMessage));

            try
            {
                await _objectUnderTest.LoadData();
            }
            finally
            {
                Assert.AreEqual(1, _objectUnderTest.Children.Count);
            }
        }

        [TestMethod]
        public async Task TestLoadNoTopics()
        {
            // Test that the children are cleared.
            _objectUnderTest.Children.Add(new TreeNode());
            _topicSource.SetResult(new List<Topic>());
            _subscriptionSource.SetResult(new List<Subscription>());

            await _objectUnderTest.LoadData();

            Assert.AreEqual(0, _objectUnderTest.Children.Count);
        }

        [TestMethod]
        public async Task TestLoadParentChild()
        {
            _objectUnderTest.Initialize(_contextMock.Object);
            _topicSource.SetResult(new List<Topic> { s_topic });
            _subscriptionSource.SetResult(new List<Subscription> { s_childSubscription });

            await _objectUnderTest.LoadData();

            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            var child = _objectUnderTest.Children[0] as TopicViewModel;
            Assert.IsNotNull(child);
            Assert.AreEqual(MockTopicName, child.Caption);
            Assert.AreEqual(1, child.Children.Count);
            var subChild = child.Children[0] as SubscriptionViewModel;
            Assert.IsNotNull(subChild);
            Assert.AreEqual(MockSubscriptionName, subChild.Caption);
        }

        [TestMethod]
        public async Task TestLoadOrphanedSubscription()
        {
            _objectUnderTest.Initialize(_contextMock.Object);
            _topicSource.SetResult(new List<Topic> { s_topic });
            _subscriptionSource.SetResult(new List<Subscription> { s_orphanedSubscription });

            await _objectUnderTest.LoadData();

            Assert.AreEqual(2, _objectUnderTest.Children.Count);
            var topicChild = _objectUnderTest.Children[0] as TopicViewModel;
            Assert.IsNotNull(topicChild);
            Assert.AreEqual(MockTopicName, topicChild.Caption);
            Assert.AreEqual(0, topicChild.Children.Count);
            var orphanedHolder = _objectUnderTest.Children[1] as OrphanedSubscriptionsViewModel;
            Assert.IsNotNull(orphanedHolder);
            Assert.AreEqual(1, orphanedHolder.Children.Count);
            var subChild = orphanedHolder.Children[0] as SubscriptionViewModel;
            Assert.IsNotNull(subChild);
            Assert.AreEqual(MockSubscriptionName, subChild.Caption);
        }

        [TestMethod]
        public async Task TestLoadBlacklistedTopics()
        {
            _objectUnderTest.Initialize(_contextMock.Object);
            _topicSource.SetResult(
                new List<Topic>
                {
                    new Topic {Name = $"{TopicPrefix}cloud-builds"},
                    new Topic {Name = $"{TopicPrefix}repository-changes.default"},
                    new Topic {Name = $"{TopicPrefix}asia.gcr.io%2F{MockProjectId}"},
                    new Topic {Name = $"{TopicPrefix}eu.gcr.io%2F{MockProjectId}"},
                    new Topic {Name = $"{TopicPrefix}gcr.io%2F{MockProjectId}"},
                    new Topic {Name = $"{TopicPrefix}us.gcr.io%2F{MockProjectId}"}
                });
            _subscriptionSource.SetResult(new List<Subscription>());

            await _objectUnderTest.LoadData();

            Assert.AreEqual(0, _objectUnderTest.Children.Count);
        }

        [TestMethod]
        public void TestNewTopicCommandCanceled()
        {
            string projectIdParam = null;
            string details = null;
            _dataSourceMock.Setup(ds => ds.NewTopicAsync(It.IsAny<string>())).Returns(Task.FromResult(s_topic));
            _objectUnderTest.Initialize(_contextMock.Object);
            _objectUnderTest.NewTopicUserPrompt = projectId =>
            {
                projectIdParam = projectId;
                return null;
            };
            _objectUnderTest.ErrorPrompt = (message, header, errorDetails) =>
            {
                details = errorDetails;
            };

            _objectUnderTest.OnNewTopicCommand();

            Assert.AreEqual(MockProjectId, projectIdParam);
            Assert.IsNull(details);
            Assert.AreEqual(0, _objectUnderTest.RefreshHitCount);
            _dataSourceMock.Verify(ds => ds.NewTopicAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestNewTopicCommandError()
        {
            string projectIdParam = null;
            string details = null;
            _dataSourceMock.Setup(ds => ds.NewTopicAsync(It.IsAny<string>()))
                .Throws(new DataSourceException(MockExceptionMessage));
            _objectUnderTest.Initialize(_contextMock.Object);
            _objectUnderTest.NewTopicUserPrompt = projectId =>
            {
                projectIdParam = projectId;
                return MockTopicName;
            };
            _objectUnderTest.ErrorPrompt = (message, header, errorDetails) =>
            {
                details = errorDetails;
            };

            _objectUnderTest.OnNewTopicCommand();

            Assert.AreEqual(MockProjectId, projectIdParam);
            Assert.AreEqual(MockExceptionMessage, details);
            Assert.AreEqual(0, _objectUnderTest.RefreshHitCount);
            _dataSourceMock.Verify(ds => ds.NewTopicAsync(MockTopicName), Times.Once);
            _dataSourceMock.Verify(ds => ds.NewTopicAsync(It.IsNotIn(MockTopicName)), Times.Never);
        }

        [TestMethod]
        public void TestNewTopicCommandSuccess()
        {
            string projectIdParam = null;
            string details = null;
            _dataSourceMock.Setup(ds => ds.NewTopicAsync(It.IsAny<string>())).Returns(Task.FromResult(s_topic));
            _objectUnderTest.Initialize(_contextMock.Object);
            _objectUnderTest.NewTopicUserPrompt = projectId =>
            {
                projectIdParam = projectId;
                return MockTopicName;
            };
            _objectUnderTest.ErrorPrompt = (message, header, errorDetails) =>
            {
                details = errorDetails;
            };

            _objectUnderTest.OnNewTopicCommand();

            Assert.AreEqual(MockProjectId, projectIdParam);
            Assert.IsNull(details);
            Assert.AreEqual(1, _objectUnderTest.RefreshHitCount);
            _dataSourceMock.Verify(ds => ds.NewTopicAsync(MockTopicName), Times.Once);
            _dataSourceMock.Verify(ds => ds.NewTopicAsync(It.IsNotIn(MockTopicName)), Times.Never);
        }

        [TestMethod]
        public void TestOpenCloudConsoleCommand()
        {
            string target = null;
            _objectUnderTest.StartProcess = t =>
            {
                target = t;
                return null;
            };
            _objectUnderTest.Initialize(_contextMock.Object);

            _objectUnderTest.OnOpenCloudConsoleCommand();

            Assert.AreEqual(string.Format(PubsubSourceRootViewModel.PubSubConsoleUrlFormat, MockProjectId), target);
        }

        private class TestablePubsubSourceRootViewModel : PubsubSourceRootViewModel
        {
            // Avoid having to enable pack urls.
            private readonly Mock<BitmapSource> _rootIconMock = new Mock<BitmapSource>();

            public override ImageSource RootIcon => _rootIconMock.Object;

            public int RefreshHitCount { get; private set; }

            public TestablePubsubSourceRootViewModel(Func<IPubsubDataSource> factory) : base(factory) { }

            internal Task LoadData()
            {
                return LoadDataOverride();
            }

            public override void Refresh()
            {
                RefreshHitCount++;
            }
        }
    }
}
