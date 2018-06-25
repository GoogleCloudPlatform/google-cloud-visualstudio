// Copyright 2017 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension;
using GoogleCloudExtension.Analytics;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Windows;
using System.Windows.Threading;

namespace GoogleCloudExtensionUnitTests
{
    [TestClass]
    public static class AssemblyInitialize
    {
        private static IGoogleCloudExtensionPackage s_packageToRestore;

        [AssemblyInitialize]
        public static void InitializeAssembly(TestContext context)
        {
            // This fixes some odd unit test errors loading Microsoft.VisualStudio.Utilities
            // see: https://ci.appveyor.com/project/GoogleCloudPlatform/google-cloud-visualstudio/build/2.0.0-dev.135/tests
            new Mock<ISettingsManager>().Setup(m => m.GetSubset(It.IsAny<string>()))
                .Returns(Mock.Of<ISettingsSubset>());

            EventsReporterWrapper.DisableReporting();
            s_packageToRestore = GoogleCloudExtensionPackage.Instance;
            GoogleCloudExtensionPackage.Instance = null;
            // Enable pack URIs.
            Assert.AreEqual(new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown }, Application.Current);
        }

        [AssemblyCleanup]
        public static void CleanupAfterAllTests()
        {
            GoogleCloudExtensionPackage.Instance = s_packageToRestore;
            Application.Current.Shutdown();
            Dispatcher.CurrentDispatcher.InvokeShutdown();

        }
    }
}
