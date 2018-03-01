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

        [TestInitialize]
        public void BeforeEach()
        {
            const string defaultAccountName = "default-account";
            const string defaultProjectId = "default-project";
            const string defaultProjectName = "default-project";
            _mockedLoggingDataSource = Mock.Of<ILoggingDataSource>();
            CredentialsStore.Default.UpdateCurrentAccount(new UserAccount { AccountName = defaultAccountName });
            CredentialsStore.Default.UpdateCurrentProject(
                new Project { Name = defaultProjectName, ProjectId = defaultProjectId });
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
            var objectUnderTest = new LogsViewerViewModel(_mockedLoggingDataSource);

            Assert.IsNull(objectUnderTest.LogIdList);
            Assert.IsNotNull(objectUnderTest.DateTimePickerModel);
            Assert.IsTrue(objectUnderTest.AdvancedFilterHelpCommand.CanExecuteCommand);
            Assert.IsTrue(objectUnderTest.SubmitAdvancedFilterCommand.CanExecuteCommand);
            Assert.IsTrue(objectUnderTest.SimpleTextSearchCommand.CanExecuteCommand);
            Assert.IsTrue(objectUnderTest.FilterSwitchCommand.CanExecuteCommand);
            Assert.IsTrue(objectUnderTest.OnDetailTreeNodeFilterCommand.CanExecuteCommand);
            Assert.IsNull(objectUnderTest.AdvancedFilterText);
            Assert.IsFalse(objectUnderTest.ShowAdvancedFilter);
            Assert.IsNull(objectUnderTest.SimpleSearchText);
            Assert.AreEqual(7, objectUnderTest.LogSeverityList.Count);
            Assert.IsNotNull(objectUnderTest.ResourceTypeSelector);
            Assert.AreEqual(LogSeverity.All, objectUnderTest.SelectedLogSeverity.Severity);
            Assert.AreEqual(TimeZoneInfo.GetSystemTimeZones(), objectUnderTest.SystemTimeZones);
            Assert.AreEqual(TimeZoneInfo.Local, objectUnderTest.SelectedTimeZone);
            Assert.IsTrue(objectUnderTest.RefreshCommand.CanExecuteCommand);
            Assert.AreEqual(testAccountName, objectUnderTest.Account);
            Assert.AreEqual(testProjectName, objectUnderTest.Project);
            Assert.IsFalse(objectUnderTest.ToggleExpandAllExpanded);
            Assert.AreEqual(Resources.LogViewerExpandAllTip, objectUnderTest.ToggleExapandAllToolTip);
            Assert.IsNotNull(objectUnderTest.LogItemCollection);
            Assert.IsTrue(objectUnderTest.CancelRequestCommand.CanExecuteCommand);
            Assert.IsFalse(objectUnderTest.ShowCancelRequestButton);
            Assert.IsFalse(objectUnderTest.ShowRequestErrorMessage);
            Assert.IsNull(objectUnderTest.RequestErrorMessage);
            Assert.IsNull(objectUnderTest.RequestStatusText);
            Assert.IsFalse(objectUnderTest.ShowRequestStatus);
            Assert.IsTrue(objectUnderTest.IsControlEnabled);
            Assert.IsTrue(objectUnderTest.OnAutoReloadCommand.CanExecuteCommand);
            Assert.IsFalse(objectUnderTest.IsAutoReloadChecked);
            Assert.AreEqual((uint)3, objectUnderTest.AutoReloadIntervalSeconds);
        }

        [TestMethod]
        public void TestToggleExpandAll()
        {
            var objectUnderTests = new LogsViewerViewModel(_mockedLoggingDataSource);

            objectUnderTests.ToggleExpandAllExpanded = true;

            Assert.IsTrue(objectUnderTests.ToggleExpandAllExpanded);
            Assert.AreEqual(Resources.LogViewerCollapseAllTip, objectUnderTests.ToggleExapandAllToolTip);
        }

        [TestMethod]
        public void TestToggleCollapseAll()
        {
            var objectUnderTests = new LogsViewerViewModel(_mockedLoggingDataSource);
            objectUnderTests.ToggleExpandAllExpanded = true;

            objectUnderTests.ToggleExpandAllExpanded = false;

            Assert.IsFalse(objectUnderTests.ToggleExpandAllExpanded);
            Assert.AreEqual(Resources.LogViewerExpandAllTip, objectUnderTests.ToggleExapandAllToolTip);
        }

        [TestMethod]
        public void TestSimplePageLoading()
        {
            Mock<ILoggingDataSource> dataSourceMock = Mock.Get(_mockedLoggingDataSource);
            dataSourceMock.Setup(ds => ds.GetResourceDescriptorsAsync())
                .Returns(
                    Task.FromResult<IList<MonitoredResourceDescriptor>>(
                        new List<MonitoredResourceDescriptor>
                        {
                            new MonitoredResourceDescriptor {Type = ResourceTypeNameConsts.GlobalType}
                        }));
            dataSourceMock.Setup(ds => ds.ListResourceKeysAsync())
                .Returns(
                    Task.FromResult<IList<ResourceKeys>>(
                        new List<ResourceKeys> { new ResourceKeys { Type = ResourceTypeNameConsts.GlobalType } }));
            dataSourceMock
                .Setup(
                    ds => ds.ListProjectLogNamesAsync(
                        ResourceTypeNameConsts.GlobalType, It.IsAny<IEnumerable<string>>()))
                .Returns(Task.FromResult<IList<string>>(new[] { "log-id" }));
            var tcs = new TaskCompletionSource<LogEntryRequestResult>();
            dataSourceMock.Setup(
                ds => ds.ListLogEntriesAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var objectUnderTests = new LogsViewerViewModel(_mockedLoggingDataSource);

            objectUnderTests.SimpleTextSearchCommand.Execute(null);
            objectUnderTests.SimpleTextSearchCommand.Execute(null);

            Assert.IsFalse(objectUnderTests.IsControlEnabled);
            Assert.IsNull(objectUnderTests.RequestErrorMessage);
            Assert.IsFalse(objectUnderTests.ShowRequestErrorMessage);
            Assert.AreEqual(Resources.LogViewerRequestProgressMessage, objectUnderTests.RequestStatusText);
            Assert.IsTrue(objectUnderTests.ShowRequestStatus);
            Assert.IsTrue(objectUnderTests.ShowCancelRequestButton);
        }

        [TestMethod]
        public void TestAdvancedPageLoading()
        {
            Mock<ILoggingDataSource> dataSourceMock = Mock.Get(_mockedLoggingDataSource);
            dataSourceMock.Setup(ds => ds.GetResourceDescriptorsAsync())
                .Returns(
                    Task.FromResult<IList<MonitoredResourceDescriptor>>(
                        new List<MonitoredResourceDescriptor>
                        {
                            new MonitoredResourceDescriptor {Type = ResourceTypeNameConsts.GlobalType}
                        }));
            dataSourceMock.Setup(ds => ds.ListResourceKeysAsync())
                .Returns(
                    Task.FromResult<IList<ResourceKeys>>(
                        new List<ResourceKeys> { new ResourceKeys { Type = ResourceTypeNameConsts.GlobalType } }));
            dataSourceMock
                .Setup(
                    ds => ds.ListProjectLogNamesAsync(
                        ResourceTypeNameConsts.GlobalType, It.IsAny<IEnumerable<string>>()))
                .Returns(Task.FromResult<IList<string>>(new[] { "log-id" }));
            var tcs = new TaskCompletionSource<LogEntryRequestResult>();
            dataSourceMock.Setup(
                ds => ds.ListLogEntriesAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns(tcs.Task);
            var objectUnderTests = new LogsViewerViewModel(_mockedLoggingDataSource);

            objectUnderTests.SubmitAdvancedFilterCommand.Execute(null);
            objectUnderTests.SubmitAdvancedFilterCommand.Execute(null);

            Assert.IsFalse(objectUnderTests.IsControlEnabled);
            Assert.IsNull(objectUnderTests.RequestErrorMessage);
            Assert.IsFalse(objectUnderTests.ShowRequestErrorMessage);
            Assert.AreEqual(Resources.LogViewerRequestProgressMessage, objectUnderTests.RequestStatusText);
            Assert.IsTrue(objectUnderTests.ShowRequestStatus);
            Assert.IsTrue(objectUnderTests.ShowCancelRequestButton);
        }

        [TestMethod]
        public void TestShowAdvancedFilterHelp()
        {
            var objectUnderTests = new LogsViewerViewModel(_mockedLoggingDataSource);
            objectUnderTests.StartProcess = Mock.Of<Func<string, Process>>();

            objectUnderTests.AdvancedFilterHelpCommand.Execute(null);

            Mock.Get(objectUnderTests.StartProcess).Verify(f => f(LogsViewerViewModel.AdvancedHelpLink));
        }

        [TestMethod]
        public void TestSetAdvancedFilterText()
        {
            var objectUnderTests = new LogsViewerViewModel(_mockedLoggingDataSource);

            const string testFilterText = "filter text";
            objectUnderTests.AdvancedFilterText = testFilterText;

            Assert.AreEqual(testFilterText, objectUnderTests.AdvancedFilterText);
        }

        [TestMethod]
        public void TestSwapFilter()
        {
            var objectUnderTests = new LogsViewerViewModel(_mockedLoggingDataSource);
            var tcs = new TaskCompletionSource<IList<MonitoredResourceDescriptor>>();
            Mock<ILoggingDataSource> dataSourceMock = Mock.Get(_mockedLoggingDataSource);
            dataSourceMock.Setup(ds => ds.GetResourceDescriptorsAsync())
                .Returns(tcs.Task);

            objectUnderTests.FilterSwitchCommand.Execute(null);

            Assert.IsTrue(objectUnderTests.ShowAdvancedFilter);
        }

        [TestMethod]
        public void TestFilterLog()
        {
            var objectUnderTests = new LogsViewerViewModel(_mockedLoggingDataSource);
            var tcs = new TaskCompletionSource<IList<MonitoredResourceDescriptor>>();
            Mock<ILoggingDataSource> dataSourceMock = Mock.Get(_mockedLoggingDataSource);
            dataSourceMock.Setup(ds => ds.GetResourceDescriptorsAsync())
                .Returns(tcs.Task);

            const string testFilterText = "test filter";
            objectUnderTests.FilterLog(testFilterText);

            Assert.IsFalse(objectUnderTests.IsAutoReloadChecked);
            Assert.IsTrue(objectUnderTests.ShowAdvancedFilter);
            Assert.IsTrue(objectUnderTests.AdvancedFilterText.StartsWith(testFilterText, StringComparison.Ordinal));
        }

        [TestMethod]
        public void TestFilterTreeNode()
        {
            const string testNodeName = "test-node";
            const string testNodeValue = "test-value";
            var objectUnderTests = new LogsViewerViewModel(_mockedLoggingDataSource);
            var tcs = new TaskCompletionSource<IList<MonitoredResourceDescriptor>>();
            Mock<ILoggingDataSource> dataSourceMock = Mock.Get(_mockedLoggingDataSource);
            dataSourceMock.Setup(ds => ds.GetResourceDescriptorsAsync())
                .Returns(tcs.Task);

            var treeNode = new ObjectNodeTree(testNodeName, testNodeValue, null);
            objectUnderTests.OnDetailTreeNodeFilterCommand.Execute(treeNode);

            Assert.IsFalse(objectUnderTests.IsAutoReloadChecked);
            Assert.IsTrue(objectUnderTests.ShowAdvancedFilter);
            Assert.IsTrue(objectUnderTests.AdvancedFilterText.Contains(testNodeName));
            Assert.IsTrue(objectUnderTests.AdvancedFilterText.Contains(testNodeValue));
        }
    }
}