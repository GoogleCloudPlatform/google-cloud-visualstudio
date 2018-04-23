using GoogleCloudExtension;
using GoogleCloudExtension.Analytics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests
{
    public abstract class ExtensionTestBase
    {

        protected Mock<IGoogleCloudExtensionPackage> PackageMock { get; private set; }

        [TestInitialize]
        public void IntializeGlobalsForTest()
        {
            PackageMock = new Mock<IGoogleCloudExtensionPackage>();
            GoogleCloudExtensionPackage.Instance = PackageMock.Object;
            EventsReporterWrapper.DisableReporting();
            BeforeEach();
        }

        protected virtual void BeforeEach() { }

        [TestCleanup]
        public void CleanupGlobalsForTest()
        {
            AfterEach();
            GoogleCloudExtensionPackage.Instance = null;
        }

        protected virtual void AfterEach() { }
    }
}
