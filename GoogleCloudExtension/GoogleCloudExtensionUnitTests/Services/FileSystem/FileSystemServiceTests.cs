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

using System;
using GoogleCloudExtension.Services.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.Services.FileSystem
{
    [TestClass]
    public class FileSystemServiceTests
    {
        [TestMethod]
        public void TestConstructor_SetsFile()
        {
            IFile file = Mock.Of<IFile>();

            var objectUnderTest = new FileSystemService(new Lazy<IFile>(() => file), null, null, null);

            Assert.AreEqual(file, objectUnderTest.File);
        }

        [TestMethod]
        public void TestConstructor_SetsXDocument()
        {
            IXDocument expectedXDocument = Mock.Of<IXDocument>();

            var objectUnderTest = new FileSystemService(
                null,
                new Lazy<IXDocument>(() => expectedXDocument),
                null,
                null);

            Assert.AreEqual(expectedXDocument, objectUnderTest.XDocument);
        }

        [TestMethod]
        public void TestConstructor_SetsDirectory()
        {
            IDirectory expectedDirectory = Mock.Of<IDirectory>();

            var objectUnderTest = new FileSystemService(
                null,
                null,
                new Lazy<IDirectory>(() => expectedDirectory),
                null);

            Assert.AreEqual(expectedDirectory, objectUnderTest.Directory);
        }

        [TestMethod]
        public void TestConstructor_SetsPath()
        {

            IPath expectedPath = Mock.Of<IPath>();

            var objectUnderTest = new FileSystemService(
                null,
                null,
                null,
                new Lazy<IPath>(() => expectedPath));

            Assert.AreEqual(expectedPath, objectUnderTest.Path);
        }
    }
}
