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
using stdole;
using System;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace GoogleCloudExtensionUnitTests
{
    /// <summary>
    /// Tests for <see cref="GoogleCloudExtensionPackage"/> class.
    /// </summary>
    [TestClass]
    [DeploymentItem(VsixManifestFileName)]
    public class GoogleCloudExtensionPackageTests
    {
        private const string ExpectedAssemblyName = "google-cloud-visualstudio";
        private const string VsixManifestFileName = "source.extension.vsixmanifest";

        [TestMethod]
        public void TestPackageValues()
        {
            const string mockedVersion = "MockVsVersion";
            const string mockedEdition = "MockedEdition";
            InitPackageMock(
                dteMock =>
                {
                    dteMock.Setup(dte => dte.Version).Returns(mockedVersion);
                    dteMock.Setup(dte => dte.Edition).Returns(mockedEdition);
                });
            string expectedAssemblyVersion = GetVsixManifestVersion();

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

        private static string GetVsixManifestVersion()
        {
            XDocument vsixManifest = XDocument.Load(VsixManifestFileName);
            XNamespace ns = vsixManifest.Root?.Name.Namespace ?? XNamespace.None;
            XElement manifestRoot = vsixManifest.Element(ns.GetName("PackageManifest"));
            XElement metadata = manifestRoot?.Element(ns.GetName("Metadata"));
            XElement identity = metadata?.Element(ns.GetName("Identity"));
            return identity?.Attribute("Version")?.Value;
        }

        public static void InitPackageMock(Action<Mock<DTE>> dteSetupAction)
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            var dteMock = new Mock<DTE>();
            var activityLogMock = new Mock<IVsActivityLog>();
            activityLogMock.Setup(al => al.LogEntry(It.IsAny<uint>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(VSConstants.S_OK);
            dteSetupAction(dteMock);
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

        private static void SetupService<ServiceType, InterfaceType>(
            Mock<IServiceProvider> serviceProviderMock,
            IMock<InterfaceType> mockObj) where InterfaceType : class
        {
            Guid serviceGuid = typeof(ServiceType).GUID;
            Guid iUnknownGuid = typeof(IUnknown).GUID;
            // ReSharper disable once RedundantAssignment
            IntPtr interfacePtr = Marshal.GetIUnknownForObject(mockObj.Object);
            serviceProviderMock
                .Setup(x => x.QueryService(ref serviceGuid, ref iUnknownGuid, out interfacePtr))
                .Returns(0);
        }
    }
}
