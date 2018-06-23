// Copyright 2018 Google Inc. All Rights Reserved.
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

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Threading;

namespace GoogleCloudExtensionUnitTests
{
    public class WpfTestBase : MockedGlobalServiceProviderTestsBase
    {
        protected override IVsPackage Package => _packageMock.Object;
        private Mock<Package> _packageMock;

        [TestInitialize]
        public void InitWpfServiceProvider()
        {
            // Allow previous windows to die before bringing up a new one.
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

            _packageMock = new Mock<Package> { CallBase = true };

            Mock<IVsSettingsManager> settingsManagerMock =
                ServiceProviderMock.SetupService<SVsSettingsManager, IVsSettingsManager>();

            // ReSharper disable once RedundantAssignment
            var intValue = 0;
            // ReSharper disable once RedundantAssignment
            var store = Mock.Of<IVsSettingsStore>(
                ss => ss.GetIntOrDefault(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), out intValue) == 0);
            settingsManagerMock.Setup(sm => sm.GetReadOnlySettingsStore(It.IsAny<uint>(), out store)).Returns(0);

            ServiceProviderMock.SetupService<IVsUIShell, IVsUIShell>();

            // Reset the service provider in an internal microsoft class.
            Type windowHelper = typeof(Microsoft.Internal.VisualStudio.PlatformUI.WindowHelper);
            PropertyInfo serviceProviderProperty =
                windowHelper.GetProperty("ServiceProvider", BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(serviceProviderProperty != null);
            serviceProviderProperty.SetMethod.Invoke(null, new object[] { null });

            // Set the global service provider.
            RunPackageInitalize();
        }
    }
}