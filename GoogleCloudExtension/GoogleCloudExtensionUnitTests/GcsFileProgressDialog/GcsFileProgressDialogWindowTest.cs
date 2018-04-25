﻿// Copyright 2018 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GoogleCloudExtension.GcsFileProgressDialog;
using GoogleCloudExtension.GcsUtils;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.GcsFileProgressDialog
{
    /// <summary>
    /// Summary description for GcsFileProgressDialogContentTest
    /// </summary>
    [TestClass]
    public class GcsFileProgressDialogWindowTest
    {
        private Mock<IServiceProvider> _serviceProviderMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();

            Mock<IVsSettingsManager> settingManagerMock =
                _serviceProviderMock.SetupService<SVsSettingsManager, IVsSettingsManager>();

            // ReSharper disable once RedundantAssignment
            var intValue = 0;
            // ReSharper disable once RedundantAssignment
            var store = Mock.Of<IVsSettingsStore>(
                ss => ss.GetIntOrDefault(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), out intValue) == 0);
            settingManagerMock.Setup(sm => sm.GetReadOnlySettingsStore(It.IsAny<uint>(), out store)).Returns(0);

            _serviceProviderMock.SetupService<IVsUIShell, IVsUIShell>();

            _serviceProviderMock.SetupDefaultServices();
            _serviceProviderMock.SetAsGlobalProvider();
        }

        [TestCleanup]
        public void AfterEach()
        {
            _serviceProviderMock.Dispose();
        }

        [TestMethod]
        [TestCategory("WPF")]
        public async Task TestBindingsLoadCorrectly()
        {
            var tcs = new TaskCompletionSource<bool>();
            var objectUnderTest = new GcsFileProgressDialogWindow(
                "test-caption", "test-message", "test-progress-message", new GcsOperation[0],
                new CancellationTokenSource());
            objectUnderTest.SourceInitialized += (sender, args) =>
            {
                objectUnderTest.Close();
                tcs.SetResult(true);
            };

            objectUnderTest.ShowModal();
            await tcs.Task;
        }
    }
}
