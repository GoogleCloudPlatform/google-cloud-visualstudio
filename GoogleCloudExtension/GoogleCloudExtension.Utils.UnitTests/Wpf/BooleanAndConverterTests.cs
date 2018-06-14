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

using GoogleCloudExtension.Utils.Wpf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Windows;

namespace GoogleCloudExtension.Utils.UnitTests.Wpf
{
    [TestClass]
    public class BooleanAndConverterTests
    {
        private BooleanAndConverter _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new BooleanAndConverter();
        }

        [TestMethod]
        [DataRow(new object[] { true, true }, true)]
        [DataRow(new object[] { false, true }, false)]
        [DataRow(new object[] { "true", "true" }, true)]
        [DataRow(new object[] { "false", "true" }, false)]
        public void TestConvert_ArrayOfBools(object[] values, bool expectedResult)
        {
            object result = _objectUnderTest.Convert(
                values, typeof(bool), null, CultureInfo.InvariantCulture);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        [DataRow(new object[] { true, true }, true)]
        [DataRow(new object[] { false, true }, false)]
        [DataRow(new object[] { "true", "true" }, true)]
        [DataRow(new object[] { "false", "true" }, false)]
        public void TestConvert_ArrayOfBoolsToString(object[] values, bool expectedResult)
        {
            object result = _objectUnderTest.Convert(
                values, typeof(string), null, CultureInfo.InvariantCulture);

            Assert.AreEqual(expectedResult.ToString(CultureInfo.InvariantCulture), result);
        }

        [TestMethod]
        public void TestConvert_UnconvertableValueUnsetsValue()
        {
            object result = _objectUnderTest.Convert(
                new[] { new object() },
                typeof(bool),
                null,
                CultureInfo.InvariantCulture);
            Assert.AreEqual(DependencyProperty.UnsetValue, result);
        }

        [TestMethod]
        public void TestConvertBack_ThrowsNotSupported()
        {
            Assert.ThrowsException<NotSupportedException>(() => _objectUnderTest.ConvertBack(null, null, null, null));
        }
    }
}
