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

using System.Windows;
using Google.Apis.CloudSourceRepositories.v1.Data;
using GoogleCloudExtension.CloudSourceRepositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.CloudSourceRepository
{
    /// <summary>
    /// Unit tests for class <seealso cref="RepoNameConverter"/>
    /// </summary>
    [TestClass]
    public class RepoNameConverterTests
    {
        private RepoNameConverter _testConverter;

        [TestInitialize]
        public void Initialize()
        {
            _testConverter = new RepoNameConverter();
        }

        [TestMethod]
        public void ConvertSucceedsTest()
        {
            string testName = "test-repo";
            var testRepo = new Repo { Name = $"projects/test-project-id/repos/{testName}" };
            var converted = _testConverter.Convert(testRepo, null, null, null);
            Assert.AreEqual(testName, converted);
        }

        [TestMethod]
        public void NullInputTest()
        {
            var converted = _testConverter.Convert(null, null, null, null);
            Assert.AreEqual(DependencyProperty.UnsetValue, converted);
        }
    }
}
