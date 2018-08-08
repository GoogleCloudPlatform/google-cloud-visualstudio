﻿// Copyright 2016 Google Inc. All Rights Reserved.
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
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Task = System.Threading.Tasks.Task;
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
        private Mock<IVsRegisterUIFactories> _registerUiFactoryMock;
        private Mock<IEventsReporter> _reporterMock;

        [TestInitialize]
        public void BeforeEach()
        {
            // Initalize the export provider to get types exported in GoogleCloudExtension.dll.
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
        public async Task TestPackageValues()
        {
            const string mockedVersion = "MockVsVersion";
            const string mockedEdition = "MockedEdition";
            DteMock.Setup(dte => dte.Version).Returns(mockedVersion);
            DteMock.Setup(dte => dte.Edition).Returns(mockedEdition);

            await RunPackageInitalizeAsync();

            string expectedAssemblyVersion = GetVsixManifestVersion();
            Assert.AreEqual(mockedVersion, GoogleCloudExtensionPackage.Instance.VsVersion);
            Assert.AreEqual(mockedEdition, GoogleCloudExtensionPackage.Instance.VsEdition);
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
        public async Task TestUpdatePackageVersion()
        {
            _objectUnderTest.GeneralSettings.InstalledVersion = "0.1.0.0";

            await RunPackageInitalizeAsync();

            Assert.AreEqual(
                GoogleCloudExtensionPackage.Instance.ApplicationVersion,
                GoogleCloudExtensionPackage.Instance.GeneralSettings.InstalledVersion);
            _reporterMock.Verify(
                r => r.ReportEvent(
                    It.IsAny<string>(), It.IsAny<string>(), UpgradeEvent.UpgradeEventName, It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
        }

        [TestMethod]
        public async Task TestNewPackageInstallation()
        {
            await RunPackageInitalizeAsync();

            Assert.AreEqual(
                GoogleCloudExtensionPackage.Instance.ApplicationVersion,
                GoogleCloudExtensionPackage.Instance.GeneralSettings.InstalledVersion);
            _reporterMock.Verify(
                r => r.ReportEvent(
                    It.IsAny<string>(), It.IsAny<string>(), NewInstallEvent.NewInstallEventName, It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
        }

        [TestMethod]
        public async Task TestSamePackageVersion()
        {
            GoogleCloudExtensionPackage.Instance = _objectUnderTest;
            _objectUnderTest.GeneralSettings.InstalledVersion =
                typeof(GoogleCloudExtensionPackage).Assembly.GetName().Version.ToString();

            await RunPackageInitalizeAsync();

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
        public async Task TestWindowActiveWhenNormalState()
        {
            DteMock.Setup(d => d.MainWindow).Returns(Mock.Of<Window>(w => w.WindowState == vsWindowState.vsWindowStateNormal));

            await RunPackageInitalizeAsync();

            Assert.IsTrue(_objectUnderTest.IsWindowActive());
        }

        [TestMethod]
        public async Task TestWindowActiveWhenMaximizedState()
        {
            DteMock.Setup(d => d.MainWindow).Returns(Mock.Of<Window>(w => w.WindowState == vsWindowState.vsWindowStateMaximize));

            await RunPackageInitalizeAsync();

            Assert.IsTrue(_objectUnderTest.IsWindowActive());
        }

        [TestMethod]
        public async Task TestWindowActiveWhenMinimizedState()
        {
            DteMock.Setup(d => d.MainWindow).Returns(Mock.Of<Window>(w => w.WindowState == vsWindowState.vsWindowStateMinimize));

            await RunPackageInitalizeAsync();

            Assert.IsFalse(_objectUnderTest.IsWindowActive());
        }

        [TestMethod]
        public async Task TestGetServiceSI_GetsServiceOfTypeIRegisteredByS()
        {
            Mock<IVsSolution> solutionMock = ServiceProviderMock.SetupService<SVsSolution, IVsSolution>();
            await RunPackageInitalizeAsync();

            IVsSolution service = _objectUnderTest.GetService<SVsSolution, IVsSolution>();

            Assert.AreEqual(solutionMock.Object, service);
        }

        [TestMethod]
        public async Task TestGetMefService_GetsServiceFromMef()
        {
            var mockedFileSystemService = Mock.Of<IFileSystem>();
            ComponentModelMock.Setup(s => s.GetService<IFileSystem>()).Returns(mockedFileSystemService);
            await RunPackageInitalizeAsync();

            var service = _objectUnderTest.GetMefService<IFileSystem>();

            Assert.AreEqual(mockedFileSystemService, service);
        }

        [TestMethod]
        public async Task TestGetMefServiceLazy_GetsLazyServiceFromMef()
        {
            var exportProvider = new FakeExportProvider<IFileSystem>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);
            await RunPackageInitalizeAsync();

            Lazy<IFileSystem> service = _objectUnderTest.GetMefServiceLazy<IFileSystem>();

            Assert.AreEqual(exportProvider.MockedValue, service.Value);
        }

        [TestMethod]
        public async Task TestShowOptionPage_OptionPage()
        {
            await RunPackageInitalizeAsync();

            _objectUnderTest.ShowOptionPage<AnalyticsOptions>();
        }

        [TestMethod]
        public async Task TestShellUtils_Initalized()
        {
            var exportProvider = new FakeExportProvider<IShellUtils>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);

            await RunPackageInitalizeAsync();

            Assert.AreEqual(exportProvider.MockedValue, _objectUnderTest.ShellUtils);
        }

        [TestMethod]
        public async Task TestGcpOutputWindow_Initalized()
        {
            var exportProvider = new FakeExportProvider<IGcpOutputWindow>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);

            await RunPackageInitalizeAsync();
            Assert.AreEqual(exportProvider.MockedValue, _objectUnderTest.GcpOutputWindow);
        }

        [TestMethod]
        public async Task TestSubscribeClosingEvent()
        {
            var eventHandlerMock = new Mock<Action<object, EventArgs>>();
            await RunPackageInitalizeAsync();

            _objectUnderTest.SubscribeClosingEvent(new EventHandler(eventHandlerMock.Object));
            ((IVsPackage)_objectUnderTest).QueryClose(out _);

            eventHandlerMock.Verify(f => f(It.IsAny<object>(), It.IsAny<EventArgs>()));
        }

        [TestMethod]
        public async Task TestUnsubscribeClosingEvent()
        {
            var eventHandlerMock = new Mock<Action<object, EventArgs>>();
            var mockedHandler = new EventHandler(eventHandlerMock.Object);
            await RunPackageInitalizeAsync();
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
        public async Task TestFindToolWindow_ReturnsInstanceForCreateTrue()
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

            await RunPackageInitalizeAsync();
            var toolWindow = _objectUnderTest.FindToolWindow<LogsViewerToolWindow>(true);

            Assert.IsNotNull(toolWindow);
        }

        [TestMethod]
        public async Task TestFindToolWindow_ReturnsExistingInstance()
        {
            Mock<IVsUIShell> uiShellMock = ServiceProviderMock.SetupService<SVsUIShell, IVsUIShell>();
            Guid clsid = Guid.Empty;
            Guid activate = Guid.Empty;
            Guid persistenceSlot = typeof(LogsViewerToolWindow).GUID;
            // ReSharper disable once RedundantAssignment
            IVsWindowFrame frame = VsWindowFrameMocks.GetMockedWindowFrame();
            uiShellMock.Setup(
                    shell => shell.CreateToolWindow(
                        It.IsAny<uint>(),
                        It.IsAny<uint>(),
                        It.IsAny<object>(),
                        ref
                        clsid,
                        ref persistenceSlot,
                        ref activate,
                        It.IsAny<IServiceProvider>(),
                        It.IsAny<string>(),
                        It.IsAny<int[]>(),
                        out frame))
                .Returns(VSConstants.S_OK);

            await RunPackageInitalizeAsync();
            var toolWindow = _objectUnderTest.FindToolWindow<LogsViewerToolWindow>(true);
            var existingWindow = _objectUnderTest.FindToolWindow<LogsViewerToolWindow>(false);

            Assert.AreEqual(toolWindow, existingWindow);
        }

        [TestMethod]
        public async Task TestProcessService_Initalized()
        {
            var exportProvider = new FakeExportProvider<IProcessService>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);

            await RunPackageInitalizeAsync();

            Assert.AreEqual(exportProvider.MockedValue, _objectUnderTest.ProcessService);
        }

        [TestMethod]
        public async Task TestStatusbarHelper_Initalized()
        {
            var exportProvider = new FakeExportProvider<IStatusbarService>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);

            await RunPackageInitalizeAsync();

            Assert.AreEqual(exportProvider.MockedValue, _objectUnderTest.StatusbarHelper);
        }

        [TestMethod]
        public async Task TestUserPromptService_Initalized()
        {
            var exportProvider = new FakeExportProvider<IUserPromptService>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);

            await RunPackageInitalizeAsync();

            Assert.AreEqual(exportProvider.MockedValue, _objectUnderTest.UserPromptService);
        }

        [TestMethod]
        public async Task TestDataSourceFactory_Initalized()
        {
            var exportProvider = new FakeExportProvider<IDataSourceFactory>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);

            await RunPackageInitalizeAsync();

            Assert.AreEqual(exportProvider.MockedValue, _objectUnderTest.DataSourceFactory);
        }

        [TestMethod]
        public async Task TestCredentialsStore_Initalized()
        {
            var mockedCredentialStore = Mock.Of<ICredentialsStore>();
            ComponentModelMock.Setup(s => s.GetService<ICredentialsStore>()).Returns(mockedCredentialStore);

            await RunPackageInitalizeAsync();

            Assert.AreEqual(mockedCredentialStore, _objectUnderTest.CredentialsStore);
        }

        [TestMethod]
        public async Task TestCredentialsStore_CurrentProjectIdChangedSubscribed()
        {
            var exportProvider = new FakeExportsProvider();
            var credentialsStoreMock = new Mock<ICredentialsStore>();
            ComponentModelMock.Setup(s => s.DefaultExportProvider).Returns(exportProvider);
            ComponentModelMock.Setup(s => s.GetService<ICredentialsStore>()).Returns(credentialsStoreMock.Object);

            await RunPackageInitalizeAsync();
            credentialsStoreMock.Raise(cs => cs.CurrentProjectIdChanged += null, EventArgs.Empty);

            var shellUtilsMock = (Mock<IShellUtils>)exportProvider.MockObjects[typeof(IShellUtils)];
            shellUtilsMock.Verify(su => su.InvalidateCommandsState());
        }

        [TestMethod]
        public async Task TestInitalize_RegistersGcpMeuBarControlFactory()
        {
            await RunPackageInitalizeAsync();

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
            /// <summary>Gets the service object of the specified type.</summary>
            /// <returns>A service object of type <paramref name="serviceType" />.-or- null if there is no service object of type <paramref name="serviceType" />.</returns>
            /// <param name="serviceType">An object that specifies the type of service object to get. </param>
            public object GetService(Type serviceType) => ServiceProvider.GlobalProvider.GetService(serviceType);
        }

        private async Task RunPackageInitalizeAsync()
        {
            var asyncServiceProviderMock =
                new Mock<Microsoft.VisualStudio.Shell.Interop.IAsyncServiceProvider>(MockBehavior.Strict);
            asyncServiceProviderMock.As<IAsyncServiceProvider>()
                .Setup(sp => sp.GetServiceAsync(It.IsAny<Type>()))
                .Returns(
                    async (Type t) =>
                    {
                        await _objectUnderTest.JoinableTaskFactory.SwitchToMainThreadAsync();
                        return ((System.IServiceProvider)_objectUnderTest).GetService(t);
                    });

            IAsyncLoadablePackageInitialize packageInit = _objectUnderTest;
            // This runs the AsyncPackage.InitializeAsync() method.
            await packageInit.Initialize(asyncServiceProviderMock.Object, null, null);
        }
    }
}
