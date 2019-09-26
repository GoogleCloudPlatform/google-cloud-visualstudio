using System;
using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.StackdriverLogsViewer;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.StackdriverLogsViewer
{

    [TestClass]
    public class LogsViewerToolWindowTests : ExtensionTestBase
    {
        private LogsViewerToolWindow _objectUnderTest;
        private Mock<IVsWindowFrame> _frameMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _frameMock = VsWindowFrameMocks.GetWindowFrameMock();
            _objectUnderTest = new LogsViewerToolWindow();
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            Assert.IsNull(_objectUnderTest.ViewModel);
            Assert.IsInstanceOfType(_objectUnderTest.Content, typeof(LogsViewerToolWindowControl));
            Assert.AreEqual(Resources.LogViewerToolWindowCaption, _objectUnderTest.Caption);
        }

        [TestMethod]
        public void TestToolWindowCreated()
        {
            _objectUnderTest.Frame = _frameMock.Object;

            Assert.IsNotNull(_objectUnderTest.ViewModel);
        }

        [TestMethod]
        public void TestProjectIdChanged()
        {
            _objectUnderTest.Frame = _frameMock.Object;
            ILogsViewerViewModel oldViewModel = _objectUnderTest.ViewModel;

            CredentialStoreMock.Raise(
                cs => cs.CurrentProjectIdChanged += null, CredentialsStore.Default, null);

            Assert.AreNotEqual(oldViewModel, _objectUnderTest.ViewModel);
        }

        [TestMethod]
        public void TestCloseDisablesProjectIdChanged()
        {
            _objectUnderTest.Frame = _frameMock.Object;
            ILogsViewerViewModel oldViewModel = _objectUnderTest.ViewModel;

            ((IVsWindowPane)_objectUnderTest).ClosePane();
            CredentialStoreMock.Raise(
                cs => cs.CurrentProjectIdChanged += null, CredentialsStore.Default, null);

            Assert.AreEqual(oldViewModel, _objectUnderTest.ViewModel);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidCastException))]
        public void TestSetContentOfInvalidType() => _objectUnderTest.Content = new object();

        [TestMethod]
        public void TestSetContentOfValidType()
        {
            var newContent = new LogsViewerToolWindowControl();
            _objectUnderTest.Content = newContent;

            Assert.AreEqual(newContent, _objectUnderTest.Content);
        }
    }
}
