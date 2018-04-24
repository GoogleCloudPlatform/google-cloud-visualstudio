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

using System;
using Google.Apis.Clouderrorreporting.v1beta1;
using Google.Apis.Clouderrorreporting.v1beta1.Data;
using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.StackdriverErrorReporting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GroupTimeRangePeriodEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;

namespace GoogleCloudExtensionUnitTests.StackdriverErrorReporting
{
    [TestClass]
    public class ErrorReportingViewModelTests : ExtensionTestBase
    {
        private ErrorReportingViewModel _objectUnderTest;
        private TaskCompletionSource<ListGroupStatsResponse> _getPageOfGroupStatusSource;
        private List<string> _propertiesChanged;

        protected override void BeforeEach()
        {
            CredentialsStore.Default.UpdateCurrentProject(new Project());
            _getPageOfGroupStatusSource = new TaskCompletionSource<ListGroupStatsResponse>();
            var dataSourceMock = new Mock<IStackdriverErrorReportingDataSource>();
            dataSourceMock
                .Setup(
                    ds => ds.GetPageOfGroupStatusAsync(
                        It.IsAny<GroupTimeRangePeriodEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => _getPageOfGroupStatusSource.Task);
            _objectUnderTest = new ErrorReportingViewModel(dataSourceMock.Object);
            _propertiesChanged = new List<string>();
            _objectUnderTest.PropertyChanged += (sender, args) => _propertiesChanged.Add(args.PropertyName);
        }

        [TestMethod]
        public void TestInitalConditions()
        {
            Assert.IsNull(_objectUnderTest.ErrorString);
            Assert.IsFalse(_objectUnderTest.ShowError);
            Assert.IsTrue(_objectUnderTest.IsLoadingComplete);
            Assert.IsFalse(_objectUnderTest.IsRefreshing);
            Assert.IsFalse(_objectUnderTest.IsLoadingNextPage);
            Assert.IsFalse(_objectUnderTest.IsGridVisible);
            CollectionAssert.AreEqual(TimeRangeItem.CreateTimeRanges(), _objectUnderTest.TimeRangeItemList);
            Assert.AreEqual(_objectUnderTest.TimeRangeItemList.Last(), _objectUnderTest.SelectedTimeRangeItem);
            Assert.AreEqual(0, _objectUnderTest.GroupStatsView.Count);
            Assert.IsTrue(_objectUnderTest.OnGotoDetailCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.OnAutoReloadCommand.CanExecuteCommand);
            Assert.IsNotNull(_objectUnderTest.CurrentTimeRangeCaption);
        }

        [TestMethod]
        public void TestErrorStringProperty()
        {
            var newValue = "new error string";

            _objectUnderTest.ErrorString = newValue;

            Assert.AreEqual(newValue, _objectUnderTest.ErrorString);
            CollectionAssert.Contains(_propertiesChanged, nameof(_objectUnderTest.ErrorString));
        }

        [TestMethod]
        public void TestShowErrorProperty()
        {

            _objectUnderTest.ShowError = true;

            Assert.IsTrue(_objectUnderTest.ShowError);
            CollectionAssert.Contains(_propertiesChanged, nameof(_objectUnderTest.ShowError));
        }

        [TestMethod]
        public void TestIsLoadingCompleteProperty()
        {

            _objectUnderTest.IsLoadingComplete = true;

            Assert.IsTrue(_objectUnderTest.IsLoadingComplete);
            CollectionAssert.Contains(_propertiesChanged, nameof(_objectUnderTest.IsLoadingComplete));
        }

        [TestMethod]
        public void TestIsRefreshingProperty()
        {

            _objectUnderTest.IsRefreshing = true;

            Assert.IsTrue(_objectUnderTest.IsRefreshing);
            CollectionAssert.Contains(_propertiesChanged, nameof(_objectUnderTest.IsRefreshing));
        }

        [TestMethod]
        public void TestIsLoadingNextPageProperty()
        {

            _objectUnderTest.IsLoadingNextPage = true;

            Assert.IsTrue(_objectUnderTest.IsLoadingNextPage);
            CollectionAssert.Contains(_propertiesChanged, nameof(_objectUnderTest.IsLoadingNextPage));
        }

        [TestMethod]
        public void TestIsGridVisibleProperty()
        {
            CredentialsStore.Default.UpdateCurrentProject(new Project { ProjectId = "new project id" });

            Assert.IsTrue(_objectUnderTest.IsGridVisible);
            CollectionAssert.Contains(_propertiesChanged, nameof(_objectUnderTest.IsGridVisible));
        }

        [TestMethod]
        public void TestSelectedTimeRangeItemProperty()
        {
            var newValue = new TimeRangeItem("", "", 0, 0);

            _objectUnderTest.SelectedTimeRangeItem = newValue;

            Assert.AreEqual(newValue, _objectUnderTest.SelectedTimeRangeItem);
            Assert.IsFalse(_objectUnderTest.IsLoadingComplete);
            CollectionAssert.Contains(_propertiesChanged, nameof(_objectUnderTest.SelectedTimeRangeItem));
        }

        [TestMethod]
        public void TestOnAutoReloadCommandLoading()
        {
            _objectUnderTest.ShowError = true;

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.AreEqual(0, _objectUnderTest.GroupStatsView.Count);
            Assert.IsFalse(_objectUnderTest.IsLoadingComplete);
            Assert.IsFalse(_objectUnderTest.ShowError);
            Assert.IsTrue(_objectUnderTest.IsRefreshing);
            Assert.IsFalse(_objectUnderTest.IsLoadingNextPage);
        }

        [TestMethod]
        public void TestAutoReloadWhenOffScreen()
        {
            _objectUnderTest.IsVisibleUnbound = false;
            _objectUnderTest.ShowError = false;
            _getPageOfGroupStatusSource.SetException(new DataSourceException());

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            // No API call should be made as the control is off screen
            Assert.IsFalse(_objectUnderTest.ShowError);
        }

        [TestMethod]
        public void TestAutoReloadWhenMinimized()
        {
            _objectUnderTest.ShowError = false;
            _getPageOfGroupStatusSource.SetException(new DataSourceException());
            _packageMock.Setup(p => p.IsWindowActive()).Returns(false);

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            // No API call should be made as the control is off screen
            Assert.IsFalse(_objectUnderTest.ShowError);
        }

        [TestMethod]
        public void TestOnAutoReloadCommandLoadingError()
        {
            _objectUnderTest.ShowError = false;
            _objectUnderTest.ErrorString = null;
            _getPageOfGroupStatusSource.SetException(new DataSourceException());

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.AreEqual(0, _objectUnderTest.GroupStatsView.Count);
            Assert.IsTrue(_objectUnderTest.IsLoadingComplete);
            Assert.IsTrue(_objectUnderTest.ShowError);
            Assert.IsFalse(_objectUnderTest.IsRefreshing);
            Assert.IsFalse(_objectUnderTest.IsLoadingNextPage);
            Assert.AreEqual(Resources.ErrorReportingDataSourceGenericErrorMessage, _objectUnderTest.ErrorString);
        }

        [TestMethod]
        public void TestOnAutoReloadCommandLoadEmpty()
        {
            _objectUnderTest.ShowError = true;
            _objectUnderTest.IsRefreshing = true;
            _objectUnderTest.IsLoadingNextPage = true;
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new ErrorGroupStats[0] });

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.AreEqual(0, _objectUnderTest.GroupStatsView.Count);
            Assert.IsTrue(_objectUnderTest.IsLoadingComplete);
            Assert.IsFalse(_objectUnderTest.ShowError);
            Assert.IsFalse(_objectUnderTest.IsRefreshing);
            Assert.IsFalse(_objectUnderTest.IsLoadingNextPage);
        }

        [TestMethod]
        public void TestOnAutoReloadCommandLoadList()
        {
            _objectUnderTest.ShowError = true;
            _objectUnderTest.IsRefreshing = true;
            _objectUnderTest.IsLoadingNextPage = true;
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { new ErrorGroupStats() } });

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.AreEqual(1, _objectUnderTest.GroupStatsView.Count);
            Assert.IsTrue(_objectUnderTest.IsLoadingComplete);
            Assert.IsFalse(_objectUnderTest.ShowError);
            Assert.IsFalse(_objectUnderTest.IsRefreshing);
            Assert.IsFalse(_objectUnderTest.IsLoadingNextPage);
        }

        [TestMethod]
        public void TestLoadNextPageNoPageToken()
        {
            _objectUnderTest.LoadNextPage();

            Assert.AreEqual(0, _propertiesChanged.Count);
        }

        [TestMethod]
        public void TestLoadNextPageLoading()
        {
            _objectUnderTest.ShowError = true;
            _objectUnderTest.IsRefreshing = true;
            _objectUnderTest.IsLoadingNextPage = false;
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { new ErrorGroupStats() }, NextPageToken = "someToken" });
            _objectUnderTest.OnAutoReloadCommand.Execute(null);
            _getPageOfGroupStatusSource = new TaskCompletionSource<ListGroupStatsResponse>();

            _objectUnderTest.LoadNextPage();

            Assert.AreEqual(1, _objectUnderTest.GroupStatsView.Count);
            Assert.IsFalse(_objectUnderTest.IsLoadingComplete);
            Assert.IsFalse(_objectUnderTest.ShowError);
            Assert.IsFalse(_objectUnderTest.IsRefreshing);
            Assert.IsTrue(_objectUnderTest.IsLoadingNextPage);
        }

        [TestMethod]
        public void TestLoadNextPageLoaded()
        {
            _objectUnderTest.ShowError = true;
            _objectUnderTest.IsRefreshing = true;
            _objectUnderTest.IsLoadingNextPage = false;
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse
                {
                    ErrorGroupStats = new[] { new ErrorGroupStats() },
                    NextPageToken = "someToken"
                });
            _objectUnderTest.OnAutoReloadCommand.Execute(null);
            _getPageOfGroupStatusSource = new TaskCompletionSource<ListGroupStatsResponse>();
            _getPageOfGroupStatusSource.SetResult(
                new ListGroupStatsResponse { ErrorGroupStats = new[] { new ErrorGroupStats() } });

            _objectUnderTest.LoadNextPage();

            Assert.AreEqual(2, _objectUnderTest.GroupStatsView.Count);
            Assert.IsTrue(_objectUnderTest.IsLoadingComplete);
            Assert.IsFalse(_objectUnderTest.ShowError);
            Assert.IsFalse(_objectUnderTest.IsRefreshing);
            Assert.IsFalse(_objectUnderTest.IsLoadingNextPage);
        }
    }
}
