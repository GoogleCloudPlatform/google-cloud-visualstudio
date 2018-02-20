﻿using GoogleCloudExtension.GcsFileProgressDialog;
using GoogleCloudExtension.GcsUtils;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GoogleCloudExtensionUnitTests.GcsFileProgressDialog
{
    /// <summary>
    /// Summary description for GcsFileProgressDialogContentTest
    /// </summary>
    [TestClass]
    public class GcsFileProgressDialogWindowTest
    {
        private Mock<IVsSettingsManager> _settingManagerMock;
        private Mock<IVsUIShell> _uiShellMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _settingManagerMock = new Mock<IVsSettingsManager>();
            _uiShellMock = new Mock<IVsUIShell>();
            int intValue = 0;
            IVsSettingsStore store = Mock.Of<IVsSettingsStore>(
                ss => ss.GetIntOrDefault(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), out intValue) == 0);
            _settingManagerMock.Setup(sm => sm.GetReadOnlySettingsStore(It.IsAny<uint>(), out store)).Returns(0);
            GoogleCloudExtensionPackageTests.InitPackageMock(dte =>
            {
                Mock<IServiceProvider> providerMock = dte.As<IServiceProvider>();
                GoogleCloudExtensionPackageTests.SetupService<SVsSettingsManager, IVsSettingsManager>(
                    providerMock, _settingManagerMock);
                GoogleCloudExtensionPackageTests.SetupService<IVsUIShell, IVsUIShell>(providerMock, _uiShellMock);
            });
        }

        [TestMethod]
        public async Task TestBindingsLoadCorrectly()
        {
            var objectUnderTest = new GcsFileProgressDialogWindow(
                "test-caption", "test-message", "test-progress-message", new GcsOperation[0],
                new CancellationTokenSource());
            Task closeTask = Application.Current.Dispatcher.InvokeAsync(
               () =>
               {
                   Thread.Yield();
                   objectUnderTest.Close();
               }).Task;
            objectUnderTest.ShowModal();
            await closeTask;
        }
    }
}
