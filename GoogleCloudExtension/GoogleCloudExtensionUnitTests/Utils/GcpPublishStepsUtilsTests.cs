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

using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class GcpPublishStepsUtilsTests
    {
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("already-kebob-case")]
        [DataRow("end-with-dash")]
        public void TestToValid_NameUnchanged(string unchangingName)
        {
            string result = GcpPublishStepsUtils.ToValidName(unchangingName);

            Assert.AreEqual(unchangingName, result);
        }

        [TestMethod]
        [DataRow("UpperCamelCase", "upper-camel-case")]
        [DataRow("many.(*)symbols!@$", "many----symbols---")]
        [DataRow("-start-with-dash", "start-with-dash")]
        [DataRow("@start-with-symbol", "start-with-symbol")]
        public void TestToValid_NameChange(string invalidName, string expectedResult)
        {
            string result = GcpPublishStepsUtils.ToValidName(invalidName);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestToValid_NameTruncatesAt100Characters()
        {
            string longString = string.Join("", Enumerable.Range(1, 200));

            string result = GcpPublishStepsUtils.ToValidName(longString);

            Assert.IsTrue(result.Length == 100);
        }
    }
}
