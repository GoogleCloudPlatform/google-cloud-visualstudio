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
using System;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class GcpPublishStepsUtilsTests
    {
        [TestCleanup]
        public void AfterEach()
        {
            GcpPublishStepsUtils.NowOverride = null;
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("already-kebob-case")]
        [DataRow("end-with-dash-")]
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

        [TestMethod]
        public void TestIncrementVersion_ReplacesDefaultVersion()
        {
            GcpPublishStepsUtils.NowOverride = DateTime.Parse("2011-11-11 01:01:01");
            string initalVersion = GcpPublishStepsUtils.GetDefaultVersion();
            GcpPublishStepsUtils.NowOverride = DateTime.Parse("2012-12-12 02:02:02");

            string result = GcpPublishStepsUtils.IncrementVersion(initalVersion);

            Assert.AreEqual(GcpPublishStepsUtils.GetDefaultVersion(), result);
        }

        [TestMethod]
        public void TestIncrementVersion_IncrementsSingleIntegerVersion()
        {
            const string initalVersion = "My1Version";

            string result = GcpPublishStepsUtils.IncrementVersion(initalVersion);

            Assert.AreEqual("My2Version", result);
        }

        [TestMethod]
        public void TestIncrementVersion_IncrementsTrailingIntegerVersion()
        {
            const string initalVersion = "My1Version3";

            string result = GcpPublishStepsUtils.IncrementVersion(initalVersion);

            Assert.AreEqual("My1Version4", result);
        }

        [TestMethod]
        public void TestIncrementVersion_AppendsTrailingInteger()
        {
            const string initalVersion = "My1Version2BeUpdated";

            string result = GcpPublishStepsUtils.IncrementVersion(initalVersion);

            Assert.AreEqual("My1Version2BeUpdated2", result);
        }

        [TestMethod]
        public void TestIncrementVersion_HandlesNull()
        {
            GcpPublishStepsUtils.NowOverride = DateTime.Parse("2023-10-10 10:10:10");

            string result = GcpPublishStepsUtils.IncrementVersion(null);

            Assert.AreEqual(GcpPublishStepsUtils.GetDefaultVersion(), result);
        }

        [TestMethod]
        public void TestIsDefaultVersion_True()
        {
            GcpPublishStepsUtils.NowOverride = DateTime.Parse("2010-10-10 10:10:10");

            Assert.IsTrue(GcpPublishStepsUtils.IsDefaultVersion(GcpPublishStepsUtils.GetDefaultVersion()));
            Assert.IsTrue(GcpPublishStepsUtils.IsDefaultVersion("12345678t123456"));
        }

        [TestMethod]
        [DataRow("5")]
        [DataRow("MyVersion23")]
        [DataRow("   ")]
        [DataRow("")]
        [DataRow(null)]
        public void TestIsDefaultVersion_False(string version)
        {
            Assert.IsFalse(GcpPublishStepsUtils.IsDefaultVersion(version));
        }
    }
}
