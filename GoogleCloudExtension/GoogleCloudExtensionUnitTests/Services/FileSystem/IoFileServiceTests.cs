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
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.Services.FileSystem
{
    [TestClass]
    [DeploymentItem(TestResourcesPath, TestResourcesPath)]
    public class IOFileServiceTests
    {
        private const string TestXmlFilePath = @"Services\FileSystem\Resources\TestXmlFile.xml";
        private const string TestResourcesPath = @"Services\FileSystem\Resources";
        private const string TargetFilePath = @"Services\FileSystem\Resources\TargetFile.txt";
        private IOFileService _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new IOFileService();
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

        [TestMethod]
        public async Task TestOpenText()
        {
            string expectedResult;
            string result;
            using (TextReader reader = _objectUnderTest.OpenText(TestXmlFilePath))
            {
                Task<string> resultTask = reader.ReadToEndAsync();
                using (TextReader fileReader = File.OpenText(TestXmlFilePath))
                {
                    expectedResult = await fileReader.ReadToEndAsync();
                }

                result = await resultTask;
            }

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestCreateText()
        {
            const string fileContents = "File Contents";
            using (TextWriter writer = _objectUnderTest.CreateText(TargetFilePath))
            {
                await writer.WriteAsync(fileContents);
            }

            Assert.IsTrue(File.Exists(TargetFilePath));
            Assert.AreEqual(fileContents, File.ReadAllText(TargetFilePath));
        }

        [TestMethod]
        public void TestDelete()
        {
            File.Create(TargetFilePath);
            _objectUnderTest.Delete(TargetFilePath);
            Assert.IsFalse(File.Exists(TargetFilePath));
        }
    }
}
