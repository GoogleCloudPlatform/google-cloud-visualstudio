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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension;
using GoogleCloudExtension.FirewallManagement;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.FirewallManagement
{
    [TestClass]
    public class PortManagerWindowTests : WpfTestBase<PortManagerWindow>
    {
        protected override void RegisterActivatedEvent(EventHandler handler)
        {
            PortManagerWindow.WindowActivated += handler;
        }

        protected override void UnregisterActivatedEvent(EventHandler handler)
        {
            PortManagerWindow.WindowActivated -= handler;
        }

        [TestInitialize]
        public void BeforeEach()
        {
            GoogleCloudExtensionPackage.Instance =
                Mock.Of<IGoogleCloudExtensionPackage>(p => p.VsVersion == VsVersionUtils.VisualStudio2017Version);
        }

        [TestMethod]
        public async Task TestPromptUser_SetsTitle()
        {
            PortManagerWindow window = await GetWindowAsync(() => PortManagerWindow.PromptUser(new Instance()));

            Assert.AreEqual(Resources.PortManagerWindowCaption, window.Title);
        }

        [TestMethod]
        public async Task TestPromptUser_ReturnsNullWhenCancelled()
        {
            PortChanges result = await GetResult(w => w.Close(), () => PortManagerWindow.PromptUser(new Instance()));
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestPromptUser_ReturnsResultsOnOkCommand()
        {
            PortChanges result = await GetResult(
                w => w.ViewModel.OkCommand.Execute(null),
                () => PortManagerWindow.PromptUser(new Instance()));
            Assert.IsNotNull(result);
        }
    }
}
