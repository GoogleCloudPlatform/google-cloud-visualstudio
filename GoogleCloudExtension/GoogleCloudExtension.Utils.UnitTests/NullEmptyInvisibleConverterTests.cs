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
            var val = _objectUnderTest.Convert(null, null, null, null);

            Assert.AreEqual(Visibility.Collapsed, val);
        }

        [TestMethod]
        public void TestConvertNotNull()
        {
            var val = _objectUnderTest.Convert(new Object(), null, null, null);

            Assert.AreEqual(Visibility.Visible, val);
        }

        [TestMethod]
        public void TestConvertEmpty()
        {
            var val = _objectUnderTest.Convert("", null, null, null);

            Assert.AreEqual(Visibility.Collapsed, val);
        }

        [TestMethod]
        public void TestConvertWhitespace()
        {
            var val = _objectUnderTest.Convert(" \t\n\f\r", null, null, null);

            Assert.AreEqual(Visibility.Collapsed, val);
        }

        [TestMethod]
        public void TestConvertString()
        {
            var val = _objectUnderTest.Convert("not an empty string", null, null, null);

            Assert.AreEqual(Visibility.Visible, val);
        }

        [TestMethod]
        public void TestConvertNullInvert()
        {
            _objectUnderTest.Invert = true;
            var val = _objectUnderTest.Convert(null, null, null, null);

            Assert.AreEqual(Visibility.Visible, val);
        }

        [TestMethod]
        public void TestConvertNotNullInvert()
        {
            _objectUnderTest.Invert = true;
            var val = _objectUnderTest.Convert(new Object(), null, null, null);

            Assert.AreEqual(Visibility.Collapsed, val);
        }

        [TestMethod]
        public void TestConvertEmptyInvert()
        {
            _objectUnderTest.Invert = true;
            var val = _objectUnderTest.Convert("", null, null, null);

            Assert.AreEqual(Visibility.Visible, val);
        }

        [TestMethod]
        public void TestConvertWhitespaceInvert()
        {
            _objectUnderTest.Invert = true;
            var val = _objectUnderTest.Convert(" \t\n\f\r", null, null, null);

            Assert.AreEqual(Visibility.Visible, val);
        }

        [TestMethod]
        public void TestConvertStringInvert()
        {
            _objectUnderTest.Invert = true;
            var val = _objectUnderTest.Convert("not an empty string", null, null, null);

            Assert.AreEqual(Visibility.Collapsed, val);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public void TestConvertBack()
        {
            _objectUnderTest.ConvertBack(null, null, null, null);
            Assert.Fail();
        }

        [TestMethod]
        public void TestProvideValue()
        {
            var val = _objectUnderTest.ProvideValue(Mock.Of<IServiceProvider>());

            Assert.AreEqual(_objectUnderTest, val);
        }
    }
}
