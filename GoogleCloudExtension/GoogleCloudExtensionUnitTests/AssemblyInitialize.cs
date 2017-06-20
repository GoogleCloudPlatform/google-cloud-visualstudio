
using GoogleCloudExtension.Analytics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;

namespace GoogleCloudExtensionUnitTests
{
    [TestClass]
    public static class AssemblyInitialize
    {
        [AssemblyInitialize]
        public static void InitializeAssembly(TestContext context)
        {
            EventsReporterWrapper.DisableReporting();
            // Enable pack URIs.
            Assert.AreEqual(new Application(), Application.Current);
        }
    }
}
