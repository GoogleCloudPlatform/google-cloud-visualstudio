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
using Moq;
using System;

namespace GoogleCloudExtensionUnitTests.Services.FileSystem
{
    [TestClass]
    public class FileSystemServiceTests
    {
        [TestMethod]
        public void TestConstructor_SetsFile()
        {
            var file = Mock.Of<IFile>();

            var objectUnderTest = new FileSystemService(new Lazy<IFile>(() => file), null, null);

            Assert.AreEqual(file, objectUnderTest.File);
        }

        [TestMethod]
        public void TestConstructor_SetsXDocument()
        {
            var expectedXDocument = Mock.Of<IXDocument>();

            var objectUnderTest = new FileSystemService(null, new Lazy<IXDocument>(() => expectedXDocument), null);

            Assert.AreEqual(expectedXDocument, objectUnderTest.XDocument);
        }

        [TestMethod]
        public void TestConstructor_SetsDirectory()
        {
            var expectedDirectory = Mock.Of<IDirectory>();

            var objectUnderTest = new FileSystemService(null, null, new Lazy<IDirectory>(() => expectedDirectory));

            Assert.AreEqual(expectedDirectory, objectUnderTest.Directory);
        }
    }
}
