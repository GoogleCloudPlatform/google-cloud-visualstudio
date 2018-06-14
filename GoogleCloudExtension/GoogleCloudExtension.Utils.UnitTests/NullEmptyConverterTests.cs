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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace GoogleCloudExtension.Utils.UnitTests
{
    /// <summary>
    /// Test class for the <see cref="NullEmptyConverter{T}"/>.
    /// </summary>
    [TestClass]
    public class NullEmptyConverterTests
    {
        private const string WhiteSpaceString = " \t\n\f\r";
        private NullEmptyConverter<object> _objectUnderTest;
        private static readonly object s_emptyResult = TestNullEmptyConverter.EmptyResult;
        private static readonly object s_notEmptyResult = TestNullEmptyConverter.NotEmptyResult;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _objectUnderTest = new TestNullEmptyConverter();
        }

        [TestMethod]
        public void TestConvertNull()
        {
            object val = _objectUnderTest.Convert(null, null, null, null);

            Assert.AreEqual(s_emptyResult, val);
        }

        [TestMethod]
        public void TestConvertNotNull()
        {
            object val = _objectUnderTest.Convert(new object(), null, null, null);

            Assert.AreEqual(s_notEmptyResult, val);
        }

        [TestMethod]
        public void TestConvertEmpty()
        {
            object val = _objectUnderTest.Convert("", null, null, null);

            Assert.AreEqual(s_emptyResult, val);
        }

        [TestMethod]
        public void TestConvertWhitespace()
        {
            object val = _objectUnderTest.Convert(WhiteSpaceString, null, null, null);

            Assert.AreEqual(s_emptyResult, val);
        }

        [TestMethod]
        public void TestConvertString()
        {
            object val = _objectUnderTest.Convert("not an empty string", null, null, null);

            Assert.AreEqual(s_notEmptyResult, val);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(WhiteSpaceString)]
        public void TestConvert_ConvertableEmpty(string convertResult)
        {
            var convertible = Mock.Of<IConvertible>(c => c.ToString(It.IsAny<IFormatProvider>()) == convertResult);

            object val = _objectUnderTest.Convert(convertible, null, null, null);

            Assert.AreEqual(s_emptyResult, val);
        }

        [TestMethod]
        public void TestConvert_ConvertableNotEmpty()
        {
            var convertible = Mock.Of<IConvertible>(c => c.ToString(It.IsAny<IFormatProvider>()) == "Not Empty");

            object val = _objectUnderTest.Convert(convertible, null, null, null);

            Assert.AreEqual(s_notEmptyResult, val);
        }

        [TestMethod]
        public void TestConvertNullInvert()
        {
            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert(null, null, null, null);

            Assert.AreEqual(s_notEmptyResult, val);
        }

        [TestMethod]
        public void TestConvertNotNullInvert()
        {
            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert(new object(), null, null, null);

            Assert.AreEqual(s_emptyResult, val);
        }

        [TestMethod]
        public void TestConvertEmptyInvert()
        {
            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert("", null, null, null);

            Assert.AreEqual(s_notEmptyResult, val);
        }

        [TestMethod]
        public void TestConvertWhitespaceInvert()
        {
            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert(WhiteSpaceString, null, null, null);

            Assert.AreEqual(s_notEmptyResult, val);
        }

        [TestMethod]
        public void TestConvertStringInvert()
        {
            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert("not an empty string", null, null, null);

            Assert.AreEqual(s_emptyResult, val);
        }

        [TestMethod]
        public void TestConvertBack()
        {
            Assert.ThrowsException<NotSupportedException>(() => _objectUnderTest.ConvertBack(null, null, null, null));
        }

        [TestMethod]
        public void TestProvideValue()
        {
            object val = _objectUnderTest.ProvideValue(Mock.Of<IServiceProvider>());

            Assert.AreEqual(_objectUnderTest, val);
        }

        private class TestNullEmptyConverter : NullEmptyConverter<object>
        {
            public static readonly object NotEmptyResult = new object();
            public static readonly object EmptyResult = new object();
            protected override object NotEmptyValue { get; } = NotEmptyResult;
            protected override object EmptyValue { get; } = EmptyResult;
        }
    }

}
