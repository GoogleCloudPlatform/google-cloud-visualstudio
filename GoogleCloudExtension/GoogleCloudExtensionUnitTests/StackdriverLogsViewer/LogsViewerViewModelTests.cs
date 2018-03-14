using Google.Apis.Logging.v2.Data;
using Google.Apis.Logging.v2.Data.Extensions;
using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.StackdriverLogsViewer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Process = System.Diagnostics.Process;
using Project = Google.Apis.CloudResourceManager.v1.Data.Project;

namespace GoogleCloudExtensionUnitTests.StackdriverLogsViewer
{
    [TestClass]
    public class LogsViewerViewModelTests
    {
        private ILoggingDataSource _mockedLoggingDataSource;
        private TaskCompletionSource<LogEntryRequestResult> _listLogEntriesSource;
        private TaskCompletionSource<IList<MonitoredResourceDescriptor>> _getResourceDescriptorsSource;
        private TaskCompletionSource<IList<ResourceKeys>> _listResourceKeysSource;
        private TaskCompletionSource<IList<string>> _listProjectLogNamesSource;
        private LogsViewerViewModel _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            const string defaultAccountName = "default-account";
            const string defaultProjectId = "default-project";
            const string defaultProjectName = "default-project";
            _getResourceDescriptorsSource = new TaskCompletionSource<IList<MonitoredResourceDescriptor>>();
            _listResourceKeysSource = new TaskCompletionSource<IList<ResourceKeys>>();
            _listProjectLogNamesSource = new TaskCompletionSource<IList<string>>();
            _listLogEntriesSource = new TaskCompletionSource<LogEntryRequestResult>();
            _mockedLoggingDataSource = Mock.Of<ILoggingDataSource>(
                ds =>
                    ds.GetResourceDescriptorsAsync() == _getResourceDescriptorsSource.Task &&
                    ds.ListResourceKeysAsync() == _listResourceKeysSource.Task &&
                    ds.ListProjectLogNamesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()) ==
                    _listProjectLogNamesSource.Task &&
                    ds.ListLogEntriesAsync(
                        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(),
                        It.IsAny<CancellationToken>()) ==
                    _listLogEntriesSource.Task);
            CredentialsStore.Default.UpdateCurrentAccount(new UserAccount { AccountName = defaultAccountName });
            CredentialsStore.Default.UpdateCurrentProject(
                new Project { Name = defaultProjectName, ProjectId = defaultProjectId });
            _objectUnderTest = new LogsViewerViewModel(_mockedLoggingDataSource);
        }

        [TestMethod]
        public void TestInitalConditions()
        {
            const string testAccountName = "test-account";
            const string testProjectName = "test-project";
            const string testProjectId = "test-project";
            CredentialsStore.Default.UpdateCurrentAccount(new UserAccount { AccountName = testAccountName });
            CredentialsStore.Default.UpdateCurrentProject(
                new Project { Name = testProjectName, ProjectId = testProjectId });

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
            Assert.IsFalse(_objectUnderTest.ShowRequestErrorMessage);
            Assert.IsNull(_objectUnderTest.RequestErrorMessage);
            Assert.IsNull(_objectUnderTest.RequestStatusText);
            Assert.IsFalse(_objectUnderTest.ShowRequestStatus);
            Assert.IsTrue(_objectUnderTest.IsControlEnabled);
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
        public void TestSimplePageLoading()
        {
            _getResourceDescriptorsSource.SetResult(
                new[] { new MonitoredResourceDescriptor { Type = ResourceTypeNameConsts.GlobalType } });
            _listResourceKeysSource.SetResult(new[] { new ResourceKeys { Type = ResourceTypeNameConsts.GlobalType } });
            _listProjectLogNamesSource.SetResult(new[] { "log-id" });

            // Two calls here are required by the test but not normal use.
            // This is probably due to all async calls completing syncronously.
            // TODO(przybjw): Fix issue #898.
            _objectUnderTest.SimpleTextSearchCommand.Execute(null);
            _objectUnderTest.SimpleTextSearchCommand.Execute(null);

            Assert.IsFalse(_objectUnderTest.IsControlEnabled);
            Assert.IsNull(_objectUnderTest.RequestErrorMessage);
            Assert.IsFalse(_objectUnderTest.ShowRequestErrorMessage);
            Assert.AreEqual(Resources.LogViewerRequestProgressMessage, _objectUnderTest.RequestStatusText);
            Assert.IsTrue(_objectUnderTest.ShowRequestStatus);
            Assert.IsTrue(_objectUnderTest.ShowCancelRequestButton);
        }

        [TestMethod]
        public void TestAdvancedPageLoading()
        {
            _getResourceDescriptorsSource.SetResult(
                new[] { new MonitoredResourceDescriptor { Type = ResourceTypeNameConsts.GlobalType } });
            _listResourceKeysSource.SetResult(new[] { new ResourceKeys { Type = ResourceTypeNameConsts.GlobalType } });
            _listProjectLogNamesSource.SetResult(new[] { "log-id" });

            // Two calls here are required by the test but not normal use.
            // This is probably due to all async calls completing syncronously.
            // TODO(przybjw): Fix issue #898.
            _objectUnderTest.SubmitAdvancedFilterCommand.Execute(null);
            _objectUnderTest.SubmitAdvancedFilterCommand.Execute(null);

            Assert.IsFalse(_objectUnderTest.IsControlEnabled);
            Assert.IsNull(_objectUnderTest.RequestErrorMessage);
            Assert.IsFalse(_objectUnderTest.ShowRequestErrorMessage);
            Assert.AreEqual(Resources.LogViewerRequestProgressMessage, _objectUnderTest.RequestStatusText);
            Assert.IsTrue(_objectUnderTest.ShowRequestStatus);
            Assert.IsTrue(_objectUnderTest.ShowCancelRequestButton);
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
            const string testNodeName = "test-node";
            const string testNodeValue = "test-value";

            var treeNode = new ObjectNodeTree(testNodeName, testNodeValue, null);
            _objectUnderTest.OnDetailTreeNodeFilterCommand.Execute(treeNode);

            Assert.IsFalse(_objectUnderTest.IsAutoReloadChecked);
            Assert.IsTrue(_objectUnderTest.ShowAdvancedFilter);
            Assert.IsTrue(_objectUnderTest.AdvancedFilterText.Contains(testNodeName));
            Assert.IsTrue(_objectUnderTest.AdvancedFilterText.Contains(testNodeValue));
        }
    }
}