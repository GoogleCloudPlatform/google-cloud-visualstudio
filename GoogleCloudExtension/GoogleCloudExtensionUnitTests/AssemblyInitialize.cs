using GoogleCloudExtension.Analytics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests
{
    [TestClass]
    public static class AssemblyInitialize
    {
        [AssemblyInitialize]
        public static void InitializeAssembly(TestContext context)
        {
            EventsReporterWrapper.DisableReporting();
        }
    }
}
