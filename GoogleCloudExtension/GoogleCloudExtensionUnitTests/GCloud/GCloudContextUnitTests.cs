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

using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.GCloud;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.GCloud
{
    [TestClass]
    public class GCloudContextUnitTests : ExtensionTestBase
    {
        [TestMethod]
        public void TestConstructor_SetsCredentialsPath()
        {
            const string expectedCredentialsPath = "expected-credentials-path";
            PackageMock.SetupGet(p => p.GetMefService<ICredentialsStore>().CurrentAccountPath)
                .Returns(expectedCredentialsPath);

            var objectUnderTest = new GCloudContext();

            Assert.AreEqual(expectedCredentialsPath, objectUnderTest.CredentialsPath);
        }

        [TestMethod]
        public void TestConstructor_SetsProjectId()
        {
            const string expectedProjectId = "project-id";
            PackageMock.SetupGet(p => p.GetMefService<ICredentialsStore>().CurrentProjectId).Returns(expectedProjectId);

            var objectUnderTest = new GCloudContext();

            Assert.AreEqual(expectedProjectId, objectUnderTest.ProjectId);
        }

        [TestMethod]
        public void TestConstructor_SetsAppName()
        {
            const string expectedAppName = "app-name";
            PackageMock.Setup(p => p.ApplicationName).Returns(expectedAppName);

            var objectUnderTest = new GCloudContext();

            Assert.AreEqual(expectedAppName, objectUnderTest.AppName);
        }

        [TestMethod]
        public void TestConstructor_SetsAppVersion()
        {
            const string expectedAppVersion = "app-version";
            PackageMock.Setup(p => p.ApplicationVersion).Returns(expectedAppVersion);

            var objectUnderTest = new GCloudContext();

            Assert.AreEqual(expectedAppVersion, objectUnderTest.AppVersion);
        }
    }
}
