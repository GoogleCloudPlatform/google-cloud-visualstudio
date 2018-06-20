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
using Moq;
using System;
using System.Windows;

namespace GoogleCloudExtension.Utils.UnitTests
{
    /// <summary>
    /// Test class for the <see cref="NullEmptyInvisibleConverter"/>.
    /// </summary>
    [TestClass]
    public class NullEmptyInvisibleConverterTests
    {
        private NullEmptyInvisibleConverter _objectUnderTest;

        [TestInitialize]
        public void BeforeEachTest()
        {
            _objectUnderTest = new NullEmptyInvisibleConverter();
        }

        [TestMethod]
        public void TestConvertNull()
        {
            object val = _objectUnderTest.Convert(null, null, null, null);

            Assert.AreEqual(Visibility.Collapsed, val);
        }

        [TestMethod]
        public void TestConvertNotNull()
        {
            object val = _objectUnderTest.Convert(new object(), null, null, null);

            Assert.AreEqual(Visibility.Visible, val);
        }

        [TestMethod]
        public void TestConvertEmpty()
        {
            object val = _objectUnderTest.Convert("", null, null, null);

            Assert.AreEqual(Visibility.Collapsed, val);
        }

        [TestMethod]
        public void TestConvertWhitespace()
        {
            object val = _objectUnderTest.Convert(" \t\n\f\r", null, null, null);

            Assert.AreEqual(Visibility.Collapsed, val);
        }

        [TestMethod]
        public void TestConvertString()
        {
            object val = _objectUnderTest.Convert("not an empty string", null, null, null);

            Assert.AreEqual(Visibility.Visible, val);
        }

        [TestMethod]
        public void TestConvertNullInvert()
        {
            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert(null, null, null, null);

            Assert.AreEqual(Visibility.Visible, val);
        }

        [TestMethod]
        public void TestConvertNotNullInvert()
        {
            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert(new object(), null, null, null);

            Assert.AreEqual(Visibility.Collapsed, val);
        }

        [TestMethod]
        public void TestConvertEmptyInvert()
        {
            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert("", null, null, null);

            Assert.AreEqual(Visibility.Visible, val);
        }

        [TestMethod]
        public void TestConvertWhitespaceInvert()
        {
            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert(" \t\n\f\r", null, null, null);

            Assert.AreEqual(Visibility.Visible, val);
        }

        [TestMethod]
        public void TestConvertStringInvert()
        {
            _objectUnderTest.Invert = true;
            object val = _objectUnderTest.Convert("not an empty string", null, null, null);

            Assert.AreEqual(Visibility.Collapsed, val);
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
    }
}
