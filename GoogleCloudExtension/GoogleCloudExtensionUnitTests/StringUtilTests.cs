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
    }
}
