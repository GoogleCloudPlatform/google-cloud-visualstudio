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

using GoogleCloudExtension.Services;
using GoogleCloudExtension.VsVersion.VS14;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.VsVersion.VS14
{
    [TestClass]
    public class ToolsPathProviderTests : ExtensionTestBase
    {
        private const string DefaultX86ProgramFilesPath = @"c:\Default X86 Program Files";
        private Mock<IEnvironment> _environmentMock;
        private ToolsPathProvider _objectUnderTest;

        protected override void BeforeEach()
        {
            _environmentMock = new Mock<IEnvironment>();

            _environmentMock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86))
                .Returns(DefaultX86ProgramFilesPath);

            PackageMock.Setup(p => p.GetMefServiceLazy<IEnvironment>()).Returns(_environmentMock.ToLazy());
            _objectUnderTest = new ToolsPathProvider();
        }

        [TestMethod]
        public void TestGetMsbuildPath_CreatesFromEnvironmentVariable()
        {
            const string expectedX86ProgramFiles = @"c:\Expected X86 Program Files";
            _environmentMock.Setup(e => e.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86))
                .Returns(expectedX86ProgramFiles);

            string result = _objectUnderTest.GetMsbuildPath();

            StringAssert.StartsWith(result, expectedX86ProgramFiles);
        }

        [TestMethod]
        public void TestGetMsbuildPath_AppendsExpectedConstant()
        {
            string result = _objectUnderTest.GetMsbuildPath();

            StringAssert.EndsWith(result, ToolsPathProvider.MSBuildSubPath);
        }
    }
}
