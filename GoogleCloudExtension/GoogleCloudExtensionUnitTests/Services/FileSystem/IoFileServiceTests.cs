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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.Services.FileSystem
{
    [TestClass]
    [DeploymentItem(TestResourcesPath, TestResourcesPath)]
    public class IoFileServiceTests
    {
        private const string TestXmlFilePath = @"Services\FileSystem\Resources\TestXmlFile.xml";
        private const string TestResourcesPath = @"Services\FileSystem\Resources";
        private const string TargetFilePath = @"Services\FileSystem\Resources\TargetFile.txt";
        private IoFileService _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new IoFileService();
        }

        [TestCleanup]
        public void AfterEach()
        {
            if (File.Exists(TargetFilePath))
            {
                File.Delete(TargetFilePath);
            }
        }

        [TestMethod]
        public void TestExists_True()
        {
            Assert.IsTrue(_objectUnderTest.Exists(TestXmlFilePath));
        }

        [TestMethod]
        public void TestExists_False()
        {
            Assert.IsFalse(_objectUnderTest.Exists(@"Services\FileSystem\Resources\NonExistantFile.xml"));
        }

        [TestMethod]
        public void TestWriteAllText()
        {
            const string fileContents = "This is the contents written to the file!";

            _objectUnderTest.WriteAllText(TargetFilePath, fileContents);

            Assert.AreEqual(File.ReadAllText(TargetFilePath), fileContents);
        }

        [TestMethod]
        public void TestReadLines()
        {
            string[] expectedLines = { "Line 1!", "Line2?" };
            File.WriteAllLines(TargetFilePath, expectedLines);

            IEnumerable<string> result = _objectUnderTest.ReadLines(TargetFilePath);

            CollectionAssert.AreEqual(expectedLines, result.ToList());
        }

        [TestMethod]
        public void TestCopy()
        {
            _objectUnderTest.Copy(TestXmlFilePath, TargetFilePath, true);

            Assert.IsTrue(File.Exists(TargetFilePath));
            Assert.AreEqual(File.ReadAllText(TestXmlFilePath), File.ReadAllText(TargetFilePath));

        }
    }
}
