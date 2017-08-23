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
using GoogleCloudExtension;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace GoogleCloudExtensionUnitTests
{
    /// <summary>
    /// Tests for the GoogleCloudExtensionPackage class.
    /// </summary>
    [TestClass]
    [DeploymentItem(VsixManifestFileName)]
    public class GoogleCloudExtensionPackageTests
    {
        private const string ExpectedAssemblyName = "google-cloud-visualstudio";
        private const string VsixManifestFileName = "source.extension.vsixmanifest";

        /// <summary>
        /// IID of <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms680509">IUnknown</see>.
        /// </summary>
        private static Guid s_iidIUnknown = new Guid("00000000-0000-0000-C000-000000000046");

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public TestContext TestContext { get; set; }

        private Mock<DTE> _dteMock;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _dteMock = new Mock<DTE>();
        }

        [TestMethod]
        public void TestPackageValues()
        {
            const string mockedVersion = "MockVsVersion";
            const string mockedEdition = "MockedEdition";
            _dteMock.Setup(dte => dte.Version).Returns(mockedVersion);
            _dteMock.Setup(dte => dte.Edition).Returns(mockedEdition);
            string expectedAssemblyVersion = GetVsixManifestVersion();

            InitGlobalServiceProvider(_dteMock);

            Assert.AreEqual(mockedVersion, GoogleCloudExtensionPackage.VsVersion);
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

        private string GetVsixManifestVersion()
        {
            XDocument vsixManifest = XDocument.Load(Path.Combine(TestContext.TestDeploymentDir, VsixManifestFileName));
            XNamespace ns = vsixManifest.Root?.Name.Namespace ?? XNamespace.None;
            XElement manifestRoot = vsixManifest.Element(ns.GetName("PackageManifest"));
            XElement metadata = manifestRoot?.Element(ns.GetName("Metadata"));
            XElement identity = metadata?.Element(ns.GetName("Identity"));
            return identity?.Attribute("Version")?.Value;
        }

        public static void InitGlobalServiceProvider(Mock<DTE> dteMock)
        {
            Mock<IServiceProvider> serviceProviderMock = dteMock.As<IServiceProvider>();
            var activityLogMock = new Mock<IVsActivityLog>();
            activityLogMock.Setup(al => al.LogEntry(It.IsAny<uint>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(VSConstants.S_OK);
            SetupService<DTE, DTE>(serviceProviderMock, dteMock);
            SetupService<SVsActivityLog, IVsActivityLog>(serviceProviderMock, activityLogMock);

            // Remove the old GlobalProvider if it exists.
            ServiceProvider.GlobalProvider?.Dispose();
            // This sets the ServiceProvider.GlobalProvider
            // and causes it to use the mocked IServiceProvider.
            ServiceProvider.CreateFromSetSite(serviceProviderMock.Object);
            // This runs the Initialize() method.
            ((IVsPackage)new GoogleCloudExtensionPackage()).SetSite(serviceProviderMock.Object);
        }

        internal static void SetupService<ServiceType, InterfaceType>(
            Mock<IServiceProvider> serviceProviderMock,
            IMock<InterfaceType> mockObj) where InterfaceType : class
        {
            Guid serviceGuid = typeof(ServiceType).GUID;
            // ReSharper disable once RedundantAssignment
            IntPtr interfacePtr = Marshal.GetIUnknownForObject(mockObj.Object);
            serviceProviderMock
                .Setup(x => x.QueryService(ref serviceGuid, ref s_iidIUnknown, out interfacePtr))
                .Returns(VSConstants.S_OK);
        }
    }
}
