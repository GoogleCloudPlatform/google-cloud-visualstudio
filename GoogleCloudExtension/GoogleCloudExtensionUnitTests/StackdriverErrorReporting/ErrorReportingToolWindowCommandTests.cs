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

namespace GoogleCloudExtensionUnitTests.StackdriverErrorReporting
{
    [TestClass]
    public class ErrorReportingToolWindowCommandTests
    {
        private Mock<IMenuCommandService> _menuCommandServiceMock;
        private Package _mockPackage;

        [TestInitialize]
        public void BeforeEach()
        {
            _menuCommandServiceMock = new Mock<IMenuCommandService>();
            var packageMock = new Mock<Package>();
            packageMock.As<IServiceProvider>().Setup(sp => sp.GetService(typeof(IMenuCommandService)))
                .Returns(_menuCommandServiceMock.Object);
            _mockPackage = packageMock.Object;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestInitializeNullPackage() => ErrorReportingToolWindowCommand.Initialize(null);

        [TestMethod]
        public void TestInitialize()
        {
            ErrorReportingToolWindowCommand.Initialize(_mockPackage);

            Assert.IsNotNull(ErrorReportingToolWindowCommand.Instance);
            _menuCommandServiceMock.Verify(
                s => s.AddCommand(
                    It.Is<OleMenuCommand>(
                        mi => mi.CommandID.Guid == ErrorReportingToolWindowCommand.CommandSet &&
                            mi.CommandID.ID == ErrorReportingToolWindowCommand.CommandId)));
        }
    }
}
