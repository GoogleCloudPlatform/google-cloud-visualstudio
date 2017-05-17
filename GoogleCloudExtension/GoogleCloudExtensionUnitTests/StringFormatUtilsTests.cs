using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests
{
    [TestClass]
    public class StringFormatUtilsTests
    {
        [TestMethod]
        public void BasicFormattingTests()
        {
            // Ensures that the basic formatting works. The actual details of how the number is converted
            // into a human readable size are implemented by the Win32 API StrFormatByteSizeW. This test ensures
            // that we can call into the API and get a valid result.
            Assert.AreEqual("10 bytes", StringFormatUtils.FormatByteSize(10));
            Assert.AreEqual("1.00 KB", StringFormatUtils.FormatByteSize(1024));
            Assert.AreEqual("1.00 MB", StringFormatUtils.FormatByteSize(1024 * 1024));
            Assert.AreEqual("1.00 GB", StringFormatUtils.FormatByteSize(1024 * 1024 * 1024));
        }
    }
}
