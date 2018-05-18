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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using static GoogleCloudExtension.Utils.StringUtils;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class StringUtilTests
    {
        [TestMethod]
        public void IsDigitsOnlyTests()
        {
            Assert.IsFalse(IsDigitsOnly(null), "null input");
            Assert.IsFalse(IsDigitsOnly("  "), "Space input");

            Assert.IsTrue(IsDigitsOnly(""), "Empty input");
            Assert.IsTrue(IsDigitsOnly("124"), "124 input");
            Assert.IsTrue(IsDigitsOnly("00"), "00 input");

            Assert.IsFalse(IsDigitsOnly("yiwe"), "yiwe input");
            Assert.IsFalse(IsDigitsOnly("12a"), "12a input");
            Assert.IsFalse(IsDigitsOnly("@"), "@ input");
            Assert.IsFalse(IsDigitsOnly("!--34654&"), "!--34654& input");
            Assert.IsFalse(IsDigitsOnly(" k  "), " k   input");
        }

        [TestMethod]
        public void FirstNonSpaceIndexTests()
        {
            Assert.AreEqual(1, FirstNonSpaceIndex(" abc   "));
            Assert.AreEqual(0, FirstNonSpaceIndex("abc   "));
            Assert.AreEqual(-1, FirstNonSpaceIndex("     "));
            Assert.AreEqual(-1, FirstNonSpaceIndex(""));
            Assert.AreEqual(-1, FirstNonSpaceIndex(null));
            Assert.AreEqual(3, FirstNonSpaceIndex("   uu  pp  "));
        }

        [TestMethod]
        public void LastNonSpaceIndexTests()
        {
            Assert.AreEqual(2, LastNonSpaceIndex("abc   "));
            Assert.AreEqual(4, LastNonSpaceIndex("  abc   "));
            Assert.AreEqual(-1, LastNonSpaceIndex("     "));
            Assert.AreEqual(-1, LastNonSpaceIndex(""));
            Assert.AreEqual(-1, LastNonSpaceIndex(null));
            Assert.AreEqual(8, LastNonSpaceIndex("   uu  pp  "));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("word")]
        [DataRow("already-kebob-case")]
        [DataRow("%$#(*&)(@#$")]
        public void TestToKebobCase_Unchanged(string unchangingArgument)
        {
            string result = ToKebobCase(unchangingArgument);

            Assert.AreEqual(unchangingArgument, result);
        }

        [TestMethod]
        [DataRow("ALLUPPERCASE", "alluppercase")]
        [DataRow("UpperCamelCase", "upper-camel-case")]
        [DataRow("lowerCamelCase", "lower-camel-case")]
        [DataRow("Upper-Kebob-Case", "upper-kebob-case")]
        [DataRow("20number2", "20-number-2")]
        [DataRow("UpperCamelCaseWith2Numbers100", "upper-camel-case-with-2-numbers-100")]
        [DataRow("CamelCase&Symbols!", "camel-case&symbols!")]
        public void TestToKebobCase_Changes(string argument, string expectedResult)
        {
            string result = ToKebobCase(argument);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void TestToKebobCase_ExtremelyLongValueChanges()
        {
            string longNumberString = string.Join("", Enumerable.Range(1, 200));
            string argument = "CamelCaseHead" + longNumberString + "CamelCaseTail";
            string expected = "camel-case-head-" + longNumberString + "-camel-case-tail";

            string result = ToKebobCase(argument);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestToKebobCase_ExtremelyLongValueUnchanged()
        {
            string longNumberString = string.Join("", Enumerable.Range(1, 200));

            string result = ToKebobCase(longNumberString);

            Assert.AreEqual(longNumberString, result);
        }
    }
}
