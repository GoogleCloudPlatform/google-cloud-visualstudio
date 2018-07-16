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

using EnvDTE80;
using GoogleCloudExtension.VsVersion.VS15;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.VsVersion.VS15
{
    [TestClass]
    public class ToolsPathProviderTests : ExtensionTestBase
    {
        private const string DevenvPathFromRoot = @"Common7\IDE\devenv.exe";
        private const string DefaultDevenvPath = @"c:\Default\" + DevenvPathFromRoot;

        private ToolsPathProvider _objectUnderTest;
        private Mock<DTE2> _dteMock;

        protected override void BeforeEach()
        {
            _dteMock = new Mock<DTE2>();

            _dteMock.Setup(dte => dte.FullName).Returns(DefaultDevenvPath);

            PackageMock.Setup(p => p.Dte).Returns(_dteMock.Object);
            _objectUnderTest = new ToolsPathProvider();
        }

        [TestMethod]
        public void TestGetMsbuildPath_CreatesFromEnvironmentVariable()
        {
            const string expectedVsRoot = @"c:\Path\To\Vs";
            const string devenvPath = @"c:\Path\To\Vs\Common7\IDE\devenv.exe";
            _dteMock.Setup(dte => dte.FullName).Returns(devenvPath);

            string result = _objectUnderTest.GetMsbuildPath();

            StringAssert.StartsWith(result, expectedVsRoot);
        }

        [TestMethod]
        public void TestGetMsbuildPath_AppendsExpectedConstant()
        {
            string result = _objectUnderTest.GetMsbuildPath();

            StringAssert.EndsWith(result, ToolsPathProvider.MSBuildSubPath);
        }
    }

}
