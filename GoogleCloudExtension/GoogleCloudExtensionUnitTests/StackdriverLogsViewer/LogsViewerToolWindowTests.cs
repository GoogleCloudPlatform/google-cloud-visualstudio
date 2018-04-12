using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.StackdriverLogsViewer
{

    [TestClass]
    public class LogsViewerToolWindowTests
    {
        public static IVsWindowFrame GetMockedWindowFrame()
        {
            // ReSharper disable once RedundantAssignment
            object outProperty = null;
            return Mock.Of<IVsWindowFrame>(
                f => f.Show() == VSConstants.S_OK &&
                    f.GetProperty(It.IsAny<int>(), out outProperty) == VSConstants.S_OK);
        }

        public static Mock<IVsWindowFrame> GetWindowFrameMock()
        {
            return Mock.Get(GetMockedWindowFrame());
        }
    }
}