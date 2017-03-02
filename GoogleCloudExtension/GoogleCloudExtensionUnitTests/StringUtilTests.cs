using static GoogleCloudExtension.Utils.StringUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests
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
            Assert.AreEqual(8, FirstNonSpaceIndex("   uu  pp  "));
        }

    }
}
