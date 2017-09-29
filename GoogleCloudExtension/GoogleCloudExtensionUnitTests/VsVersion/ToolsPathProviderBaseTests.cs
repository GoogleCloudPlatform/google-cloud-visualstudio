// Copyright 2017 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GoogleCloudExtensionUnitTests.VsVersion
{
    [TestClass]
    public class ToolsPathProviderBaseTests
    {
        private const string SdkVersion = "2.0.0";
        private ToolsPathProviderBase _objectUnderTest;
        private string _dotnetPath;
        private string _sdkPath;

        [TestInitialize]

        public void BeforeEach()
        {
            _dotnetPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            _sdkPath = Path.Combine(_dotnetPath, ToolsPathProviderBase.SdkDirectoryName);
            _objectUnderTest =
                Mock.Of<ToolsPathProviderBase>(p => p.GetDotnetPath() == Path.Combine(_dotnetPath, "dotnet.exe"));
        }

        [TestCleanup]
        public void AfterEach()
        {
            if (Directory.Exists(_dotnetPath))
            {
                Directory.Delete(_dotnetPath, true);
            }
        }

        [TestMethod]
        public void TestGetNetCoreSdkVersionsDotnetAsRoot()
        {
            _objectUnderTest = Mock.Of<ToolsPathProviderBase>(p => p.GetDotnetPath() == "c:\\");

            IEnumerable<string> versions = _objectUnderTest.GetNetCoreSdkVersions();

            Assert.AreEqual(0, versions.Count());
        }


        [TestMethod]
        public void TestGetNetCoreSdkVersionsNoDotnetDirectory()
        {
            IEnumerable<string> versions = _objectUnderTest.GetNetCoreSdkVersions();

            Assert.AreEqual(0, versions.Count());
        }

        [TestMethod]
        public void TestGetNetCoreSdkVersionsNoSdkDirectory()
        {
            Directory.CreateDirectory(_dotnetPath);

            IEnumerable<string> versions = _objectUnderTest.GetNetCoreSdkVersions();

            Assert.AreEqual(0, versions.Count());
        }

        [TestMethod]
        public void TestGetNetCoreSdkVersionsEmptyDirectory()
        {
            Directory.CreateDirectory(_dotnetPath);
            Directory.CreateDirectory(_sdkPath);

            IEnumerable<string> versions = _objectUnderTest.GetNetCoreSdkVersions();

            Assert.AreEqual(0, versions.Count());
        }

        [TestMethod]
        public void TestGetNetCoreSdkVersionsSingleSdkDirectory()
        {
            Directory.CreateDirectory(_dotnetPath);
            Directory.CreateDirectory(_sdkPath);
            Directory.CreateDirectory(Path.Combine(_sdkPath, SdkVersion));
            Thread.Sleep(100);

            IEnumerable<string> versions = _objectUnderTest.GetNetCoreSdkVersions();

            Assert.AreEqual(SdkVersion, versions.Single());
        }

        [TestMethod]
        public void TestGetNetCoreSdkVersionsWithNugetFallbackDirectory()
        {
            Directory.CreateDirectory(_dotnetPath);
            Directory.CreateDirectory(_sdkPath);
            Directory.CreateDirectory(Path.Combine(_sdkPath, SdkVersion));
            Directory.CreateDirectory(Path.Combine(_sdkPath, ToolsPathProviderBase.NugetFallbackFolderName));
            Thread.Sleep(100);

            IEnumerable<string> versions = _objectUnderTest.GetNetCoreSdkVersions();

            Assert.AreEqual(SdkVersion, versions.Single());
        }
    }
}
