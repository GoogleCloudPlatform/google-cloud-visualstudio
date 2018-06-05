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
using GoogleCloudExtension.Options;
using GoogleCloudExtension.Services.FileSystem;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
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
            Assert.AreEqual(ExpectedAssemblyName, GoogleCloudExtensionPackage.ApplicationName);
            Assert.AreEqual(expectedAssemblyVersion, GoogleCloudExtensionPackage.ApplicationVersion);
            Assert.AreEqual(
                $"{ExpectedAssemblyName}/{expectedAssemblyVersion}",
                GoogleCloudExtensionPackage.VersionedApplicationName);
            Assert.AreEqual(
                GoogleCloudExtensionPackage.ApplicationVersion,
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
                GoogleCloudExtensionPackage.ApplicationVersion,
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
                GoogleCloudExtensionPackage.ApplicationVersion,
                GoogleCloudExtensionPackage.Instance.AnalyticsSettings.InstalledVersion);
            _reporterMock.Verify(
                r => r.ReportEvent(
                    It.IsAny<string>(), It.IsAny<string>(), NewInstallEvent.NewInstallEventName, It.IsAny<bool>(),
                    It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()));
        }

        [TestMethod]
        public void TestSamePackageVersion()
        {
            _objectUnderTest.AnalyticsSettings.InstalledVersion = GoogleCloudExtensionPackage.ApplicationVersion;

            RunPackageInitalize();

            Assert.AreEqual(
                GoogleCloudExtensionPackage.ApplicationVersion,
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
        public void TestGetServicesSI_GetsServiceOfTypeIRegisteredByS()
        {
            Mock<IVsSolution> solutionMock = ServiceProviderMock.SetupService<SVsSolution, IVsSolution>();
            RunPackageInitalize();

            IVsSolution service = _objectUnderTest.GetService<SVsSolution, IVsSolution>();

            Assert.AreEqual(solutionMock.Object, service);
        }

        [TestMethod]
        public void TestGetServicesT_GetsService()
        {
            RunPackageInitalize();

            var service = _objectUnderTest.GetService<DTE>();

            Assert.AreEqual(DteMock.Object, service);
        }

        [TestMethod]
        public void TestGetServicesT_GetsServiceFromMef()
        {
            Mock<IComponentModel> serviceMock = ServiceProviderMock.SetupService<SComponentModel, IComponentModel>();
            var mockedFileSystemService = Mock.Of<IFileSystem>();
            serviceMock.Setup(s => s.GetService<IFileSystem>()).Returns(mockedFileSystemService);
            RunPackageInitalize();

            var service = _objectUnderTest.GetService<IFileSystem>();

            Assert.AreEqual(mockedFileSystemService, service);
        }

        [TestMethod]
        public void TestShowOptionPage_OptionPage()
        {
            RunPackageInitalize();

            _objectUnderTest.ShowOptionPage<AnalyticsOptions>();
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
    }
}
