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
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GoogleCloudExtensionUnitTests.VsVersion
{
    [TestClass]
    public class VsVersionUtilsTests : ExtensionTestBase
    {

        [TestMethod]
        public void TestGetToolsPathProvider_GetsVs14Provider()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2015Version);

            IToolsPathProvider result = VsVersionUtils.GetToolsPathProvider();

            Assert.IsInstanceOfType(result, typeof(GoogleCloudExtension.VsVersion.VS14.ToolsPathProvider));
        }

        [TestMethod]
        public void TestGetToolsPathProvider_GetsVs15Provider()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);

            IToolsPathProvider result = VsVersionUtils.GetToolsPathProvider();

            Assert.IsInstanceOfType(result, typeof(GoogleCloudExtension.VsVersion.VS15.ToolsPathProvider));
        }

        [TestMethod]
        public void TestGetToolsPathProvider_GetsVs16Provider()
        {
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2019Version);

            IToolsPathProvider result = VsVersionUtils.GetToolsPathProvider();

            Assert.IsInstanceOfType(result, typeof(GoogleCloudExtension.VsVersion.VS16.ToolsPathProvider));
        }

        [TestMethod]
        public void TestGetToolsPathProvider_ThrowsNotSupportedException()
        {
            const string expectedUnknownVersion = "ExpectedUnknownVersion";
            PackageMock.Setup(p => p.VsVersion).Returns(expectedUnknownVersion);

            var e = Assert.ThrowsException<NotSupportedException>(VsVersionUtils.GetToolsPathProvider);

            StringAssert.Contains(e.Message, expectedUnknownVersion);
        }
    }
}
