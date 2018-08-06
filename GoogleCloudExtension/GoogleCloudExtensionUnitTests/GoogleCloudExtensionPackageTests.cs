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
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.Analytics.Events;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.MenuBarControls;
using GoogleCloudExtension.Options;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.StackdriverLogsViewer;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Xml.Linq;
using TestingHelpers;
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
        private const string ExpectedAssemblyName = "google-cloud-visualstudio";
        private const string VsixManifestFileName = "source.extension.vsixmanifest";

        private GoogleCloudExtensionPackage _objectUnderTest;
        private Mock<IEventsReporter> _reporterMock;
        private Mock<IVsRegisterUIFactories> _registerUiFactoryMock;

        protected override IVsPackage Package => _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            // Initalize the export provider to get types exported in GoogleCloudExtension.dll.
            DelegatingServiceProvider.Delegate = _objectUnderTest;
            var container = new CompositionContainer(
                new AggregateCatalog(
                    new AssemblyCatalog(typeof(GoogleCloudExtensionPackage).Assembly),
                    new TypeCatalog(typeof(DelegatingServiceProvider))));
            ComponentModelMock.Setup(cm => cm.DefaultExportProvider).Returns(container);
            ComponentModelMock.Setup(cm => cm.GetService<GcpMenuBarControlFactory>())
                .Returns(new GcpMenuBarControlFactory(MockHelpers.LazyOf<IGcpMenuBarControl>()));

            _reporterMock = new Mock<IEventsReporter>();
            EventsReporterWrapper.ReporterLazy = _reporterMock.ToLazy();
            _objectUnderTest = new GoogleCloudExtensionPackage();

            _registerUiFactoryMock = ServiceProviderMock.SetupService<SVsUIFactory, IVsRegisterUIFactories>();
            Guid menuBarControlFactoryGuid = typeof(GcpMenuBarControlFactory).GUID;
            _registerUiFactoryMock.Setup(
                f => f.RegisterUIFactory(ref menuBarControlFactoryGuid, It.IsAny<IVsUIFactory>()));
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
                GoogleCloudExtensionPackage.Instance.GeneralSettings.InstalledVersion);
            Assert.IsNull(GoogleCloudExtensionPackage.Instance.GeneralSettings.ClientId);
            Assert.IsFalse(GoogleCloudExtensionPackage.Instance.GeneralSettings.DialogShown);
            Assert.IsFalse(GoogleCloudExtensionPackage.Instance.GeneralSettings.OptIn);
        }

        [TestMethod]
        public void TestUpdatePackageVersion()
        {
            _objectUnderTest.GeneralSettings.InstalledVersion = "0.1.0.0";

            RunPackageInitalize();

            Assert.AreEqual(
                GoogleCloudExtensionPackage.Instance.ApplicationVersion,
                GoogleCloudExtensionPackage.Instance.GeneralSettings.InstalledVersion);
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
                GoogleCloudExtensionPackage.Instance.GeneralSettings.InstalledVersion);
            _reporterMock.Verify(
                r => r.ReportEvent(
                    It.IsAny<string>(), It.IsAny<string>(), NewInstallEvent.NewInstallEventName, It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
        }

        [TestMethod]
        public void TestSamePackageVersion()
        {
            GoogleCloudExtensionPackage.Instance = _objectUnderTest;
            _objectUnderTest.GeneralSettings.InstalledVersion =
                typeof(GoogleCloudExtensionPackage).Assembly.GetName().Version.ToString();

            RunPackageInitalize();

            Assert.AreEqual(
                GoogleCloudExtensionPackage.Instance.ApplicationVersion,
                GoogleCloudExtensionPackage.Instance.GeneralSettings.InstalledVersion);
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
            // ReSharper disable once RedundantAssignment
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
            // ReSharper disable once RedundantAssignment
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

        [TestMethod]
        public void TestProcessService_Initalized()
        {
            var exportProvider = new FakeExportProvider<IProcessService>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);

            RunPackageInitalize();

            Assert.AreEqual(exportProvider.MockedValue, _objectUnderTest.ProcessService);
        }

        [TestMethod]
        public void TestStatusbarHelper_Initalized()
        {
            var exportProvider = new FakeExportProvider<IStatusbarService>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);

            RunPackageInitalize();

            Assert.AreEqual(exportProvider.MockedValue, _objectUnderTest.StatusbarHelper);
        }

        [TestMethod]
        public void TestUserPromptService_Initalized()
        {
            var exportProvider = new FakeExportProvider<IUserPromptService>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);

            RunPackageInitalize();

            Assert.AreEqual(exportProvider.MockedValue, _objectUnderTest.UserPromptService);
        }

        [TestMethod]
        public void TestDataSourceFactory_Initalized()
        {
            var exportProvider = new FakeExportProvider<IDataSourceFactory>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);

            RunPackageInitalize();

            Assert.AreEqual(exportProvider.MockedValue, _objectUnderTest.DataSourceFactory);
        }

        [TestMethod]
        public void TestCredentialsStore_Initalized()
        {
            var mockedCredentialStore = Mock.Of<ICredentialsStore>();
            ComponentModelMock.Setup(s => s.GetService<ICredentialsStore>()).Returns(mockedCredentialStore);

            RunPackageInitalize();

            Assert.AreEqual(mockedCredentialStore, _objectUnderTest.CredentialsStore);
        }

        [TestMethod]
        public void TestCredentialsStore_CurrentProjectIdChangedSubscribed()
        {
            var exportProvider = new FakeExportsProvider();
            var credentialsStoreMock = new Mock<ICredentialsStore>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);
            ComponentModelMock.Setup(s => s.GetService<ICredentialsStore>()).Returns(credentialsStoreMock.Object);

            RunPackageInitalize();
            credentialsStoreMock.Raise(cs => cs.CurrentProjectIdChanged += null, EventArgs.Empty);

            var shellUtilsMock = (Mock<IShellUtils>)exportProvider.MockObjects[typeof(IShellUtils)];
            shellUtilsMock.Verify(su => su.InvalidateCommandsState());
        }

        [TestMethod]
        public void TestInitalize_RegistersGcpMeuBarControlFactory()
        {
            RunPackageInitalize();

            Guid factoryGuid = typeof(GcpMenuBarControlFactory).GUID;
            _registerUiFactoryMock.Verify(
                f => f.RegisterUIFactory(ref factoryGuid, It.IsAny<GcpMenuBarControlFactory>()));
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
                AtomicComposition atomicComposition) => new[]
            {
                new Export(
                    definition.ContractName,
                    () => definition.ContractName == typeof(T).FullName ? MockedValue : null)
            };
        }

        private class FakeExportsProvider : ExportProvider
        {
            private readonly Dictionary<Type, Mock> _mockObjects = new Dictionary<Type, Mock>();
            public IReadOnlyDictionary<Type, Mock> MockObjects => _mockObjects;

            /// <summary>Gets all the exports that match the constraint defined by the specified definition.</summary>
            /// <returns>A collection that contains all the exports that match the specified condition.</returns>
            /// <param name="definition">The object that defines the conditions of the <see cref="T:System.ComponentModel.Composition.Primitives.Export" /> objects to return.</param>
            /// <param name="atomicComposition">The transactional container for the composition.</param>
            protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition atomicComposition)
            {

                Type contractType = typeof(GoogleCloudExtensionPackage).Assembly.GetType(definition.ContractName);

                return new[]
                {
                    new Export(definition.ContractName, GetMockedExport)
                };

                object GetMockedExport()
                {
                    if (!_mockObjects.ContainsKey(contractType))
                    {
                        var mock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(contractType));
                        _mockObjects[contractType] = mock;
                    }

                    return _mockObjects[contractType].Object;
                }
            }
        }

        [Export(typeof(SVsServiceProvider))]
        public class DelegatingServiceProvider : SVsServiceProvider
        {
            public static System.IServiceProvider Delegate { get; set; }

            /// <summary>Gets the service object of the specified type.</summary>
            /// <returns>A service object of type <paramref name="serviceType" />.-or- null if there is no service object of type <paramref name="serviceType" />.</returns>
            /// <param name="serviceType">An object that specifies the type of service object to get. </param>
            public object GetService(Type serviceType) => Delegate.GetService(serviceType);
        }
    }
}
