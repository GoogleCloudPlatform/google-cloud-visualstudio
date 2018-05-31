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

using GoogleCloudExtension.Services.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace GoogleCloudExtensionUnitTests.Services.FileSystem
{
    [TestClass]
    [DeploymentItem(TestResourcesPath, TestResourcesPath)]
    public class IoFileServiceTests
    {
        private const string TestXmlFileName = @"TestXmlFile.xml";
        private const string TestResourcesPath = @"Services\FileSystem\Resources";
        private IoFileService _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new IoFileService();
        }

        [TestMethod]
        [DataRow(TestResourcesPath + @"\" + TestXmlFileName)]
        [DataRow(TestResourcesPath + @"\NonExistantFile.xml")]
        public void TestExists_MatchesIOFileExists(string testFilePath)
        {
            Assert.AreEqual(File.Exists(testFilePath), _objectUnderTest.Exists(testFilePath));
        }
    }
}
