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

using GoogleCloudExtension.StackdriverErrorReporting;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.ComponentModel.Design;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace GoogleCloudExtensionUnitTests.StackdriverErrorReporting
{
    [TestClass]
    public class ErrorReportingToolWindowCommandTests : ExtensionTestBase
    {
        private Mock<IMenuCommandService> _menuCommandServiceMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _menuCommandServiceMock = new Mock<IMenuCommandService>();
            PackageMock.Setup(sp => sp.GetServiceAsync<IMenuCommandService, IMenuCommandService>())
                .Returns(Task.FromResult(_menuCommandServiceMock.Object));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task TestInitializeNullPackage() =>
            await ErrorReportingToolWindowCommand.InitializeAsync(null, CancellationToken.None);

        [TestMethod]
        public async Task TestInitialize()
        {
            await ErrorReportingToolWindowCommand.InitializeAsync(PackageMock.Object, CancellationToken.None);

            _menuCommandServiceMock.Verify(
                s => s.AddCommand(
                    It.Is<OleMenuCommand>(
                        mi => mi.CommandID.Guid == ErrorReportingToolWindowCommand.CommandSet &&
                            mi.CommandID.ID == ErrorReportingToolWindowCommand.CommandId)));
        }
    }
}
