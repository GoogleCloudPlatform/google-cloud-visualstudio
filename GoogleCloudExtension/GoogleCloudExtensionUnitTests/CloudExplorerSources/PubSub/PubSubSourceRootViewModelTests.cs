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
using GoogleCloudExtension.CloudExplorer.Options;
using GoogleCloudExtension.CloudExplorerSources.PubSub;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Options;
using GoogleCloudExtension.UserPrompt;
using GoogleCloudExtensionUnitTests.CloudExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace GoogleCloudExtensionUnitTests.CloudExplorerSources.PubSub
{
    /// <summary>
    /// Tests for <see cref="PubsubSourceRootViewModel"/>
    /// </summary>
    [TestClass]
    public class PubSubSourceRootViewModelTests : ExtensionTestBase
    {
        public const string MockTopicFullName = TopicPrefix + "MockTopic";
        public const string MockSubscriptionLeafName = "MockSubscription";
        public const string MockSubscriptionFullName = SubscriptionPrefix + MockSubscriptionLeafName;

        public const string MockExceptionMessage = SourceRootViewModelBaseTests.MockExceptionMessage;
        private const string ProjectResourcePrefix = "projects/" + "parent.com:mock-project";
        private const string TopicPrefix = ProjectResourcePrefix + "/topics/";
        private const string SubscriptionPrefix = ProjectResourcePrefix + "/subscriptions/";


        /// <summary>
        /// Defined by the Pub/Sub api.
        /// <see href="https://cloud.google.com/pubsub/docs/reference/rest/v1/projects.topics/delete"/>
        /// </summary>
        public const string DeletedTopicName = "_deleted-topic_";


        private static readonly Topic s_topic = new Topic { Name = MockTopicFullName };

        private static readonly Subscription s_childSubscription = new Subscription
        {
            Topic = MockTopicFullName,
            Name = MockSubscriptionFullName
        };

        private static readonly Subscription s_orphanedSubscription = new Subscription
        {
            Topic = DeletedTopicName,
            Name = MockSubscriptionFullName
        };

        private Mock<IPubsubDataSource> _dataSourceMock;
        private Mock<ICloudSourceContext> _contextMock;
        private Mock<Func<IPubsubDataSource>> _factoryMock;
        private TaskCompletionSource<IList<Topic>> _topicSource;
        private TaskCompletionSource<IList<Subscription>> _subscriptionSource;
        private TestablePubsubSourceRootViewModel _objectUnderTest;
        private Mock<Func<string, Process>> _startProcessMock;
        private Mock<CloudExplorerOptions> _cloudExplorerOptionsMock;

        protected override void BeforeEach()
        {
            _startProcessMock = new Mock<Func<string, Process>>();

            _dataSourceMock = new Mock<IPubsubDataSource>();
            _contextMock = new Mock<ICloudSourceContext>();
            _contextMock.Setup(c => c.CurrentProject.ProjectId).Returns("parent.com:mock-project");
            _factoryMock = new Mock<Func<IPubsubDataSource>>();
            _factoryMock.Setup(f => f()).Returns(() => _dataSourceMock.Object);

            _cloudExplorerOptionsMock = new Mock<CloudExplorerOptions>(MockBehavior.Strict);
            _cloudExplorerOptionsMock.SetupSet(o => o.PubSubTopicFilters = It.IsAny<IEnumerable<string>>());
            PackageMock.Setup(p => p.GetDialogPage<CloudExplorerOptions>()).Returns(_cloudExplorerOptionsMock.Object);


            _objectUnderTest = new TestablePubsubSourceRootViewModel(_factoryMock.Object);
            _objectUnderTest.StartProcess = _startProcessMock.Object;

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

            TreeLeaf loadingPlaceholder = _objectUnderTest.LoadingPlaceholder;
            Assert.AreEqual(Resources.CloudExplorerPubSubLoadingTopicsCaption, loadingPlaceholder.Caption);
            Assert.IsTrue(loadingPlaceholder.IsLoading);
            Assert.IsFalse(loadingPlaceholder.IsWarning);
            Assert.IsFalse(loadingPlaceholder.IsError);
            TreeLeaf noItemsPlaceholder = _objectUnderTest.NoItemsPlaceholder;
            Assert.AreEqual(Resources.CloudExplorerPubSubNoTopicsFoundCaption, noItemsPlaceholder.Caption);
            Assert.IsFalse(noItemsPlaceholder.IsLoading);
            Assert.IsTrue(noItemsPlaceholder.IsWarning);
            Assert.IsFalse(noItemsPlaceholder.IsError);
            TreeLeaf errorPlaceholder = _objectUnderTest.ErrorPlaceholder;
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

            List<MenuItem> menuItems = _objectUnderTest.ContextMenu.ItemsSource.Cast<MenuItem>().ToList();
            CollectionAssert.AreEquivalent(
                new[]
                {
                    Resources.CloudExplorerPubSubNewTopicMenuHeader,
                    Resources.CloudExplorerPubSubChangeFiltersMenuHeader,
                    Resources.UiOpenOnCloudConsoleMenuHeader
                },
                menuItems.Select(mi => mi.Header).ToList());
        }

        [TestMethod]
        public void TestDataSource()
        {
            _factoryMock.Verify(x => x(), Times.Never);
            Assert.IsNotNull(_objectUnderTest.DataSource);
            _factoryMock.Verify(x => x(), Times.Once);
            Assert.IsNotNull(_objectUnderTest.DataSource);
            _factoryMock.Verify(x => x(), Times.Once);
        }

        [TestMethod]
        public void TestInvalidateProjectOrAccount()
        {
            // Testing side effects of Property.
            IPubsubDataSource unused = _objectUnderTest.DataSource;

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
            _cloudExplorerOptionsMock.SetupGet(o => o.PubSubTopicFilters).Returns(CloudExplorerOptions.DefaultPubSubTopicFilters);
            _objectUnderTest.Initialize(_contextMock.Object);
            _topicSource.SetResult(new List<Topic> { s_topic });
            _subscriptionSource.SetResult(new List<Subscription> { s_childSubscription });

            await _objectUnderTest.LoadData();

            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            var child = _objectUnderTest.Children[0] as TopicViewModel;
            Assert.IsNotNull(child);
            Assert.AreEqual("MockTopic", child.Caption);
            Assert.AreEqual(1, child.Children.Count);
            var subChild = child.Children[0] as SubscriptionViewModel;
            Assert.IsNotNull(subChild);
            Assert.AreEqual(MockSubscriptionLeafName, subChild.Caption);
        }

        [TestMethod]
        public async Task TestLoadOrphanedSubscription()
        {
            _cloudExplorerOptionsMock.SetupGet(o => o.PubSubTopicFilters).Returns(CloudExplorerOptions.DefaultPubSubTopicFilters);

            _objectUnderTest.Initialize(_contextMock.Object);
            _topicSource.SetResult(new List<Topic> { s_topic });
            _subscriptionSource.SetResult(new List<Subscription> { s_orphanedSubscription });

            await _objectUnderTest.LoadData();

            Assert.AreEqual(2, _objectUnderTest.Children.Count);
            var topicChild = _objectUnderTest.Children[0] as TopicViewModel;
            Assert.IsNotNull(topicChild);
            Assert.AreEqual("MockTopic", topicChild.Caption);
            Assert.AreEqual(0, topicChild.Children.Count);
            var orphanedHolder = _objectUnderTest.Children[1] as OrphanedSubscriptionsViewModel;
            Assert.IsNotNull(orphanedHolder);
            Assert.AreEqual(1, orphanedHolder.Children.Count);
            var subChild = orphanedHolder.Children[0] as SubscriptionViewModel;
            Assert.IsNotNull(subChild);
            Assert.AreEqual(MockSubscriptionLeafName, subChild.Caption);
        }

        [TestMethod]
        public async Task TestLoadBlacklistedTopics()
        {
            _cloudExplorerOptionsMock.SetupGet(o => o.PubSubTopicFilters).Returns(CloudExplorerOptions.DefaultPubSubTopicFilters);

            var analyticsOptionsMock = new Mock<AnalyticsOptions>();
            PackageMock.SetupGet(p => p.AnalyticsSettings).Returns(analyticsOptionsMock.Object);

            _objectUnderTest.Initialize(_contextMock.Object);
            const string mockProjectId = "parent.com:mock-project";
            string gcrProjectId = mockProjectId.Replace(":", "%2F");
            string nonBlacklistTopicName = $"{TopicPrefix}cloud-builds:projects:xxxx-id-xxx:topics";
            _topicSource.SetResult(
                new List<Topic>
                {
                    new Topic {Name = $"{TopicPrefix}cloud-builds"},
                    new Topic {Name = $"{TopicPrefix}repository-changes.default"},
                    new Topic {Name = $"{TopicPrefix}repository-changes.another-repo-name"},
                    new Topic {Name = $"{TopicPrefix}gcr.io%2F{mockProjectId}"},
                    new Topic {Name = $"{TopicPrefix}asia.gcr.io%2F{mockProjectId}"},
                    new Topic {Name = $"{TopicPrefix}eu.gcr.io%2F{mockProjectId}"},
                    new Topic {Name = $"{TopicPrefix}us.gcr.io%2F{mockProjectId}"},
                    new Topic {Name = $"{TopicPrefix}gcr.io%2F{gcrProjectId}"},
                    new Topic {Name = $"{TopicPrefix}asia.gcr.io%2F{gcrProjectId}"},
                    new Topic {Name = $"{TopicPrefix}eu.gcr.io%2F{gcrProjectId}"},
                    new Topic {Name = $"{TopicPrefix}us.gcr.io%2F{gcrProjectId}"},
                    new Topic {Name = nonBlacklistTopicName}
                });
            _subscriptionSource.SetResult(new List<Subscription>());

            await _objectUnderTest.LoadData();

            Assert.AreEqual(1, _objectUnderTest.Children.Count);
            var topicNode = _objectUnderTest.Children[0] as TopicViewModel;
            Assert.IsNotNull(topicNode);
            Assert.AreEqual(nonBlacklistTopicName, topicNode.Item.FullName);
        }

        [TestMethod]
        public async Task TestNewTopicCommandCanceled()
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
            UserPromptWindow.PromptUserFunction = options =>
            {
                details = options.ErrorDetails;
                return true;
            };

            await _objectUnderTest.OnNewTopicCommandAsync();

            Assert.AreEqual("parent.com:mock-project", projectIdParam);
            Assert.IsNull(details);
            Assert.AreEqual(0, _objectUnderTest.RefreshHitCount);
            _dataSourceMock.Verify(ds => ds.NewTopicAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task TestNewTopicCommandError()
        {
            string projectIdParam = null;
            string details = null;
            _dataSourceMock.Setup(ds => ds.NewTopicAsync(It.IsAny<string>()))
            .Throws(new DataSourceException(MockExceptionMessage));
            _objectUnderTest.Initialize(_contextMock.Object);
            _objectUnderTest.NewTopicUserPrompt = projectId =>
            {
                projectIdParam = projectId;
                return MockTopicFullName;
            };
            UserPromptWindow.PromptUserFunction = options =>
            {
                details = options.ErrorDetails;
                return true;
            };

            await _objectUnderTest.OnNewTopicCommandAsync();

            Assert.AreEqual("parent.com:mock-project", projectIdParam);
            Assert.AreEqual(MockExceptionMessage, details);
            Assert.AreEqual(0, _objectUnderTest.RefreshHitCount);
            _dataSourceMock.Verify(ds => ds.NewTopicAsync(MockTopicFullName), Times.Once);
            _dataSourceMock.Verify(ds => ds.NewTopicAsync(It.IsNotIn(MockTopicFullName)), Times.Never);
        }

        [TestMethod]
        public async Task TestNewTopicCommandSuccess()
        {
            var analyticsOptionsMock = new Mock<AnalyticsOptions>();
            PackageMock.SetupGet(p => p.AnalyticsSettings).Returns(analyticsOptionsMock.Object);

            string projectIdParam = null;
            string details = null;
            _dataSourceMock.Setup(ds => ds.NewTopicAsync(It.IsAny<string>())).Returns(Task.FromResult(s_topic));
            _objectUnderTest.Initialize(_contextMock.Object);
            _objectUnderTest.NewTopicUserPrompt = projectId =>
            {
                projectIdParam = projectId;
                return MockTopicFullName;
            };
            UserPromptWindow.PromptUserFunction = options =>
            {
                details = options.ErrorDetails;
                return true;
            };

            await _objectUnderTest.OnNewTopicCommandAsync();

            Assert.AreEqual("parent.com:mock-project", projectIdParam);
            Assert.IsNull(details);
            Assert.AreEqual(1, _objectUnderTest.RefreshHitCount);
            _dataSourceMock.Verify(ds => ds.NewTopicAsync(MockTopicFullName), Times.Once);
            _dataSourceMock.Verify(ds => ds.NewTopicAsync(It.IsNotIn(MockTopicFullName)), Times.Never);
        }

        [TestMethod]
        public void TestOpenCloudConsoleCommand()
        {
            _objectUnderTest.Initialize(_contextMock.Object);

            _objectUnderTest.OnOpenCloudConsoleCommand();

            string expectedUrl = string.Format(PubsubSourceRootViewModel.PubSubConsoleUrlFormat, "parent.com:mock-project");
            _startProcessMock.Verify(f => f(expectedUrl));
        }

        [TestMethod]
        public async Task TestOptionsSaveTriggersRefresh()
        {
            _cloudExplorerOptionsMock.Setup(o => o.SaveSettingsToStorage()).Raises(o => o.SavingSettings += null, EventArgs.Empty);

            var analyticsOptionsMock = new Mock<AnalyticsOptions>();
            PackageMock.SetupGet(p => p.AnalyticsSettings).Returns(analyticsOptionsMock.Object);

            _topicSource.SetResult(new List<Topic>());
            _subscriptionSource.SetResult(new List<Subscription>());
            await _objectUnderTest.LoadData();

            GoogleCloudExtensionPackage.Instance.GetDialogPage<CloudExplorerOptions>().SaveSettingsToStorage();

            Assert.AreEqual(1, _objectUnderTest.RefreshHitCount);
        }

        [TestMethod]
        public void TestOpenBrowser()
        {
            const string targetUrl = "http://target/url";

            _objectUnderTest.OpenBrowser(targetUrl);

            _startProcessMock.Verify(f => f(targetUrl));
        }

        [TestMethod]
        public void TestChangeFiltersCommand()
        {
            PackageMock.Setup(p => p.ShowOptionPage<CloudExplorerOptions>());

            _objectUnderTest.Initialize(_contextMock.Object);
            ICommand changeFiltersCommand = _objectUnderTest.ContextMenu.Items.OfType<MenuItem>()
                    .Single(mi => mi.Header.Equals(Resources.CloudExplorerPubSubChangeFiltersMenuHeader)).Command;

            changeFiltersCommand.Execute(null);

            PackageMock.Verify(p => p.ShowOptionPage<CloudExplorerOptions>(), Times.Once);
        }

        private class TestablePubsubSourceRootViewModel : PubsubSourceRootViewModel
        {
            public int RefreshHitCount { get; private set; }

            public TestablePubsubSourceRootViewModel(Func<IPubsubDataSource> factory)
                : base(factory) { }

            internal async Task LoadData()
            {
                await LoadDataOverride();
            }

            public override void Refresh()
            {
                RefreshHitCount++;
            }
        }
    }
}
