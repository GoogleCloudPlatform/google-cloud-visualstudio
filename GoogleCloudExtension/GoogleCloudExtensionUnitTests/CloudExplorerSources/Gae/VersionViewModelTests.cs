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
using Google.Apis.Appengine.v1.Data;
using GoogleCloudExtension.CloudExplorerSources.Gae;
using GoogleCloudExtension.StackdriverLogsViewer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Services;
using TestingHelpers;
using Resources = GoogleCloudExtension.Resources;
using Version = Google.Apis.Appengine.v1.Data.Version;

namespace GoogleCloudExtensionUnitTests.CloudExplorerSources.Gae
{
    [TestClass]
    public class VersionViewModelTests : ExtensionTestBase
    {
        [TestMethod]
        public void TestOnBrowseStackdriverLogCommand()
        {
            string filter = null;
            var logsToolWindowMock = new Mock<LogsViewerToolWindow> { CallBase = true };
            logsToolWindowMock.Setup(w => w.ViewModel.FilterLog(It.IsAny<string>())).Callback((string s) => filter = s);
            logsToolWindowMock.Object.Frame = VsWindowFrameMocks.GetMockedWindowFrame();
            PackageMock.Setup(
                    p => p.FindToolWindow<LogsViewerToolWindow>(false, It.IsAny<int>()))
                .Returns(() => null);
            PackageMock.Setup(p => p.FindToolWindow<LogsViewerToolWindow>(true, It.IsAny<int>()))
                .Returns(logsToolWindowMock.Object);
            var objectUnderTest = new VersionViewModel(
                new GaeSourceRootViewModel(),
                Mock.Of<Service>(s => s.Id == "ServiceId" && s.Split.Allocations == new Dictionary<string, double?>()),
                new Version { Id = "VersionId" }, true);

            MenuItem logsMenuItem = objectUnderTest.ContextMenu.ItemsSource.OfType<MenuItem>().Single(
                mi => mi.Header.Equals(Resources.CloudExplorerLaunchLogsViewerMenuHeader));
            logsMenuItem.Command.Execute(null);

            StringAssert.Contains(filter, "resource.type=\"gae_app\"");
            StringAssert.Contains(filter, "resource.labels.module_id=\"ServiceId\"");
            StringAssert.Contains(filter, "resource.labels.version_id=\"VersionId\"");
        }

        [TestMethod]
        public void TestOnMigrateTrafficCommandAsync()
        {
            const string versionId = "VersionId";
            const string serviceId = "ServiceId";
            var service = new Service
            {
                Id = serviceId,
                Split = new TrafficSplit { Allocations = new Dictionary<string, double?> { [versionId] = .5 } }
            };
            var version = new Version { Id = versionId, ServingStatus = GaeVersionExtensions.ServingStatus };
            var ownerMock = new Mock<IGaeSourceRootViewModel> { DefaultValueProvider = DefaultValueProvider.Mock };
            ownerMock.Setup(o => o.DataSource.UpdateServiceTrafficSplitAsync(It.IsAny<TrafficSplit>(), serviceId))
                .Returns(Task.CompletedTask);
            ownerMock.Setup(o => o.InvalidateServiceAsync(serviceId)).Returns(Task.CompletedTask);
            var objectUnderTest = new VersionViewModel(
                ownerMock.Object,
                service,
                version,
                true);

            ICommand onMigrateTrafficCommand = objectUnderTest.ContextMenu
                .ItemsSource.OfType<MenuItem>()
                .Single(mi => (string)mi.Header == Resources.CloudExplorerGaeMigrateAllTrafficHeader)
                .Command;

            onMigrateTrafficCommand.Execute(null);

            Assert.IsTrue(objectUnderTest.IsLoading);
            ownerMock.Verify(
                o => o.DataSource.UpdateServiceTrafficSplitAsync(
                    It.Is<TrafficSplit>(ts => Math.Abs(ts.Allocations[versionId].GetValueOrDefault() - 1.0) < .01),
                    serviceId),
                Times.Once);
            ownerMock.Verify(o => o.InvalidateServiceAsync(serviceId), Times.Once);
        }

        [TestMethod]
        public void TestOnStartVersionAsync()
        {
            const string versionId = "VersionId";
            const string serviceId = "ServiceId";
            var service = new Service
            {
                Id = serviceId, Split = new TrafficSplit { Allocations = new Dictionary<string, double?>() }
            };
            var version = new Version { Id = versionId, ServingStatus = GaeVersionExtensions.StoppedStatus };
            var ownerMock = new Mock<IGaeSourceRootViewModel> { DefaultValueProvider = DefaultValueProvider.Mock };
            ownerMock.Setup(
                    o => o.DataSource.UpdateVersionServingStatus(
                        GaeVersionExtensions.ServingStatus,
                        serviceId,
                        versionId))
                .Returns(Task.CompletedTask);
            ownerMock.Setup(o => o.InvalidateServiceAsync(serviceId)).Returns(Task.CompletedTask);
            var objectUnderTest = new VersionViewModel(
                ownerMock.Object,
                service,
                version,
                true);
            objectUnderTest.Children.Add(null);

            ICommand onStartVersion = objectUnderTest.ContextMenu
                .ItemsSource.OfType<MenuItem>()
                .Single(mi => (string)mi.Header == Resources.CloudExplorerGaeStartVersion)
                .Command;

            onStartVersion.Execute(null);

            Assert.IsTrue(objectUnderTest.IsLoading);
            Assert.AreEqual(0, objectUnderTest.Children.Count);
            ownerMock.Verify(
                o => o.DataSource.UpdateVersionServingStatus(
                    GaeVersionExtensions.ServingStatus,
                    serviceId,
                    versionId),
                Times.Once);
            ownerMock.Verify(o => o.InvalidateServiceAsync(serviceId), Times.Once);
        }

        [TestMethod]
        public void TestOnStopVersionAsync()
        {
            const string versionId = "VersionId";
            const string serviceId = "ServiceId";
            var service = new Service
            {
                Id = serviceId, Split = new TrafficSplit { Allocations = new Dictionary<string, double?>() }
            };
            var version = new Version { Id = versionId, ServingStatus = GaeVersionExtensions.ServingStatus };
            var ownerMock = new Mock<IGaeSourceRootViewModel> { DefaultValueProvider = DefaultValueProvider.Mock };
            var objectUnderTest = new VersionViewModel(
                ownerMock.Object,
                service,
                version,
                true);
            ownerMock.Setup(
                    o => o.DataSource.UpdateVersionServingStatus(
                        GaeVersionExtensions.StoppedStatus,
                        serviceId,
                        versionId))
                .Returns(Task.CompletedTask);
            ownerMock.Setup(o => o.InvalidateServiceAsync(serviceId)).Returns(Task.CompletedTask);
            objectUnderTest.Children.Add(null);

            ICommand onStartVersion = objectUnderTest.ContextMenu
                .ItemsSource.OfType<MenuItem>()
                .Single(mi => (string)mi.Header == Resources.CloudExplorerGaeStopVersion)
                .Command;

            onStartVersion.Execute(null);

            Assert.IsTrue(objectUnderTest.IsLoading);
            Assert.AreEqual(0, objectUnderTest.Children.Count);
            ownerMock.Verify(
                o => o.DataSource.UpdateVersionServingStatus(
                    GaeVersionExtensions.StoppedStatus,
                    serviceId,
                    versionId),
                Times.Once);
            ownerMock.Verify(o => o.InvalidateServiceAsync(serviceId), Times.Once);
        }

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private static IEnumerable<object[]> UpdateServingStatusAsyncErrorData { get; } = new[]
        {
            new object[] { new DataSourceException(), Resources.CloudExplorerGaeUpdateServingStatusErrorMessage },
            new object[] { new TimeoutException(), Resources.CloudExploreOperationTimeoutMessage },
            new object[] { new OperationCanceledException(), Resources.CloudExploreOperationCanceledMessage }
        };

        [TestMethod]
        [DynamicData(nameof(UpdateServingStatusAsyncErrorData))]
        public void TestUpdateServingStatusAsync_OnError(Exception e, string expectedCaption)
        {
            const string versionId = "VersionId";
            const string serviceId = "ServiceId";
            var service = new Service
            {
                Id = serviceId, Split = new TrafficSplit { Allocations = new Dictionary<string, double?>() }
            };
            var version = new Version { Id = versionId, ServingStatus = GaeVersionExtensions.ServingStatus };
            var ownerMock = new Mock<IGaeSourceRootViewModel> { DefaultValueProvider = DefaultValueProvider.Mock };
            var objectUnderTest = new VersionViewModel(
                ownerMock.Object,
                service,
                version,
                true);
            ownerMock.Setup(
                    o => o.DataSource.UpdateVersionServingStatus(
                        GaeVersionExtensions.StoppedStatus,
                        serviceId,
                        versionId))
                .Returns(Task.FromException(e));

            ICommand onStartVersion = objectUnderTest.ContextMenu
                .ItemsSource.OfType<MenuItem>()
                .Single(mi => (string)mi.Header == Resources.CloudExplorerGaeStopVersion)
                .Command;

            onStartVersion.Execute(null);


            Assert.IsFalse(objectUnderTest.IsLoading);
            Assert.IsTrue(objectUnderTest.IsError);
            Assert.AreEqual(expectedCaption, objectUnderTest.Caption);
        }

        [TestMethod]
        public void TestOnDeleteVersionAsync()
        {
            const string versionId = "VersionId";
            const string serviceId = "ServiceId";
            var service = new Service
            {
                Id = serviceId, Split = new TrafficSplit { Allocations = new Dictionary<string, double?>() }
            };
            var version = new Version { Id = versionId, ServingStatus = GaeVersionExtensions.ServingStatus };
            var ownerMock = new Mock<IGaeSourceRootViewModel> { DefaultValueProvider = DefaultValueProvider.Mock };
            var objectUnderTest = new VersionViewModel(
                ownerMock.Object,
                service,
                version,
                false);
            PackageMock.Setup(
                    p => p.UserPromptService.ActionPrompt(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>()))
                .Returns(true);
            ownerMock.Setup(o => o.DataSource.DeleteVersionAsync(serviceId, versionId)).Returns(Task.CompletedTask);
            ownerMock.Setup(o => o.InvalidateServiceAsync(serviceId)).Returns(Task.CompletedTask);

            objectUnderTest.ContextMenu
                .ItemsSource.OfType<MenuItem>()
                .Single(mi => (string)mi.Header == Resources.CloudExplorerGaeDeleteVersion)
                .Command.Execute(null);

            Assert.IsTrue(objectUnderTest.IsLoading);
            ownerMock.Verify(o => o.DataSource.DeleteVersionAsync(serviceId, versionId), Times.Once);
            ownerMock.Verify(o => o.InvalidateServiceAsync(serviceId), Times.Once);
        }

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        private static IEnumerable<object[]> OnDeleteVersionAsyncErrorData { get; } = new[]
        {
            new object[] { new DataSourceException(), Resources.CloudExplorerGaeDeleteVersionErrorMessage },
            new object[] { new TimeoutException(), Resources.CloudExploreOperationTimeoutMessage },
            new object[] { new OperationCanceledException(), Resources.CloudExploreOperationCanceledMessage }
        };

        [TestMethod]
        [DynamicData(nameof(OnDeleteVersionAsyncErrorData))]
        public void TestOnDeleteVersionAsync_Error(Exception e, string expectedCaption)
        {
            const string versionId = "VersionId";
            const string serviceId = "ServiceId";
            var service = new Service
            {
                Id = serviceId, Split = new TrafficSplit { Allocations = new Dictionary<string, double?>() }
            };
            var version = new Version { Id = versionId, ServingStatus = GaeVersionExtensions.ServingStatus };
            var ownerMock = new Mock<IGaeSourceRootViewModel> { DefaultValueProvider = DefaultValueProvider.Mock };
            var objectUnderTest = new VersionViewModel(
                ownerMock.Object,
                service,
                version,
                false);
            PackageMock.Setup(
                    p => p.UserPromptService.ActionPrompt(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<bool>()))
                .Returns(true);
            ownerMock.Setup(o => o.DataSource.DeleteVersionAsync(serviceId, versionId)).Returns(Task.FromException(e));

            objectUnderTest.ContextMenu
                .ItemsSource.OfType<MenuItem>()
                .Single(mi => (string)mi.Header == Resources.CloudExplorerGaeDeleteVersion)
                .Command.Execute(null);

            Assert.IsFalse(objectUnderTest.IsLoading);
            Assert.IsTrue(objectUnderTest.IsError);
            Assert.AreEqual(expectedCaption, objectUnderTest.Caption);
        }

        [TestMethod]
        public void TestOnOpenOnCloudConsoleCommand()
        {
            const string versionId = "VersionId";
            const string serviceId = "ServiceId";
            const string projectId = "ProjectId";
            var browserServiceMock = new Mock<IBrowserService>();
            PackageMock.Setup(p => p.GetMefServiceLazy<IBrowserService>()).Returns(browserServiceMock.ToLazy());
            var service = new Service
            {
                Id = serviceId, Split = new TrafficSplit { Allocations = new Dictionary<string, double?>() }
            };
            var version = new Version { Id = versionId, ServingStatus = GaeVersionExtensions.ServingStatus };
            var ownerMock = new Mock<IGaeSourceRootViewModel> { DefaultValueProvider = DefaultValueProvider.Mock };
            var objectUnderTest = new VersionViewModel(
                ownerMock.Object,
                service,
                version,
                false);

            ownerMock.Setup(o => o.Context.CurrentProject).Returns(new Project { ProjectId = projectId });
            objectUnderTest.ContextMenu
                .ItemsSource.OfType<MenuItem>()
                .Single(mi => (string)mi.Header == Resources.UiOpenOnCloudConsoleMenuHeader)
                .Command.Execute(null);

            browserServiceMock.Verify(
                b => b.OpenBrowser(
                    It.Is<string>(
                        s => s.StartsWith(VersionViewModel.CloudConsoleServiceInstanceUrl, StringComparison.Ordinal) &&
                            s.Contains(versionId) &&
                            s.Contains(serviceId) &&
                            s.Contains(projectId))));
        }

        [TestMethod]
        public void TestOnOpenVersion()
        {
            const string versionId = "VersionId";
            const string serviceId = "ServiceId";
            const string versionUrl = "VersionUrl";
            var browserServiceMock = new Mock<IBrowserService>();
            PackageMock.Setup(p => p.GetMefServiceLazy<IBrowserService>()).Returns(browserServiceMock.ToLazy());
            var service = new Service
            {
                Id = serviceId, Split = new TrafficSplit { Allocations = new Dictionary<string, double?>() }
            };
            var version = new Version
            {
                Id = versionId,
                ServingStatus = GaeVersionExtensions.ServingStatus,
                VersionUrl = versionUrl
            };
            var ownerMock = new Mock<IGaeSourceRootViewModel> { DefaultValueProvider = DefaultValueProvider.Mock };
            var objectUnderTest = new VersionViewModel(
                ownerMock.Object,
                service,
                version,
                false);

            objectUnderTest.ContextMenu
                .ItemsSource.OfType<MenuItem>()
                .Single(mi => (string)mi.Header == Resources.CloudExplorerGaeVersionOpen)
                .Command.Execute(null);

            browserServiceMock.Verify(b => b.OpenBrowser(versionUrl));
        }
    }
}
