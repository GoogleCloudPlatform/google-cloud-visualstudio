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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace GoogleCloudExtension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [TestClass]
    public class GoogleCloudExtensionPackageTests
    {
        private static Guid _iidIUnknown = (Guid)Assembly.GetAssembly(typeof(VSConstants))
            .GetType("Microsoft.VisualStudio.NativeMethods")
            .GetField("IID_IUnknown").GetValue(null);

        [TestMethod]
        public void TestVsVersion()
        {
            string mockedVersion = "MockVsVersion";
            string mockedEdition = "MockedEdition";
            InitPackageMock(
                dteMock =>
                {

                    dteMock.Setup(dte => dte.Version).Returns(mockedVersion);
                    dteMock.Setup(dte => dte.Edition).Returns(mockedEdition);
                });

            Assert.AreEqual(mockedVersion, GoogleCloudExtensionPackage.VsVersion);
            Assert.AreEqual(mockedEdition, GoogleCloudExtensionPackage.VsEdition);
            Assert.AreEqual("google-cloud-visualstudio", GoogleCloudExtensionPackage.ApplicationName);
            Assert.AreEqual(
                GoogleCloudExtensionPackage.AssemblyVersion, GoogleCloudExtensionPackage.ApplicationVersion);
            Assert.AreEqual(
                $"google-cloud-visualstudio/{GoogleCloudExtensionPackage.AssemblyVersion}",
                GoogleCloudExtensionPackage.VersionedApplicationName);
            Assert.AreEqual(
                GoogleCloudExtensionPackage.ApplicationVersion,
                GoogleCloudExtensionPackage.Instance.AnalyticsSettings.InstalledVersion);
            Assert.IsNull(GoogleCloudExtensionPackage.Instance.AnalyticsSettings.ClientId);
            Assert.IsFalse(GoogleCloudExtensionPackage.Instance.AnalyticsSettings.DialogShown);
            Assert.IsFalse(GoogleCloudExtensionPackage.Instance.AnalyticsSettings.OptIn);
        }

        private static void InitPackageMock(Action<Mock<DTE>> dteSetupAction)
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            var dteMock = new Mock<DTE>();
            var activityLogMock = new Mock<IVsActivityLog>();
            activityLogMock.Setup(al => al.LogEntry(It.IsAny<uint>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(VSConstants.S_OK);
            dteSetupAction(dteMock);
            SetupService<DTE, DTE>(serviceProviderMock, dteMock);
            SetupService<SVsActivityLog, IVsActivityLog>(serviceProviderMock, activityLogMock);
            ServiceProvider.CreateFromSetSite(serviceProviderMock.Object);
            ((IVsPackage)new GoogleCloudExtensionPackage()).SetSite(serviceProviderMock.Object);
        }

        private static void SetupService<ServiceType, InterfaceType>(
            Mock<IServiceProvider> serviceProviderMock,
            Mock<InterfaceType> mockObj) where InterfaceType : class
        {
            var serviceGuid = typeof(ServiceType).GUID;
            // ReSharper disable once RedundantAssignment
            IntPtr interfacePtr = Marshal.GetIUnknownForObject(mockObj.Object);
            serviceProviderMock.Setup(x => x.QueryService(ref serviceGuid, ref _iidIUnknown, out interfacePtr)).Returns(0);
        }
    }
}
