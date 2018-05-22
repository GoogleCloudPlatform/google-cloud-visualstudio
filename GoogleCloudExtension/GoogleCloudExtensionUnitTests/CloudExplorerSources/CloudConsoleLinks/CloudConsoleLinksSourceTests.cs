using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.CloudExplorerSources.CloudConsoleLinks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.CloudExplorerSources.CloudConsoleLinks
{
    [TestClass]
    public class CloudConsoleLinksSourceTests
    {
        [TestMethod]
        public void TestConstructor_SetsRoot()
        {
            var objectUnderTest = new CloudConsoleLinksSource(Mock.Of<ICloudSourceContext>());

            Assert.IsNotNull(objectUnderTest.Root);
        }
    }
}