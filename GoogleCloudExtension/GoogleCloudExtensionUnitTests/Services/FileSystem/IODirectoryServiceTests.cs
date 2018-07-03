﻿// Copyright 2018 Google Inc. All Rights Reserved.
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
using System.Linq;

namespace GoogleCloudExtensionUnitTests.Services.FileSystem
{
    [TestClass]
    [DeploymentItem(TestResourcesPath, TestResourcesPath)]
    public class IODirectoryServiceTests
    {
        private const string TestResourcesPath = @"Services\FileSystem\Resources";
        private IODirectoryService _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new IODirectoryService();
        }

        [TestMethod]
        public void TestExists_True()
        {
            Assert.IsTrue(_objectUnderTest.Exists(TestResourcesPath));
        }

        [TestMethod]
        public void TestExists_False()
        {
            Assert.IsFalse(_objectUnderTest.Exists(@"Services\FileSystem\Resources\NonExistantDirectory"));
        }

        [TestMethod]
        public void TestEnumerateDirectories()
        {
            IEnumerable<string> results = _objectUnderTest.EnumerateDirectories(@"Services\FileSystem");

            CollectionAssert.AreEqual(new[] { TestResourcesPath }, results.ToList());
        }
    }
}
