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

using Google.Apis.Logging.v2.Data;
using Google.Apis.Logging.v2.Data.Extensions;
using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.StackdriverLogsViewer;
using GoogleCloudExtension.Utils.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Process = System.Diagnostics.Process;
using Task = System.Threading.Tasks.Task;

namespace GoogleCloudExtensionUnitTests.StackdriverLogsViewer
{
    [TestClass]
    public class LogsViewerViewModelTests : ExtensionTestBase
    {
        private ILoggingDataSource _mockedLoggingDataSource;
        private TaskCompletionSource<LogEntryRequestResult> _listLogEntriesSource;
        private TaskCompletionSource<IList<MonitoredResourceDescriptor>> _getResourceDescriptorsSource;
        private TaskCompletionSource<IList<ResourceKeys>> _listResourceKeysSource;
        private TaskCompletionSource<IList<string>> _listProjectLogNamesSource;
        private LogsViewerViewModel _objectUnderTest;
        private List<string> _propertiesChanged;

        protected override void BeforeEach()
        {
            PackageMock.Setup(p => p.IsWindowActive()).Returns(true);

            _getResourceDescriptorsSource = new TaskCompletionSource<IList<MonitoredResourceDescriptor>>();
            _listResourceKeysSource = new TaskCompletionSource<IList<ResourceKeys>>();
            _listProjectLogNamesSource = new TaskCompletionSource<IList<string>>();
            _listLogEntriesSource = new TaskCompletionSource<LogEntryRequestResult>();
            _mockedLoggingDataSource = Mock.Of<ILoggingDataSource>(
                ds =>
                    ds.GetResourceDescriptorsAsync() == _getResourceDescriptorsSource.Task &&
                    ds.ListResourceKeysAsync() == _listResourceKeysSource.Task &&
                    (ds.ListProjectLogNamesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()) ==
                        _listProjectLogNamesSource.Task) &&
                    (ds.ListLogEntriesAsync(
                            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(),
                            It.IsAny<CancellationToken>()) ==
                        _listLogEntriesSource.Task));

            _objectUnderTest = new LogsViewerViewModel(_mockedLoggingDataSource);
            _propertiesChanged = new List<string>();
            _objectUnderTest.PropertyChanged += (sender, args) => _propertiesChanged.Add(args.PropertyName);
        }

        [TestMethod]
        public void TestInitalConditions()
        {
            const string testAccountName = "test-account";
            const string testProjectName = "test-project";
            const string testProjectId = "test-project";
            CredentialStoreMock.SetupGet(cs => cs.CurrentAccount).Returns(new UserAccount { AccountName = testAccountName });
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns(testProjectId);

            Assert.IsNull(_objectUnderTest.LogIdList);
            Assert.IsNotNull(_objectUnderTest.DateTimePickerModel);
            Assert.IsTrue(_objectUnderTest.AdvancedFilterHelpCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.SubmitAdvancedFilterCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.SimpleTextSearchCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.FilterSwitchCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.OnDetailTreeNodeFilterCommand.CanExecuteCommand);
            Assert.IsNull(_objectUnderTest.AdvancedFilterText);
            Assert.IsFalse(_objectUnderTest.ShowAdvancedFilter);
            Assert.IsNull(_objectUnderTest.SimpleSearchText);
            Assert.AreEqual(7, _objectUnderTest.LogSeverityList.Count);
            Assert.IsNotNull(_objectUnderTest.ResourceTypeSelector);
            Assert.AreEqual(LogSeverity.All, _objectUnderTest.SelectedLogSeverity.Severity);
            Assert.AreEqual(TimeZoneInfo.GetSystemTimeZones(), _objectUnderTest.SystemTimeZones);
            Assert.AreEqual(TimeZoneInfo.Local, _objectUnderTest.SelectedTimeZone);
            Assert.IsTrue(_objectUnderTest.RefreshCommand.CanExecuteCommand);
            Assert.AreEqual(testAccountName, _objectUnderTest.Account);
            Assert.AreEqual(testProjectName, _objectUnderTest.Project);
            Assert.IsFalse(_objectUnderTest.ToggleExpandAllExpanded);
            Assert.AreEqual(Resources.LogViewerExpandAllTip, _objectUnderTest.ToggleExapandAllToolTip);
            Assert.IsNotNull(_objectUnderTest.LogItemCollection);
            Assert.IsTrue(_objectUnderTest.CancelRequestCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.ShowCancelRequestButton);
            Assert.IsNull(_objectUnderTest.RequestStatusText);
            Assert.IsTrue(_objectUnderTest.AsyncAction.IsSuccess);
            Assert.IsNull(_objectUnderTest.AsyncAction.ErrorMessage);
            Assert.IsTrue(_objectUnderTest.OnAutoReloadCommand.CanExecuteCommand);
            Assert.IsFalse(_objectUnderTest.IsAutoReloadChecked);
            Assert.AreEqual((uint)3, _objectUnderTest.AutoReloadIntervalSeconds);
        }

        [TestMethod]
        public void TestToggleExpandAll()
        {
            _objectUnderTest.ToggleExpandAllExpanded = true;

            Assert.IsTrue(_objectUnderTest.ToggleExpandAllExpanded);
            Assert.AreEqual(Resources.LogViewerCollapseAllTip, _objectUnderTest.ToggleExapandAllToolTip);
        }

        [TestMethod]
        public void TestToggleCollapseAll()
        {
            _objectUnderTest.ToggleExpandAllExpanded = true;

            _objectUnderTest.ToggleExpandAllExpanded = false;

            Assert.IsFalse(_objectUnderTest.ToggleExpandAllExpanded);
            Assert.AreEqual(Resources.LogViewerExpandAllTip, _objectUnderTest.ToggleExapandAllToolTip);
        }

        [TestMethod]
        public void TestNoLoadWhenOffScreen()
        {
            _listLogEntriesSource.SetException(new DataSourceException(""));
            _objectUnderTest.IsVisibleUnbound = false;

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.IsFalse(_objectUnderTest.AsyncAction.IsError);
        }

        [TestMethod]
        public void TestNoLoadWhenMinimized()
        {
            _listLogEntriesSource.SetException(new DataSourceException(""));
            PackageMock.Setup(p => p.IsWindowActive()).Returns(false);

            _objectUnderTest.OnAutoReloadCommand.Execute(null);

            Assert.IsFalse(_objectUnderTest.AsyncAction.IsError);
        }

        [TestMethod]
        public void TestSimplePageLoading()
        {
            _getResourceDescriptorsSource.SetResult(
                new[] { new MonitoredResourceDescriptor { Type = ResourceTypeNameConsts.GlobalType } });
            _listResourceKeysSource.SetResult(new[] { new ResourceKeys { Type = ResourceTypeNameConsts.GlobalType } });
            _listProjectLogNamesSource.SetResult(new[] { "log-id" });

            _objectUnderTest.SimpleTextSearchCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsPending);
            Assert.AreEqual(Resources.LogViewerRequestProgressMessage, _objectUnderTest.RequestStatusText);
            Assert.IsTrue(_objectUnderTest.ShowCancelRequestButton);
        }

        [TestMethod]
        public void TestAdvancedPageLoading()
        {
            _getResourceDescriptorsSource.SetResult(
                new[] { new MonitoredResourceDescriptor { Type = ResourceTypeNameConsts.GlobalType } });
            _listResourceKeysSource.SetResult(new[] { new ResourceKeys { Type = ResourceTypeNameConsts.GlobalType } });
            _listProjectLogNamesSource.SetResult(new[] { "log-id" });

            _objectUnderTest.SubmitAdvancedFilterCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsPending);
            Assert.AreEqual(Resources.LogViewerRequestProgressMessage, _objectUnderTest.RequestStatusText);
            Assert.IsTrue(_objectUnderTest.ShowCancelRequestButton);
        }

        [TestMethod]
        public void TestPageLoadingCanceling()
        {
            _getResourceDescriptorsSource.SetResult(
                new[] { new MonitoredResourceDescriptor { Type = ResourceTypeNameConsts.GlobalType } });
            _listResourceKeysSource.SetResult(new[] { new ResourceKeys { Type = ResourceTypeNameConsts.GlobalType } });
            _listProjectLogNamesSource.SetResult(new[] { "log-id" });
            _objectUnderTest.SubmitAdvancedFilterCommand.Execute(null);
            _propertiesChanged.Clear();

            _objectUnderTest.CancelRequestCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsPending);
            Assert.AreEqual(Resources.LogViewerRequestCancellingMessage, _objectUnderTest.RequestStatusText);
            Assert.IsFalse(_objectUnderTest.ShowCancelRequestButton);
            CollectionAssert.Contains(_propertiesChanged, nameof(_objectUnderTest.ShowCancelRequestButton), string.Join(", ", _propertiesChanged));
            CollectionAssert.Contains(_propertiesChanged, nameof(_objectUnderTest.RequestStatusText),
                string.Join(", ", _propertiesChanged));
        }

        [TestMethod]
        [ExpectedException(typeof(TaskCanceledException))]
        public async Task TestPageLoadingCanceled()
        {
            _getResourceDescriptorsSource.SetResult(
                new[] { new MonitoredResourceDescriptor { Type = ResourceTypeNameConsts.GlobalType } });
            _listResourceKeysSource.SetResult(new[] { new ResourceKeys { Type = ResourceTypeNameConsts.GlobalType } });
            _listProjectLogNamesSource.SetResult(new[] { "log-id" });
            _objectUnderTest.SubmitAdvancedFilterCommand.Execute(null);
            _objectUnderTest.CancelRequestCommand.Execute(null);

            _listLogEntriesSource.SetCanceled();
            try
            {
                await _objectUnderTest.AsyncAction.ActualTask;
            }
            catch (TaskCanceledException)
            {
                Assert.IsNull(_objectUnderTest.RequestStatusText);
                Assert.IsTrue(_objectUnderTest.AsyncAction.IsCanceled);
                Assert.IsFalse(_objectUnderTest.ShowCancelRequestButton);
                throw;
            }
        }

        [TestMethod]
        public void TestPageLoadError()
        {
            const string exceptionMessage = "test exception message";
            _getResourceDescriptorsSource.SetResult(
                new[] { new MonitoredResourceDescriptor { Type = ResourceTypeNameConsts.GlobalType } });
            _listResourceKeysSource.SetResult(new[] { new ResourceKeys { Type = ResourceTypeNameConsts.GlobalType } });
            _listProjectLogNamesSource.SetResult(new[] { "log-id" });
            _listLogEntriesSource.SetException(new DataSourceException(exceptionMessage));

            _objectUnderTest.SubmitAdvancedFilterCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsError);
            Assert.AreEqual(exceptionMessage, _objectUnderTest.AsyncAction.ErrorMessage);
            Assert.IsNull(_objectUnderTest.RequestStatusText);
            Assert.IsFalse(_objectUnderTest.ShowCancelRequestButton);
        }

        [TestMethod]
        public async Task TestPageLoaded()
        {
            _getResourceDescriptorsSource.SetResult(
                new[] { new MonitoredResourceDescriptor { Type = ResourceTypeNameConsts.GlobalType } });
            _listResourceKeysSource.SetResult(new[] { new ResourceKeys { Type = ResourceTypeNameConsts.GlobalType } });
            _listProjectLogNamesSource.SetResult(new[] { "log-id" });
            _listLogEntriesSource.SetResult(
                new LogEntryRequestResult(
                    new[] { new LogEntry { Timestamp = DateTimeOffset.Parse("2001-1-1 11:01") } }, null));

            _objectUnderTest.SubmitAdvancedFilterCommand.Execute(null);
            await _objectUnderTest.AsyncAction.ActualTask;

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsSuccess);
            Assert.IsNull(_objectUnderTest.RequestStatusText);
            Assert.IsFalse(_objectUnderTest.ShowCancelRequestButton);
            Assert.AreEqual(1, _objectUnderTest.LogItemCollection.Count);
        }

        [TestMethod]
        public async Task TestPageLoadedEmpty()
        {
            _getResourceDescriptorsSource.SetResult(
                new[] { new MonitoredResourceDescriptor { Type = ResourceTypeNameConsts.GlobalType } });
            _listResourceKeysSource.SetResult(new[] { new ResourceKeys { Type = ResourceTypeNameConsts.GlobalType } });
            _listProjectLogNamesSource.SetResult(new[] { "log-id" });
            _listLogEntriesSource.SetResult(new LogEntryRequestResult(new LogEntry[0], null));

            _objectUnderTest.SubmitAdvancedFilterCommand.Execute(null);
            await _objectUnderTest.AsyncAction.ActualTask;

            Assert.IsTrue(_objectUnderTest.AsyncAction.IsSuccess);
            Assert.IsNull(_objectUnderTest.RequestStatusText);
            Assert.IsFalse(_objectUnderTest.ShowCancelRequestButton);
            Assert.AreEqual(0, _objectUnderTest.LogItemCollection.Count);
        }

        [TestMethod]
        public async Task TestUpdateTimeZone()
        {
            _getResourceDescriptorsSource.SetResult(
                new[] { new MonitoredResourceDescriptor { Type = ResourceTypeNameConsts.GlobalType } });
            _listResourceKeysSource.SetResult(new[] { new ResourceKeys { Type = ResourceTypeNameConsts.GlobalType } });
            _listProjectLogNamesSource.SetResult(new[] { "log-id" });
            DateTimeOffset originalTimestamp = DateTimeOffset.Parse("2001-1-1 11:01");
            _listLogEntriesSource.SetResult(
                new LogEntryRequestResult(
                    new[] { new LogEntry { Timestamp = originalTimestamp } }, null));
            _objectUnderTest.SubmitAdvancedFilterCommand.Execute(null);
            await _objectUnderTest.AsyncAction.ActualTask;
            TimeZoneInfo newTimeZone =
                _objectUnderTest.SystemTimeZones.First(tz => !tz.Equals(_objectUnderTest.SelectedTimeZone));

            _objectUnderTest.SelectedTimeZone = newTimeZone;

            Assert.AreEqual(newTimeZone, _objectUnderTest.SelectedTimeZone);
            Assert.AreEqual(
                TimeZoneInfo.ConvertTime(originalTimestamp.DateTime, newTimeZone),
                ((LogItem)_objectUnderTest.LogItemCollection.GetItemAt(0)).TimeStamp);
        }

        [TestMethod]
        public void TestShowAdvancedFilterHelp()
        {
            var startProcessMock = new Mock<Func<string, Process>>();
            _objectUnderTest.StartProcess = startProcessMock.Object;

            _objectUnderTest.AdvancedFilterHelpCommand.Execute(null);

            startProcessMock.Verify(f => f(LogsViewerViewModel.AdvancedHelpLink));
        }

        [TestMethod]
        public void TestSetAdvancedFilterText()
        {
            const string testFilterText = "filter text";
            _objectUnderTest.AdvancedFilterText = testFilterText;

            Assert.AreEqual(testFilterText, _objectUnderTest.AdvancedFilterText);
        }

        [TestMethod]
        public void TestSwapFilter()
        {
            _objectUnderTest.FilterSwitchCommand.Execute(null);

            Assert.IsTrue(_objectUnderTest.ShowAdvancedFilter);
        }

        [TestMethod]
        public void TestFilterLog()
        {
            const string testFilterText = "test filter";

            _objectUnderTest.FilterLog(testFilterText);

            Assert.IsFalse(_objectUnderTest.IsAutoReloadChecked);
            Assert.IsTrue(_objectUnderTest.ShowAdvancedFilter);
            Assert.IsTrue(_objectUnderTest.AdvancedFilterText.StartsWith(testFilterText, StringComparison.Ordinal));
        }

        [TestMethod]
        public void TestFilterTreeNode()
        {
            ILogsViewerViewModel newViewModel = new LogsViewerViewModel(_mockedLoggingDataSource);
            var logsToolWindow = Mock.Of<LogsViewerToolWindow>(w => w.ViewModel == newViewModel);
            logsToolWindow.Frame = VsWindowFrameMocks.GetMockedWindowFrame();
            PackageMock.Setup(p => p.FindToolWindow<LogsViewerToolWindow>(false, It.IsAny<int>())).Returns(() => null);
            PackageMock.Setup(p => p.FindToolWindow<LogsViewerToolWindow>(true, It.IsAny<int>()))
                .Returns(logsToolWindow);
            const string testNodeName = "test-node";
            const string testNodeValue = "test-value";

            var treeNode = new ObjectNodeTree(testNodeName, testNodeValue, null);
            _objectUnderTest.OnDetailTreeNodeFilterCommand.Execute(treeNode);

            Assert.IsFalse(newViewModel.IsAutoReloadChecked);
            Assert.IsTrue(newViewModel.ShowAdvancedFilter);
            Assert.IsTrue(newViewModel.AdvancedFilterText.Contains(testNodeName));
            Assert.IsTrue(newViewModel.AdvancedFilterText.Contains(testNodeValue));
        }

        [TestMethod]
        public void TestInvalidateAllPropertiesEmptyAccountName()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentAccount)
                .Returns(new UserAccount { AccountName = "" });
            AsyncProperty oldAction = _objectUnderTest.AsyncAction;

            _objectUnderTest.InvalidateAllProperties();

            Assert.AreEqual(oldAction, _objectUnderTest.AsyncAction);
        }

        [TestMethod]
        public void TestInvalidateAllPropertiesEmptyProjectId()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns("");
            AsyncProperty oldAction = _objectUnderTest.AsyncAction;

            _objectUnderTest.InvalidateAllProperties();

            Assert.AreEqual(oldAction, _objectUnderTest.AsyncAction);
        }

        [TestMethod]
        public void TestInvalidateAllPropertiesStartsReload()
        {
            AsyncProperty oldAction = _objectUnderTest.AsyncAction;

            _objectUnderTest.InvalidateAllProperties();

            Assert.AreNotEqual(oldAction, _objectUnderTest.AsyncAction);
        }
    }
}
