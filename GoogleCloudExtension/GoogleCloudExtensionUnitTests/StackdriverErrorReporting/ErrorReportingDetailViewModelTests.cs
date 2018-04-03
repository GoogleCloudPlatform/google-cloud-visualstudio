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

using Google.Apis.Clouderrorreporting.v1beta1.Data;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.StackdriverErrorReporting;
using GoogleCloudExtension.UserPrompt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventTimeRangePeriodEnum =
    Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.EventsResource.ListRequest.TimeRangePeriodEnum;
using GroupTimeRangePeriodEnum =
    Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;

namespace GoogleCloudExtensionUnitTests.StackdriverErrorReporting
{
    [TestClass]
    public class ErrorReportingDetailViewModelTests
    {
        private TimeRangeItem _defaultTimeRangeItem;

        private ErrorGroupItem _defaultErrorGroupItem;

        private ErrorReportingDetailViewModel _objectUnderTest;
        private List<string> _propertiesChanged;
        private Mock<Action<ErrorGroupItem, StackFrame>> _errorFrameToSourceLineMock;
        private Mock<Func<ErrorReportingToolWindow>> _showErrorReportingToolWindowMock;
        private Mock<IStackdriverErrorReportingDataSource> _dataSourceMock;
        private TaskCompletionSource<ListEventsResponse> _getPageOfEventsSource;
        private TaskCompletionSource<ListGroupStatsResponse> _getPageOfGroupStatusSource;
        private Mock<Func<UserPromptWindow.Options, bool>> _promptUserMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _propertiesChanged = new List<string>();
            _errorFrameToSourceLineMock = new Mock<Action<ErrorGroupItem, StackFrame>>();
            _showErrorReportingToolWindowMock = new Mock<Func<ErrorReportingToolWindow>>();
            _dataSourceMock = new Mock<IStackdriverErrorReportingDataSource>();
            _getPageOfEventsSource = new TaskCompletionSource<ListEventsResponse>();
            _dataSourceMock
                .Setup(
                    ds => ds.GetPageOfEventsAsync(
                        It.IsAny<ErrorGroupStats>(),
                        It.IsAny<EventTimeRangePeriodEnum>(),
                        It.IsAny<string>()))
                .Returns(() => _getPageOfEventsSource.Task);
            _getPageOfGroupStatusSource = new TaskCompletionSource<ListGroupStatsResponse>();
            _dataSourceMock
                .Setup(
                    ds => ds.GetPageOfGroupStatusAsync(
                        It.IsAny<GroupTimeRangePeriodEnum>(), It.IsAny<string>(), It.IsAny<string>(),
                        It.IsAny<string>())).Returns(() => _getPageOfGroupStatusSource.Task);

            _objectUnderTest = new ErrorReportingDetailViewModel(_dataSourceMock.Object);

            _objectUnderTest.PropertyChanged += (sender, args) => _propertiesChanged.Add(args.PropertyName);
            _objectUnderTest.ErrorFrameToSourceLine = _errorFrameToSourceLineMock.Object;
            _objectUnderTest.ShowErrorReportingToolWindow = _showErrorReportingToolWindowMock.Object;

            _defaultTimeRangeItem = _objectUnderTest.AllTimeRangeItems.First();
            _defaultErrorGroupItem = new ErrorGroupItem(
                new ErrorGroupStats { Group = new ErrorGroup { GroupId = "" }, TimedCounts = new List<TimedCount>() },
                _defaultTimeRangeItem);

            _promptUserMock = new Mock<Func<UserPromptWindow.Options, bool>>();
            _promptUserMock.Setup(f => f(It.IsAny<UserPromptWindow.Options>())).Returns(true);
            UserPromptWindow.PromptUserFunction = _promptUserMock.Object;
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            Assert.IsFalse(_objectUnderTest.IsAccountChanged);
            Assert.IsNull(_objectUnderTest.ErrorString);
            Assert.IsFalse(_objectUnderTest.ShowError);
            Assert.IsTrue(_objectUnderTest.IsControlEnabled);
            Assert.IsFalse(_objectUnderTest.IsGroupLoading);
            Assert.IsFalse(_objectUnderTest.IsEventLoading);
            Assert.IsNull(_objectUnderTest.GroupItem);
            Assert.IsNull(_objectUnderTest.EventItemCollection);
            Assert.IsNull(_objectUnderTest.SelectedTimeRangeItem);
            CollectionAssert.AreEqual(TimeRangeItem.CreateTimeRanges(), _objectUnderTest.AllTimeRangeItems.ToList());
            Assert.IsTrue(_objectUnderTest.OnBackToOverViewCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.OnGotoSourceCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.OnAutoReloadCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestIsAccountChangedProperty()
        {
            _objectUnderTest.IsAccountChanged = true;

            Assert.IsTrue(_objectUnderTest.IsAccountChanged);
            CollectionAssert.AreEqual(new[] { nameof(_objectUnderTest.IsAccountChanged) }, _propertiesChanged);
        }

        [TestMethod]
        public void TestErrorStringProperty()
        {
            const string newValue = "new error string";
            _objectUnderTest.ErrorString = newValue;

            Assert.AreEqual(newValue, _objectUnderTest.ErrorString);
            CollectionAssert.AreEqual(new[] { nameof(_objectUnderTest.ErrorString) }, _propertiesChanged);
        }

        [TestMethod]
        public void TestShowErrorProperty()
        {
            _objectUnderTest.ShowError = true;

            Assert.IsTrue(_objectUnderTest.ShowError);
            CollectionAssert.AreEqual(new[] { nameof(_objectUnderTest.ShowError) }, _propertiesChanged);
        }

        [TestMethod]
        public void TestIsControlEnabledProperty()
        {
            _objectUnderTest.IsControlEnabled = true;

            Assert.IsTrue(_objectUnderTest.IsControlEnabled);
            CollectionAssert.AreEqual(new[] { nameof(_objectUnderTest.IsControlEnabled) }, _propertiesChanged);
        }

        [TestMethod]
        public void TestIsGroupLoadingProperty()
        {
            _objectUnderTest.IsGroupLoading = true;

            Assert.IsTrue(_objectUnderTest.IsGroupLoading);
            CollectionAssert.AreEqual(new[] { nameof(_objectUnderTest.IsGroupLoading) }, _propertiesChanged);
        }

        [TestMethod]
        public void TestIsEventLoadingProperty()
        {
            _objectUnderTest.IsEventLoading = true;

            Assert.IsTrue(_objectUnderTest.IsEventLoading);
            CollectionAssert.AreEqual(new[] { nameof(_objectUnderTest.IsEventLoading) }, _propertiesChanged);
        }

        [TestMethod]
        public void TestSelectedTimeRangeItemProperty()
        {
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, _defaultTimeRangeItem);
            TimeRangeItem newValue = _objectUnderTest.AllTimeRangeItems.Skip(1).First();
            _propertiesChanged.Clear();

            _objectUnderTest.SelectedTimeRangeItem = newValue;

            Assert.AreEqual(newValue, _objectUnderTest.SelectedTimeRangeItem);
            Assert.IsFalse(_objectUnderTest.IsControlEnabled);
            Assert.IsTrue(_objectUnderTest.IsGroupLoading);
            Assert.IsFalse(_objectUnderTest.ShowError);
            Assert.AreEqual(nameof(_objectUnderTest.SelectedTimeRangeItem), _propertiesChanged.First());
        }

        [TestMethod]
        public void TestOnGotoSourceCommand()
        {
            var commandInput = new StackFrame("raw frame");
            _objectUnderTest.OnGotoSourceCommand.Execute(commandInput);

            _errorFrameToSourceLineMock.Verify(f => f(null, commandInput));
        }

        [TestMethod]
        public void TestOnBackToOverViewCommand()
        {
            _objectUnderTest.OnBackToOverViewCommand.Execute(null);

            _showErrorReportingToolWindowMock.Verify(f => f());
        }

        [TestMethod]
        public void TestOnAutoReloadCommandLoadingGroup()
        {
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { _defaultErrorGroupItem.ErrorGroup } });
            _getPageOfEventsSource.SetResult(new ListEventsResponse());
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, _defaultTimeRangeItem);
            _getPageOfGroupStatusSource = new TaskCompletionSource<ListGroupStatsResponse>();
            _getPageOfEventsSource = new TaskCompletionSource<ListEventsResponse>();

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.IsFalse(_objectUnderTest.IsControlEnabled);
            Assert.IsTrue(_objectUnderTest.IsGroupLoading);
            Assert.IsFalse(_objectUnderTest.ShowError);
        }

        [TestMethod]
        public void TestOnAutoReloadCommandLoadGroupError()
        {
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { _defaultErrorGroupItem.ErrorGroup } });
            _getPageOfEventsSource.SetResult(new ListEventsResponse());
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, _defaultTimeRangeItem);
            _getPageOfGroupStatusSource = new TaskCompletionSource<ListGroupStatsResponse>();
            _getPageOfEventsSource = new TaskCompletionSource<ListEventsResponse>();
            _getPageOfGroupStatusSource.SetException(new DataSourceException());

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.IsFalse(_objectUnderTest.IsControlEnabled);
            Assert.IsFalse(_objectUnderTest.IsGroupLoading);
            Assert.IsNotNull(_objectUnderTest.ErrorString);

            //Todo(jimwp): See issue #912. I think this is a minor bug.
            Assert.IsFalse(_objectUnderTest.ShowError);
        }

        [TestMethod]
        public void TestOnAutoReloadCommandLoadGroupEmpty()
        {
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { _defaultErrorGroupItem.ErrorGroup } });
            _getPageOfEventsSource.SetResult(new ListEventsResponse());
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, _defaultTimeRangeItem);
            _getPageOfGroupStatusSource = new TaskCompletionSource<ListGroupStatsResponse>();
            _getPageOfEventsSource = new TaskCompletionSource<ListEventsResponse>();

            _getPageOfGroupStatusSource.SetResult(new ListGroupStatsResponse());

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.IsControlEnabled);
            Assert.IsFalse(_objectUnderTest.IsGroupLoading);
            Assert.IsFalse(_objectUnderTest.ShowError);
            Assert.IsNull(_objectUnderTest.ErrorString);
            Assert.AreEqual(0, _objectUnderTest.GroupItem.ErrorCount);
            Assert.IsNull(_objectUnderTest.GroupItem.TimedCountList);
            Assert.AreEqual("-", _objectUnderTest.GroupItem.AffectedUsersCount);
            Assert.IsNull(_objectUnderTest.GroupItem.SeenIn);
        }

        [TestMethod]
        public void TestOnAutoReloadCommandLoadingEvents()
        {
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { _defaultErrorGroupItem.ErrorGroup } });
            _getPageOfEventsSource.SetResult(new ListEventsResponse());
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, _defaultTimeRangeItem);
            _getPageOfGroupStatusSource = new TaskCompletionSource<ListGroupStatsResponse>();
            _getPageOfEventsSource = new TaskCompletionSource<ListEventsResponse>();
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { _defaultErrorGroupItem.ErrorGroup } });

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.IsFalse(_objectUnderTest.IsControlEnabled);
            Assert.IsFalse(_objectUnderTest.IsGroupLoading);
            Assert.IsTrue(_objectUnderTest.IsEventLoading);
            Assert.IsFalse(_objectUnderTest.ShowError);
            Assert.IsNull(_objectUnderTest.ErrorString);
            Assert.IsNull(_objectUnderTest.EventItemCollection);
            Assert.AreNotSame(_defaultErrorGroupItem, _objectUnderTest.GroupItem);
        }

        [TestMethod]
        public void TestOnAutoReloadCommandLoadingEventsError()
        {
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { _defaultErrorGroupItem.ErrorGroup } });
            _getPageOfEventsSource.SetResult(new ListEventsResponse());
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, _defaultTimeRangeItem);
            _getPageOfGroupStatusSource = new TaskCompletionSource<ListGroupStatsResponse>();
            _getPageOfEventsSource = new TaskCompletionSource<ListEventsResponse>();
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { _defaultErrorGroupItem.ErrorGroup } });
            _getPageOfEventsSource.SetException(new DataSourceException());

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.IsControlEnabled);
            Assert.IsFalse(_objectUnderTest.IsEventLoading);
            Assert.IsTrue(_objectUnderTest.ShowError);
            Assert.IsNotNull(_objectUnderTest.ErrorString);
        }

        [TestMethod]
        public void TestOnAutoReloadCommandLoadEmptyEvents()
        {
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { _defaultErrorGroupItem.ErrorGroup } });
            _getPageOfEventsSource.SetResult(new ListEventsResponse { ErrorEvents = new[] { new ErrorEvent() } });
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, _defaultTimeRangeItem);
            _getPageOfGroupStatusSource = new TaskCompletionSource<ListGroupStatsResponse>();
            _getPageOfEventsSource = new TaskCompletionSource<ListEventsResponse>();
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { _defaultErrorGroupItem.ErrorGroup } });
            _getPageOfEventsSource.SetResult(new ListEventsResponse());

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.IsControlEnabled);
            Assert.IsFalse(_objectUnderTest.IsEventLoading);
            Assert.IsFalse(_objectUnderTest.ShowError);
            Assert.IsNull(_objectUnderTest.ErrorString);
            Assert.IsNull(_objectUnderTest.EventItemCollection);
        }

        [TestMethod]
        public void TestOnAutoReloadCommandLoadEvents()
        {
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { _defaultErrorGroupItem.ErrorGroup } });
            _getPageOfEventsSource.SetResult(new ListEventsResponse());
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, _defaultTimeRangeItem);
            _getPageOfGroupStatusSource = new TaskCompletionSource<ListGroupStatsResponse>();
            _getPageOfEventsSource = new TaskCompletionSource<ListEventsResponse>();
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { _defaultErrorGroupItem.ErrorGroup } });
            _getPageOfEventsSource.SetResult(new ListEventsResponse { ErrorEvents = new[] { new ErrorEvent() } });

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.IsControlEnabled);
            Assert.IsFalse(_objectUnderTest.IsEventLoading);
            Assert.IsFalse(_objectUnderTest.ShowError);
            Assert.IsNull(_objectUnderTest.ErrorString);
            Assert.AreEqual(1, _objectUnderTest.EventItemCollection.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ErrorReportingException))]
        public void TestUpdateViewNullGroup()
        {
            try
            {
                _objectUnderTest.UpdateView(null, _defaultTimeRangeItem);
            }
            catch (ErrorReportingException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));
                throw;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ErrorReportingException))]
        public void TestUpdateViewNullTimeRange()
        {
            try
            {
                _objectUnderTest.UpdateView(_defaultErrorGroupItem, null);
            }
            catch (ErrorReportingException e)
            {
                Assert.IsInstanceOfType(e.InnerException, typeof(ArgumentNullException));
                throw;
            }
        }

        [TestMethod]
        public void TestUpdateViewNewTimeRange()
        {
            TimeRangeItem newTimeRange = _objectUnderTest.AllTimeRangeItems.Skip(1).First();
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, newTimeRange);

            Assert.AreEqual(newTimeRange, _objectUnderTest.SelectedTimeRangeItem);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestUpdateViewInvalidTimeRange()
        {
            var invalidTimeRange = new TimeRangeItem("", "", 0, 0);
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, invalidTimeRange);
        }

        [TestMethod]
        public void TestUpdateViewSameTimeRangeLoadingEvents()
        {
            _objectUnderTest.SelectedTimeRangeItem = _defaultTimeRangeItem;
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, _objectUnderTest.SelectedTimeRangeItem);

            Assert.IsFalse(_objectUnderTest.IsControlEnabled);
            Assert.IsTrue(_objectUnderTest.IsEventLoading);
            Assert.IsNull(_objectUnderTest.EventItemCollection);
        }

        [TestMethod]
        public void TestUpdateViewSameTimeRangeLoadEventsError()
        {
            _objectUnderTest.SelectedTimeRangeItem = _defaultTimeRangeItem;
            _getPageOfEventsSource.SetException(new DataSourceException());
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, _objectUnderTest.SelectedTimeRangeItem);

            Assert.IsTrue(_objectUnderTest.IsControlEnabled);
            Assert.IsFalse(_objectUnderTest.IsEventLoading);
            Assert.IsNull(_objectUnderTest.EventItemCollection);
            Assert.IsTrue(_objectUnderTest.ShowError);
        }

        [TestMethod]
        public void TestUpdateViewSameTimeRangeLoadedEvents()
        {
            _getPageOfEventsSource.SetResult(new ListEventsResponse { ErrorEvents = new[] { new ErrorEvent() } });
            _objectUnderTest.SelectedTimeRangeItem = _defaultTimeRangeItem;
            _objectUnderTest.UpdateView(_defaultErrorGroupItem, _objectUnderTest.SelectedTimeRangeItem);

            Assert.IsTrue(_objectUnderTest.IsControlEnabled);
            Assert.IsFalse(_objectUnderTest.IsEventLoading);
            Assert.AreEqual(1, _objectUnderTest.EventItemCollection.Count);
            Assert.IsFalse(_objectUnderTest.ShowError);
        }
    }
}
