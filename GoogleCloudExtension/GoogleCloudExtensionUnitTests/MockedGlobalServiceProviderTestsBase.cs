using EnvDTE;
using GoogleCloudExtension;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests
{
    public abstract class MockedGlobalServiceProviderTestsBase
    {
        protected Mock<DTE> DteMock { get; private set; }
        protected Mock<IServiceProvider> ServiceProviderMock { get; private set; }
        protected abstract IVsPackage Package { get; }

        protected void RunPackageInitalize()
        {
            // This runs the Initialize() method.
            Package.SetSite(ServiceProviderMock.Object);
        }

        [TestInitialize]
        public void TestInitalize()
        {
            DteMock = new Mock<DTE>();
            ServiceProviderMock = DteMock.As<IServiceProvider>();
            ServiceProviderMock.SetupService<DTE, DTE>(DteMock);
            ServiceProviderMock.SetupDefaultServices();
        }

        [TestCleanup]
        public void AfterEach()
        {
            GoogleCloudExtensionPackage.Instance = null;
            ServiceProviderMock.Dispose();
        }
    }
}