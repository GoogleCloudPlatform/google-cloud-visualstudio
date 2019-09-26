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

using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Google.Apis.Appengine.v1.Data;
using GoogleCloudExtension.CloudExplorerSources.Gae;
using GoogleCloudExtension.StackdriverLogsViewer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Resources = GoogleCloudExtension.Resources;

namespace GoogleCloudExtensionUnitTests.CloudExplorerSources.Gae
{
    [TestClass]
    public class ServiceViewModelTests : ExtensionTestBase
    {

        [TestMethod]
        public void TestOnBrowseStackdriverLogCommand()
        {
            var logsToolWindowMock = new Mock<LogsViewerToolWindow> { CallBase = true };
            logsToolWindowMock.Object.Frame = VsWindowFrameMocks.GetMockedWindowFrame();
            PackageMock.Setup(
                    p => p.FindToolWindow<LogsViewerToolWindow>(
                        false,
                        It.IsAny<int>()))
                .Returns(() => null);
            PackageMock.Setup(p => p.FindToolWindow<LogsViewerToolWindow>(true, It.IsAny<int>()))
                .Returns(logsToolWindowMock.Object);

            string filter = null;
            logsToolWindowMock.Setup(w => w.ViewModel.FilterLog(It.IsAny<string>())).Callback((string s) => filter = s);
            var objectUnderTest = new ServiceViewModel(
                new GaeSourceRootViewModel(), Mock.Of<Service>(s => s.Id == "IdString"), new List<VersionViewModel>());

            MenuItem logsMenuItem = objectUnderTest.ContextMenu.ItemsSource.OfType<MenuItem>().Single(
                mi => mi.Header as string == Resources.CloudExplorerLaunchLogsViewerMenuHeader);
            logsMenuItem.Command.Execute(null);

            StringAssert.Contains(filter, "resource.type=\"gae_app\"");
            StringAssert.Contains(filter, "resource.labels.module_id=\"IdString\"");
        }
    }
}
