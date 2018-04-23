using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.VsVersion;
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
            CredentialsStore.CreateNewOverride();
            PackageMock = new Mock<IGoogleCloudExtensionPackage>(MockBehavior.Strict);
            GoogleCloudExtensionPackage.Instance = PackageMock.Object;
            PackageMock.Setup(p => p.VsVersion).Returns(VsVersionUtils.VisualStudio2017Version);
            EventsReporterWrapper.DisableReporting();
            BeforeEach();
        }

        protected virtual void BeforeEach() { }

        [TestCleanup]
        public void CleanupGlobalsForTest()
        {
            AfterEach();
            GoogleCloudExtensionPackage.Instance = null;
            CredentialsStore.ClearOverride();
        }

        protected virtual void AfterEach() { }
    }
}
