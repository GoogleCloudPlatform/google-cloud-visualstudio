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

using EnvDTE;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.VsVersion
{
    [TestClass]
    public class ToolsPathProviderBaseTests : ExtensionTestBase
    {
        private const string SdkVersion = "2.0.0";
        private const string DefaultProgramFilesPath = @"C:\Default Program Files";
        private const string DefaultDotnetSdkFolderPath = DefaultProgramFilesPath + @"\dotnet\sdk";
        private const string DevenvPathFromRoot = @"Common7\IDE\devenv.exe";
        private const string DefaultDevenvPath = @"c:\Default\" + DevenvPathFromRoot;
        private ToolsPathProviderBase _objectUnderTest;
        private Mock<IFileSystem> _fileSystemMock;
        private Mock<IEnvironment> _environmentMock;
        private Mock<DTE> _dteMock;

        protected override void BeforeEach()
        {
            _fileSystemMock = new Mock<IFileSystem> { DefaultValue = DefaultValue.Mock };
            _environmentMock = new Mock<IEnvironment>();
            _dteMock = new Mock<DTE>();

            _dteMock.Setup(dte => dte.FullName).Returns(DefaultDevenvPath);
            _environmentMock.Setup(e => e.ExpandEnvironmentVariables(ToolsPathProviderBase.ProgramW6432))
                .Returns(DefaultProgramFilesPath);

            PackageMock.Setup(p => p.GetMefServiceLazy<IFileSystem>()).Returns(_fileSystemMock.ToLazy());
            PackageMock.Setup(p => p.GetMefServiceLazy<IEnvironment>()).Returns(_environmentMock.ToLazy());
            PackageMock.Setup(p => p.GetService<SDTE, DTE>()).Returns(_dteMock.Object);

            _objectUnderTest = Mock.Of<ToolsPathProviderBase>();
        }


        [TestMethod]
        public void TestGetNetCoreSdkVersions_NoSdkDirectory()
        {
            _fileSystemMock.Setup(fs => fs.Directory.Exists(DefaultDotnetSdkFolderPath)).Returns(false);

            IEnumerable<string> versions = _objectUnderTest.GetNetCoreSdkVersions();

            Assert.AreEqual(0, versions.Count());
        }

        [TestMethod]
        public void TestGetNetCoreSdkVersions_EmptySdkDirectory()
        {
            _fileSystemMock.Setup(fs => fs.Directory.Exists(DefaultDotnetSdkFolderPath)).Returns(true);
            _fileSystemMock.Setup(fs => fs.Directory.EnumerateDirectories(DefaultDotnetSdkFolderPath))
                .Returns(Enumerable.Empty<string>());

            IEnumerable<string> versions = _objectUnderTest.GetNetCoreSdkVersions();

            Assert.AreEqual(0, versions.Count());
        }

        [TestMethod]
        public void TestGetNetCoreSdkVersions_RetrievesSingleSdkVersion()
        {
            _fileSystemMock.Setup(fs => fs.Directory.Exists(DefaultDotnetSdkFolderPath)).Returns(true);
            _fileSystemMock.Setup(fs => fs.Directory.EnumerateDirectories(DefaultDotnetSdkFolderPath))
                .Returns(new[] { SdkVersion });

            IEnumerable<string> versions = _objectUnderTest.GetNetCoreSdkVersions();

            Assert.AreEqual(SdkVersion, versions.Single());
        }

        [TestMethod]
        public void TestGetNetCoreSdkVersions_FiltersOutNugetFallbackDirectory()
        {
            _fileSystemMock.Setup(fs => fs.Directory.Exists(DefaultDotnetSdkFolderPath)).Returns(true);
            _fileSystemMock.Setup(fs => fs.Directory.EnumerateDirectories(DefaultDotnetSdkFolderPath))
                .Returns(new[] { SdkVersion, ToolsPathProviderBase.NugetFallbackFolderName });

            IEnumerable<string> versions = _objectUnderTest.GetNetCoreSdkVersions();

            Assert.AreEqual(SdkVersion, versions.Single());
        }

        [TestMethod]
        public void TestGetDotnetPath_ReadsEnvironmentVariable()
        {
            const string expectedProgramFiles = @"c:\Expected Program Files";
            _environmentMock.Setup(e => e.ExpandEnvironmentVariables(ToolsPathProviderBase.ProgramW6432))
                .Returns(expectedProgramFiles);

            string result = _objectUnderTest.GetDotnetPath();

            StringAssert.StartsWith(result, expectedProgramFiles);
        }

        [TestMethod]
        public void TestGetDotnetPath_AppendsExpectedConstant()
        {
            string result = _objectUnderTest.GetDotnetPath();

            StringAssert.EndsWith(result, ToolsPathProviderBase.DotnetExeSubPath);
        }

        [TestMethod]
        public void TestGetExternalToolsPath_CreatesFromDte()
        {
            const string expectedVsRoot = @"c:\Path\To\Vs";
            const string devenvPath = @"c:\Path\To\Vs\Common7\IDE\devenv.exe";
            _dteMock.Setup(dte => dte.FullName).Returns(devenvPath);

            string result = _objectUnderTest.GetExternalToolsPath();

            StringAssert.StartsWith(result, expectedVsRoot);
        }

        [TestMethod]
        public void TestGetExternalToolsPath_AppendsExpectedConstant()
        {
            string result = _objectUnderTest.GetExternalToolsPath();

            StringAssert.EndsWith(result, @"Web\External");
        }

        [TestMethod]
        public void TestGetRemoteDebuggerToolsPath_CreatesFromDte()
        {
            const string ideFolder = @"c:\Path\To\Vs\Common7\IDE";
            const string devenvPath = @"c:\Path\To\Vs\Common7\IDE\devenv.exe";
            _dteMock.Setup(dte => dte.FullName).Returns(devenvPath);

            string result = _objectUnderTest.GetRemoteDebuggerToolsPath();

            StringAssert.StartsWith(result, ideFolder);
        }

        [TestMethod]
        public void TestGetRemoteDebuggerToolsPath_AppendsExpectedConstant()
        {
            string result = _objectUnderTest.GetRemoteDebuggerToolsPath();

            StringAssert.EndsWith(result, @"Remote Debugger\x64\*");
        }
    }
}
