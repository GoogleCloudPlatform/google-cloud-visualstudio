using GoogleCloudExtension;
using GoogleCloudExtension.Extensions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests
{
    [TestClass]
    public class WindowFrameExtensionTests
    {
        private int _isOnScreen;
        private Mock<IVsWindowFrame> _frameMock;
        private Mock<GoogleCloudExtensionPackage> _packageMock;
        private GoogleCloudExtensionPackage _oldPackage;

        [TestInitialize]
        public void TestInit()
        {
            _frameMock = new Mock<IVsWindowFrame>();
            _packageMock = new Mock<GoogleCloudExtensionPackage>();
            _oldPackage = GoogleCloudExtensionPackage.Instance;
            GoogleCloudExtensionPackage.Instance = _packageMock.Object;
        }

        [TestCleanup]
        public void TestCleanup()
        {
            GoogleCloudExtensionPackage.Instance = _oldPackage;
        }

        [TestMethod]
        public void TestWhenOnScreenAndMaximized()
        {
            _isOnScreen = 1;
            _frameMock.Setup(o => o.IsOnScreen(out _isOnScreen)).Returns(VSConstants.S_OK);
            _packageMock.Setup(o => o.IsWindowActive()).Returns(true);

            Assert.IsTrue(_frameMock.Object.IsVisibleOnScreen());
        }

        [TestMethod]
        public void TestWhenOnScreenAndMinimized()
        {
            _isOnScreen = 1;
            _frameMock.Setup(o => o.IsOnScreen(out _isOnScreen)).Returns(VSConstants.S_OK);
            _packageMock.Setup(o => o.IsWindowActive()).Returns(false);

            Assert.IsFalse(_frameMock.Object.IsVisibleOnScreen());
        }

        [TestMethod]
        public void TestWhenOffScreenAndMaximized()
        {
            _isOnScreen = 0;
            _frameMock.Setup(o => o.IsOnScreen(out _isOnScreen)).Returns(VSConstants.S_OK);
            _packageMock.Setup(o => o.IsWindowActive()).Returns(true);

            Assert.IsFalse(_frameMock.Object.IsVisibleOnScreen());
        }

        [TestMethod]
        public void TestWhenOffScreenAndMinimized()
        {
            _isOnScreen = 0;
            _frameMock.Setup(o => o.IsOnScreen(out _isOnScreen)).Returns(VSConstants.S_OK);
            _packageMock.Setup(o => o.IsWindowActive()).Returns(false);

            Assert.IsFalse(_frameMock.Object.IsVisibleOnScreen());
        }
    }
}
