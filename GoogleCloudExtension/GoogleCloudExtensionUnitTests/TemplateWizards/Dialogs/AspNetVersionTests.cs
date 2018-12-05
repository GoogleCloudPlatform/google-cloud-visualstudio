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

using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.TemplateWizards.Dialogs.TemplateChooserDialog;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.TemplateWizards.Dialogs
{
    [TestClass]
    public class AspNetVersionTests : ExtensionTestBase
    {
        private Mock<IToolsPathProvider> _toolsPathProviderMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _toolsPathProviderMock = new Mock<IToolsPathProvider>();
            VsVersionUtils.s_toolsPathProviderOverride = _toolsPathProviderMock.Object;
        }

        [TestCleanup]
        public void AfterEach() => VsVersionUtils.s_toolsPathProviderOverride = null;

        [TestMethod]
        public void TestGetAvailableAspNetCoreVersions_ForVS2017NetCore10()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);
            _toolsPathProviderMock.Setup(p => p.GetNetCoreSdkVersions()).Returns(new[] { "1.0.35" });

            IList<AspNetVersion> results = AspNetVersion.GetAvailableAspNetCoreVersions(FrameworkType.NetCore);

            CollectionAssert.AreEqual(new[] { AspNetVersion.AspNetCore10 }, results.ToList());
        }

        [TestMethod]
        public void TestGetAvailableAspNetCoreVersions_ForVS2017NetCore11()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);
            _toolsPathProviderMock.Setup(p => p.GetNetCoreSdkVersions()).Returns(
                new[] { "1.0.35", AspNetVersion.s_firstSdkVersionWith11Runtime.ToString() });

            IList<AspNetVersion> results = AspNetVersion.GetAvailableAspNetCoreVersions(FrameworkType.NetCore);

            CollectionAssert.AreEqual(new[] { AspNetVersion.AspNetCore10, AspNetVersion.AspNetCore11 }, results.ToList());
        }

        [TestMethod]
        public void TestGetAvailableAspNetCoreVersions_ForVS2017NetCore20()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);
            _toolsPathProviderMock.Setup(p => p.GetNetCoreSdkVersions()).Returns(new[] { "2.0.35" });

            IList<AspNetVersion> results = AspNetVersion.GetAvailableAspNetCoreVersions(FrameworkType.NetCore);

            CollectionAssert.AreEqual(new[] { AspNetVersion.AspNetCore20 }, results.ToList());
        }

        [TestMethod]
        public void TestGetAvailableAspNetCoreVersions_ForVS2017NetCore21()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);
            _toolsPathProviderMock.Setup(p => p.GetNetCoreSdkVersions())
                .Returns(new[] { AspNetVersion.s_firstSdkVersionWith21Runtime.ToString() });

            IList<AspNetVersion> results = AspNetVersion.GetAvailableAspNetCoreVersions(FrameworkType.NetCore);

            CollectionAssert.AreEqual(new[] { AspNetVersion.AspNetCore20, AspNetVersion.AspNetCore21 }, results.ToList());
        }

        [TestMethod]
        public void TestGetAvailableAspNetCoreVersions_ForVS2017NetCoreAll()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);
            _toolsPathProviderMock.Setup(p => p.GetNetCoreSdkVersions()).Returns(
                new[]
                {
                    AspNetVersion.s_firstSdkVersionWith11Runtime.ToString(),
                    AspNetVersion.s_firstSdkVersionWith21Runtime.ToString()
                });

            IList<AspNetVersion> results = AspNetVersion.GetAvailableAspNetCoreVersions(FrameworkType.NetCore);

            CollectionAssert.AreEqual(
                new[]
                {
                    AspNetVersion.AspNetCore10,
                    AspNetVersion.AspNetCore11,
                    AspNetVersion.AspNetCore20,
                    AspNetVersion.AspNetCore21
                }, results.ToList());
        }
    }
}
