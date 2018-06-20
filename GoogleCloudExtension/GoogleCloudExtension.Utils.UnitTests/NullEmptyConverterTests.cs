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
using System.Globalization;

namespace GoogleCloudExtension.Utils.UnitTests
{
    /// <summary>
    /// Test class for the <see cref="NullEmptyConverter{T}"/>.
    /// </summary>
    [TestClass]
    public class NullEmptyConverterTests
    {
        private const string NotEmptyString = "not an empty string";
        private NullEmptyConverter<object> _objectUnderTest;
        private static readonly object s_emptyResult = TestNullEmptyConverter.EmptyResult;
        private static readonly object s_notEmptyResult = TestNullEmptyConverter.NotEmptyResult;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _objectUnderTest = new TestNullEmptyConverter();
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private static object[][] EmptyStringData { get; } =
        {
            new object[] {null},
            new object[] {""},
            new object[] {" \t\n\f\r"}
        };

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private static object[][] NotEmptyData { get; } =
        {
            new object[] {NotEmptyString},
            new[] {new object()}
        };

        [TestMethod]
        [DynamicData(nameof(EmptyStringData))]
        public void TestConvert_EmptyStringInput(string emptyString)
        {
            object val = _objectUnderTest.Convert(emptyString, null, null, null);

            Assert.AreEqual(s_emptyResult, val);
        }

        [TestMethod]
        public void TestConvert_ConvertableConvertsUsingGivenCultureInfo()
        {
            var convertableMock = new Mock<IConvertible>();
            CultureInfo mockedCultureInfo = new Mock<CultureInfo>("").Object;

            _objectUnderTest.Convert(convertableMock.Object, null, null, mockedCultureInfo);

            convertableMock.Verify(c => c.ToString(mockedCultureInfo));
        }

        [TestMethod]
        [DynamicData(nameof(EmptyStringData))]
        public void TestConvert_ConvertableToEmptyStringInput(string emptyString)
        {
            var convertible = Mock.Of<IConvertible>(c => c.ToString(It.IsAny<IFormatProvider>()) == emptyString);

            object val = _objectUnderTest.Convert(convertible, null, null, null);

            Assert.AreEqual(s_emptyResult, val);
        }

        [TestMethod]
        [DynamicData(nameof(NotEmptyData))]
        public void TestConvert_NotNullObjectInput(object notEmpty)
        {
            object val = _objectUnderTest.Convert(notEmpty, null, null, null);

            Assert.AreEqual(s_notEmptyResult, val);
        }

        [TestMethod]
        public void TestConvert_ConvertableToNotEmptyInput()
        {
            var convertible = Mock.Of<IConvertible>(c => c.ToString(It.IsAny<IFormatProvider>()) == NotEmptyString);

            object val = _objectUnderTest.Convert(convertible, null, null, null);

            Assert.AreEqual(s_notEmptyResult, val);
        }

        [TestMethod]
        [DynamicData(nameof(EmptyStringData))]
        public void TestConvert_InvertEmptyInput(string emptyString)
        {
            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert(null, null, null, null);

            Assert.AreEqual(s_notEmptyResult, val);
        }

        [TestMethod]
        [DynamicData(nameof(EmptyStringData))]
        public void TestConvert_InvertConvertableToEmptyStringInput(string emptyString)
        {
            var convertible = Mock.Of<IConvertible>(c => c.ToString(It.IsAny<IFormatProvider>()) == emptyString);

            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert(convertible, null, null, null);

            Assert.AreEqual(s_notEmptyResult, val);
        }

        [TestMethod]
        [DynamicData(nameof(NotEmptyData))]
        public void TestConvert_InvertNotEmptyInput(object notEmpty)
        {
            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert(notEmpty, null, null, null);

            Assert.AreEqual(s_emptyResult, val);
        }

        [TestMethod]
        public void TestConvert_InvertConvertableToNotEmptyInput()
        {
            var convertible =
                Mock.Of<IConvertible>(c => c.ToString(It.IsAny<IFormatProvider>()) == NotEmptyString);

            object val = _objectUnderTest.Convert(convertible, null, null, null);

            Assert.AreEqual(s_notEmptyResult, val);
        }

        [TestMethod]
        public void TestConvertBack_ThrowsNotSupportedException()
        {
            Assert.ThrowsException<NotSupportedException>(() => _objectUnderTest.ConvertBack(null, null, null, null));
        }

        [TestMethod]
        public void TestProvideValue_ReturnsSelf()
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
