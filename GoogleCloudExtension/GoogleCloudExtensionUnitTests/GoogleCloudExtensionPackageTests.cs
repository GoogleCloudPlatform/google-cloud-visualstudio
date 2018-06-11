// Copyright 2016 Google Inc. All Rights Reserved.
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

using EnvDTE;
using GoogleAnalyticsUtils;
using GoogleCloudExtension;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.Options;
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.StackdriverLogsViewer;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Xml.Linq;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Window = EnvDTE.Window;

namespace GoogleCloudExtensionUnitTests
{
    /// <summary>
    /// Tests for <see cref="GoogleCloudExtensionPackage"/> class.
    /// </summary>
    [TestClass]
    [DeploymentItem(VsixManifestFileName)]
    public class GoogleCloudExtensionPackageTests : MockedGlobalServiceProviderTestsBase
    {
        private Mock<IEventsReporter> _reporterMock;
        private GoogleCloudExtensionPackage _objectUnderTest;
        protected override IVsPackage Package => _objectUnderTest;
        private const string ExpectedAssemblyName = "google-cloud-visualstudio";
        private const string VsixManifestFileName = "source.extension.vsixmanifest";

        [TestInitialize]
        public void BeforeEach()
        {
            _reporterMock = new Mock<IEventsReporter>();
            EventsReporterWrapper.ReporterLazy = new Lazy<IEventsReporter>(() => _reporterMock.Object);
            _objectUnderTest = new GoogleCloudExtensionPackage();
        }

        [TestMethod]
        public void TestPackageValues()
        {
            const string mockedVersion = "MockVsVersion";
            const string mockedEdition = "MockedEdition";
            DteMock.Setup(dte => dte.Version).Returns(mockedVersion);
            DteMock.Setup(dte => dte.Edition).Returns(mockedEdition);

            RunPackageInitalize();

            string expectedAssemblyVersion = GetVsixManifestVersion();
            Assert.AreEqual(mockedVersion, GoogleCloudExtensionPackage.Instance.VsVersion);
            Assert.AreEqual(mockedEdition, GoogleCloudExtensionPackage.VsEdition);
            Assert.AreEqual(ExpectedAssemblyName, GoogleCloudExtensionPackage.Instance.ApplicationName);
            Assert.AreEqual(expectedAssemblyVersion, GoogleCloudExtensionPackage.Instance.ApplicationVersion);
            Assert.AreEqual(
                $"{ExpectedAssemblyName}/{expectedAssemblyVersion}",
                GoogleCloudExtensionPackage.Instance.VersionedApplicationName);
            Assert.AreEqual(
                GoogleCloudExtensionPackage.Instance.ApplicationVersion,
                GoogleCloudExtensionPackage.Instance.AnalyticsSettings.InstalledVersion);
            Assert.IsNull(GoogleCloudExtensionPackage.Instance.AnalyticsSettings.ClientId);
            Assert.IsFalse(GoogleCloudExtensionPackage.Instance.AnalyticsSettings.DialogShown);
            Assert.IsFalse(GoogleCloudExtensionPackage.Instance.AnalyticsSettings.OptIn);
        }

        [TestMethod]
        public void TestUpdatePackageVersion()
        {
            _objectUnderTest.AnalyticsSettings.InstalledVersion = "0.1.0.0";

            RunPackageInitalize();

            Assert.AreEqual(
                GoogleCloudExtensionPackage.Instance.ApplicationVersion,
                GoogleCloudExtensionPackage.Instance.AnalyticsSettings.InstalledVersion);
            _reporterMock.Verify(
                r => r.ReportEvent(
                    It.IsAny<string>(), It.IsAny<string>(), UpgradeEvent.UpgradeEventName, It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
        }

        [TestMethod]
        public void TestNewPackageInstallation()
        {
            RunPackageInitalize();

            Assert.AreEqual(
                GoogleCloudExtensionPackage.Instance.ApplicationVersion,
                GoogleCloudExtensionPackage.Instance.AnalyticsSettings.InstalledVersion);
            _reporterMock.Verify(
                r => r.ReportEvent(
                    It.IsAny<string>(), It.IsAny<string>(), NewInstallEvent.NewInstallEventName, It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
        }

        [TestMethod]
        public void TestSamePackageVersion()
        {
            _objectUnderTest.AnalyticsSettings.InstalledVersion =
                typeof(GoogleCloudExtensionPackage).Assembly.GetName().Version.ToString();

            RunPackageInitalize();

            Assert.AreEqual(
                GoogleCloudExtensionPackage.Instance.ApplicationVersion,
                GoogleCloudExtensionPackage.Instance.AnalyticsSettings.InstalledVersion);
            _reporterMock.Verify(
                r => r.ReportEvent(
                    It.IsAny<string>(), It.IsAny<string>(), NewInstallEvent.NewInstallEventName, It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
            _reporterMock.Verify(
                r => r.ReportEvent(
                    It.IsAny<string>(), It.IsAny<string>(), UpgradeEvent.UpgradeEventName, It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()), Times.Never);
        }

        [TestMethod]
        public void TestWindowActiveWhenNormalState()
        {
            DteMock.Setup(d => d.MainWindow).Returns(Mock.Of<Window>(w => w.WindowState == vsWindowState.vsWindowStateNormal));

            RunPackageInitalize();

            Assert.IsTrue(_objectUnderTest.IsWindowActive());
        }

        [TestMethod]
        public void TestWindowActiveWhenMaximizedState()
        {
            DteMock.Setup(d => d.MainWindow).Returns(Mock.Of<Window>(w => w.WindowState == vsWindowState.vsWindowStateMaximize));

            RunPackageInitalize();

            Assert.IsTrue(_objectUnderTest.IsWindowActive());
        }

        [TestMethod]
        public void TestWindowActiveWhenMinimizedState()
        {
            DteMock.Setup(d => d.MainWindow).Returns(Mock.Of<Window>(w => w.WindowState == vsWindowState.vsWindowStateMinimize));

            RunPackageInitalize();

            Assert.IsFalse(_objectUnderTest.IsWindowActive());
        }

        [TestMethod]
        public void TestGetServiceSI_GetsServiceOfTypeIRegisteredByS()
        {
            Mock<IVsSolution> solutionMock = ServiceProviderMock.SetupService<SVsSolution, IVsSolution>();
            RunPackageInitalize();

            IVsSolution service = _objectUnderTest.GetService<SVsSolution, IVsSolution>();

            Assert.AreEqual(solutionMock.Object, service);
        }

        [TestMethod]
        public void TestGetMefService_GetsServiceFromMef()
        {
            var mockedFileSystemService = Mock.Of<IFileSystem>();
            ComponentModelMock.Setup(s => s.GetService<IFileSystem>()).Returns(mockedFileSystemService);
            RunPackageInitalize();

            var service = _objectUnderTest.GetMefService<IFileSystem>();

            Assert.AreEqual(mockedFileSystemService, service);
        }

        [TestMethod]
        public void TestGetMefServiceLazy_GetsLazyServiceFromMef()
        {
            var exportProvider = new FakeExportProvider<IFileSystem>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);
            RunPackageInitalize();

            Lazy<IFileSystem> service = _objectUnderTest.GetMefServiceLazy<IFileSystem>();

            Assert.AreEqual(exportProvider.MockedValue, service.Value);
        }

        [TestMethod]
        public void TestShowOptionPage_OptionPage()
        {
            RunPackageInitalize();

            _objectUnderTest.ShowOptionPage<AnalyticsOptions>();
        }

        [TestMethod]
        public void TestShellUtils_Initalized()
        {
            var exportProvider = new FakeExportProvider<IShellUtils>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);

            RunPackageInitalize();

            Assert.AreEqual(exportProvider.MockedValue, _objectUnderTest.ShellUtils);
        }

        [TestMethod]
        public void TestGcpOutputWindow_Initalized()
        {
            var exportProvider = new FakeExportProvider<IGcpOutputWindow>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);

            RunPackageInitalize();
            Assert.AreEqual(exportProvider.MockedValue, _objectUnderTest.GcpOutputWindow);
        }

        [TestMethod]
        public void TestSubscribeClosingEvent()
        {
            var eventHandlerMock = new Mock<Action<object, EventArgs>>();
            RunPackageInitalize();

            _objectUnderTest.SubscribeClosingEvent(new EventHandler(eventHandlerMock.Object));
            ((IVsPackage)_objectUnderTest).QueryClose(out _);

            eventHandlerMock.Verify(f => f(It.IsAny<object>(), It.IsAny<EventArgs>()));
        }

        [TestMethod]
        public void TestUnsubscribeClosingEvent()
        {
            var eventHandlerMock = new Mock<Action<object, EventArgs>>();
            var mockedHandler = new EventHandler(eventHandlerMock.Object);
            RunPackageInitalize();
            _objectUnderTest.SubscribeClosingEvent(mockedHandler);

            _objectUnderTest.UnsubscribeClosingEvent(mockedHandler);
            ((IVsPackage)_objectUnderTest).QueryClose(out _);

            eventHandlerMock.Verify(f => f(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Never);
        }

        [TestMethod]
        public void TestFindToolWindow_ReturnsNullForCreateFalse()
        {
            var toolWindow = _objectUnderTest.FindToolWindow<CloudExplorerToolWindow>(false);

            Assert.IsNull(toolWindow);
        }

        [TestMethod]
        public void TestFindToolWindow_ReturnsInstanceForCreateTrue()
        {
            Mock<IVsUIShell> uiShellMock = ServiceProviderMock.SetupService<SVsUIShell, IVsUIShell>();
            Guid clsid = Guid.Empty;
            Guid activate = Guid.Empty;
            Guid persistenceSlot = typeof(LogsViewerToolWindow).GUID;
            IVsWindowFrame frame = VsWindowFrameMocks.GetMockedWindowFrame();
            uiShellMock.Setup(
                shell => shell.CreateToolWindow(
                    It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<object>(), ref
                    clsid, ref persistenceSlot, ref activate, It.IsAny<IServiceProvider>(), It.IsAny<string>(),
                    It.IsAny<int[]>(),
                    out frame)).Returns(VSConstants.S_OK);

            RunPackageInitalize();
            var toolWindow = _objectUnderTest.FindToolWindow<LogsViewerToolWindow>(true);

            Assert.IsNotNull(toolWindow);
        }

        [TestMethod]
        public void TestFindToolWindow_ReturnsExistingInstance()
        {
            Mock<IVsUIShell> uiShellMock = ServiceProviderMock.SetupService<SVsUIShell, IVsUIShell>();
            Guid clsid = Guid.Empty;
            Guid activate = Guid.Empty;
            Guid persistenceSlot = typeof(LogsViewerToolWindow).GUID;
            IVsWindowFrame frame = VsWindowFrameMocks.GetMockedWindowFrame();
            uiShellMock.Setup(
                shell => shell.CreateToolWindow(
                    It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<object>(), ref
                    clsid, ref persistenceSlot, ref activate, It.IsAny<IServiceProvider>(), It.IsAny<string>(),
                    It.IsAny<int[]>(),
                    out frame)).Returns(VSConstants.S_OK);

            RunPackageInitalize();
            var toolWindow = _objectUnderTest.FindToolWindow<LogsViewerToolWindow>(true);
            var existingWindow = _objectUnderTest.FindToolWindow<LogsViewerToolWindow>(false);

            Assert.AreEqual(toolWindow, existingWindow);
        }

        private static string GetVsixManifestVersion()
        {
            XDocument vsixManifest = XDocument.Load(VsixManifestFileName);
            XNamespace ns = vsixManifest.Root?.Name.Namespace ?? XNamespace.None;
            XElement manifestRoot = vsixManifest.Element(ns.GetName("PackageManifest"));
            XElement metadata = manifestRoot?.Element(ns.GetName("Metadata"));
            XElement identity = metadata?.Element(ns.GetName("Identity"));
            return identity?.Attribute("Version")?.Value;
        }

        private class FakeExportProvider<T> : ExportProvider where T : class
        {
            public readonly T MockedValue = Mock.Of<T>();

            /// <summary>Gets the single export for <typeparamref name="T"/>.</summary>
            /// <returns>
            /// An <see cref="Export"/>[] containing a single export that refers to the mocked value.
            /// </returns>
            /// <param name="definition">
            /// The object that defines the conditions of the <see cref="Export" /> objects to return.
            /// </param>
            /// <param name="atomicComposition">The transactional container for the composition.</param>
            protected override IEnumerable<Export> GetExportsCore(
                ImportDefinition definition,
                AtomicComposition atomicComposition) => new[] { new Export(nameof(T), () => MockedValue) };
        }
    }
}
