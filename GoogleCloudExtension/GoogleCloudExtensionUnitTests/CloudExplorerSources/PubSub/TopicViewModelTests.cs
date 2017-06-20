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
    public class TopicViewModelTests
    {
        public const string MockTopicFullName = PubSubSourceRootViewModelTests.MockTopicFullName;
        public const string MockSubscriptionFullName = PubSubSourceRootViewModelTests.MockSubscriptionFullName;
        public const string MockSubscriptionLeafName = PubSubSourceRootViewModelTests.MockSubscriptionLeafName;
        public const string MockExceptionMessage = PubSubSourceRootViewModelTests.MockExceptionMessage;
        private Mock<IPubsubSourceRootViewModel> _ownerMock;
        private readonly Topic _topicItem = new Topic { Name = MockTopicFullName };
        private TopicViewModel _objectUnderTest;
        private TaskCompletionSource<Subscription> _newSubscriptionSource;

        private readonly Subscription _subscription =
            new Subscription { Name = MockSubscriptionFullName, Topic = MockTopicFullName };

        private string _newSubscriptionPromptParameter;
        private Subscription _newSubscriptionPromptReturnValue;
        private IList<UserPromptWindow.Options> _promptUserOptions;
        private TaskCompletionSource<IList<Subscription>> _getSubscriptionListSource;
        private bool _promptUserReturnValue;
        private TaskCompletionSource<object> _deleteTopicSource;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _newSubscriptionSource = new TaskCompletionSource<Subscription>();
            _getSubscriptionListSource = new TaskCompletionSource<IList<Subscription>>();
            _deleteTopicSource = new TaskCompletionSource<object>();

            _ownerMock = new Mock<IPubsubSourceRootViewModel>();
            _ownerMock.Setup(o => o.Context.ShowPropertiesWindow(It.IsAny<object>()));
            _ownerMock.Setup(o => o.DataSource.NewSubscriptionAsync(It.IsAny<Subscription>()))
                .Returns(_newSubscriptionSource.Task);
            _ownerMock.Setup(o => o.DataSource.GetSubscriptionListAsync()).Returns(_getSubscriptionListSource.Task);
            _ownerMock.Setup(o => o.DataSource.DeleteTopicAsync(It.IsAny<string>())).Returns(_deleteTopicSource.Task);

            _objectUnderTest = new TopicViewModel(_ownerMock.Object, _topicItem, new List<Subscription>());

            _newSubscriptionPromptParameter = null;
            _newSubscriptionPromptReturnValue = null;
            _objectUnderTest.NewSubscriptionUserPrompt = s =>
            {
                _newSubscriptionPromptParameter = s;
                return _newSubscriptionPromptReturnValue;
            };

            _promptUserOptions = new List<UserPromptWindow.Options>();
            _promptUserReturnValue = true;
            UserPromptWindow.PromptUserFunction = options =>
            {
                _promptUserOptions.Add(options);
                return _promptUserReturnValue;
            };
        }

        [TestMethod]
        public void TestInitalConditions()
        {
            Assert.IsFalse(_objectUnderTest.IsLoading);
            Assert.IsNotNull(_objectUnderTest.Icon);
            Assert.IsInstanceOfType(_objectUnderTest.Icon, typeof(BitmapImage));
            var icon = (BitmapImage)_objectUnderTest.Icon;
            Assert.IsTrue(icon.UriSource.ToString().Contains("topic_icon.png"));
            Assert.IsNotNull(_objectUnderTest.ContextMenu);
            var menuItems = _objectUnderTest.ContextMenu.ItemsSource.Cast<MenuItem>().ToList();
            Assert.AreEqual(3, menuItems.Count);
            Assert.AreEqual(3, menuItems.Select(mi => mi.Command).Distinct().Count());
            Assert.AreEqual(3, menuItems.Select(mi => mi.Header).Distinct().Count());
            Assert.AreEqual(3,
                menuItems.Select(mi => mi.Header).Intersect(
                    new List<string>
                    {
                        Resources.CloudExplorerPubSubNewSubscriptionMenuHeader,
                        Resources.CloudExplorerPubSubDeleteTopicMenuHeader,
                        Resources.UiPropertiesMenuHeader
                    }).Count());
            _ownerMock.Verify(o => o.Context.ShowPropertiesWindow(It.IsAny<object>()), Times.Never);
        }

        [TestMethod]
        public void TestNewSubscriptionCommandCanceled()
        {
            _newSubscriptionPromptReturnValue = null;

            _objectUnderTest.OnNewSubscriptionCommand();

            Assert.AreEqual(MockTopicFullName, _newSubscriptionPromptParameter);
            _ownerMock.Verify(o => o.DataSource.NewSubscriptionAsync(It.IsAny<Subscription>()), Times.Never);
            _ownerMock.Verify(o => o.DataSource.GetSubscriptionListAsync(), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Never);
            Assert.AreEqual(0, _promptUserOptions.Count);
        }

        [TestMethod]
        public void TestNewSubscriptionCommandCreating()
        {
            _newSubscriptionPromptReturnValue = _subscription;

            _objectUnderTest.OnNewSubscriptionCommand();

            Assert.AreEqual(MockTopicFullName, _newSubscriptionPromptParameter);
            Assert.IsTrue(_objectUnderTest.IsLoading);
            Assert.AreEqual(0, _promptUserOptions.Count);
            _ownerMock.Verify(o => o.DataSource.NewSubscriptionAsync(It.IsNotIn(_subscription)), Times.Never);
            _ownerMock.Verify(o => o.DataSource.NewSubscriptionAsync(_subscription), Times.Once);
            _ownerMock.Verify(o => o.DataSource.GetSubscriptionListAsync(), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Never);
        }

        [TestMethod]
        public void TestNewSubscriptionCommandError()
        {
            _newSubscriptionPromptReturnValue = _subscription;
            _newSubscriptionSource.SetException(new DataSourceException(MockExceptionMessage));

            _objectUnderTest.OnNewSubscriptionCommand();

            Assert.AreEqual(1, _promptUserOptions.Count);
            var errorPromptOptions = _promptUserOptions.Single();
            Assert.AreEqual(MockExceptionMessage, errorPromptOptions.ErrorDetails);
            Assert.AreEqual(Resources.PubSubNewSubscriptionErrorHeader, errorPromptOptions.Title);
            Assert.AreEqual(Resources.PubSubNewSubscriptionErrorMessage, errorPromptOptions.Prompt);
            Assert.IsFalse(_objectUnderTest.IsLoading);
            _ownerMock.Verify(o => o.DataSource.NewSubscriptionAsync(It.IsNotIn(_subscription)), Times.Never);
            _ownerMock.Verify(o => o.DataSource.NewSubscriptionAsync(_subscription), Times.Once);
            _ownerMock.Verify(o => o.DataSource.GetSubscriptionListAsync(), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Once);
        }

        [TestMethod]
        public void TestNewSubscriptionCommandLoading()
        {
            _newSubscriptionPromptReturnValue = _subscription;
            _newSubscriptionSource.SetResult(_subscription);

            _objectUnderTest.OnNewSubscriptionCommand();

            Assert.IsTrue(_objectUnderTest.IsLoading);
            Assert.AreEqual(0, _promptUserOptions.Count);
            _ownerMock.Verify(o => o.DataSource.NewSubscriptionAsync(It.IsNotIn(_subscription)), Times.Never);
            _ownerMock.Verify(o => o.DataSource.NewSubscriptionAsync(_subscription), Times.Once);
            _ownerMock.Verify(o => o.DataSource.GetSubscriptionListAsync(), Times.Once);
            _ownerMock.Verify(o => o.Refresh(), Times.Never);
        }

        [TestMethod]
        public void TestNewSubscriptionCommandLoadingError()
        {
            _newSubscriptionPromptReturnValue = _subscription;
            _newSubscriptionSource.SetResult(_subscription);
            _getSubscriptionListSource.SetException(new DataSourceException(MockExceptionMessage));

            _objectUnderTest.OnNewSubscriptionCommand();

            Assert.IsFalse(_objectUnderTest.IsLoading);
            Assert.AreEqual(0, _promptUserOptions.Count);
            _ownerMock.Verify(o => o.DataSource.NewSubscriptionAsync(It.IsNotIn(_subscription)), Times.Never);
            _ownerMock.Verify(o => o.DataSource.NewSubscriptionAsync(_subscription), Times.Once);
            _ownerMock.Verify(o => o.DataSource.GetSubscriptionListAsync(), Times.Once);
            _ownerMock.Verify(o => o.Refresh(), Times.Never);
        }

        [TestMethod]
        public void TestNewSubscriptionCommandSuccess()
        {
            _newSubscriptionPromptReturnValue = _subscription;
            _newSubscriptionSource.SetResult(_subscription);
            _getSubscriptionListSource.SetResult(new List<Subscription> { _subscription });

            _objectUnderTest.OnNewSubscriptionCommand();

            Assert.IsFalse(_objectUnderTest.IsLoading);
            Assert.AreEqual(0, _promptUserOptions.Count);
            _ownerMock.Verify(o => o.DataSource.NewSubscriptionAsync(It.IsNotIn(_subscription)), Times.Never);
            _ownerMock.Verify(o => o.DataSource.NewSubscriptionAsync(_subscription), Times.Once);
            _ownerMock.Verify(o => o.DataSource.GetSubscriptionListAsync(), Times.Once);
            _ownerMock.Verify(o => o.Refresh(), Times.Never);
        }

        [TestMethod]
        public void TestDeleteTopicCommandCancel()
        {
            _promptUserReturnValue = false;

            _objectUnderTest.OnDeleteTopicCommand();

            Assert.IsFalse(_objectUnderTest.IsLoading);
            Assert.AreEqual(1, _promptUserOptions.Count);
            var deletePromptOptions = _promptUserOptions.Single();
            Assert.AreEqual(Resources.PubSubDeleteTopicWindowHeader, deletePromptOptions.Title);
            _ownerMock.Verify(o => o.DataSource.DeleteTopicAsync(It.IsAny<string>()), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Never);
        }

        [TestMethod]
        public void TestDeleteTopicCommandDeleting()
        {
            _objectUnderTest.OnDeleteTopicCommand();

            Assert.IsTrue(_objectUnderTest.IsLoading);
            Assert.AreEqual(1, _promptUserOptions.Count);
            var deletePromptOptions = _promptUserOptions.Single();
            Assert.AreEqual(Resources.PubSubDeleteTopicWindowHeader, deletePromptOptions.Title);
            _ownerMock.Verify(o => o.DataSource.DeleteTopicAsync(MockTopicFullName), Times.Once);
            _ownerMock.Verify(o => o.DataSource.DeleteTopicAsync(It.IsNotIn(MockTopicFullName)), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Never);
        }

        [TestMethod]
        public void TestDeleteTopicCommandDeleteError()
        {
            _deleteTopicSource.SetException(new DataSourceException(MockExceptionMessage));

            _objectUnderTest.OnDeleteTopicCommand();

            Assert.IsFalse(_objectUnderTest.IsLoading);
            Assert.AreEqual(2, _promptUserOptions.Count);
            var errorPromptOptions = _promptUserOptions.Skip(1).Single();
            Assert.AreEqual(Resources.PubSubDeleteTopicErrorHeader, errorPromptOptions.Title);
            Assert.AreEqual(MockExceptionMessage, errorPromptOptions.ErrorDetails);
            _ownerMock.Verify(o => o.DataSource.DeleteTopicAsync(MockTopicFullName), Times.Once);
            _ownerMock.Verify(o => o.DataSource.DeleteTopicAsync(It.IsNotIn(MockTopicFullName)), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Once);
        }

        [TestMethod]
        public void TestDeleteTopicCommandDeleteSuccess()
        {
            _deleteTopicSource.SetResult(null);

            _objectUnderTest.OnDeleteTopicCommand();

            Assert.IsFalse(_objectUnderTest.IsLoading);
            Assert.AreEqual(1, _promptUserOptions.Count);
            var deletePromptOptions = _promptUserOptions.Single();
            Assert.AreEqual(Resources.PubSubDeleteTopicWindowHeader, deletePromptOptions.Title);
            _ownerMock.Verify(o => o.DataSource.DeleteTopicAsync(MockTopicFullName), Times.Once);
            _ownerMock.Verify(o => o.DataSource.DeleteTopicAsync(It.IsNotIn(MockTopicFullName)), Times.Never);
            _ownerMock.Verify(o => o.Refresh(), Times.Once);
        }

        [TestMethod]
        public void TestPropertiesWindowCommand()
        {
            _objectUnderTest.OnPropertiesWindowCommand();

            _ownerMock.Verify(o => o.Context.ShowPropertiesWindow(_objectUnderTest.Item), Times.Once);
            _ownerMock.Verify(o => o.Context.ShowPropertiesWindow(It.IsNotIn(_objectUnderTest.Item)), Times.Never);
        }
    }
}
