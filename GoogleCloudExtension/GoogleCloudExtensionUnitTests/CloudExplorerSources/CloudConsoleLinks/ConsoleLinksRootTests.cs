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

using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.CloudExplorerSources.CloudConsoleLinks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GoogleCloudExtensionUnitTests.CloudExplorerSources.CloudConsoleLinks
{
    [TestClass]
    public class ConsoleLinksRootTests
    {
        [TestMethod]
        public void TestConstructor_SetsCaption()
        {
            var objectUnderTest = new ConsoleLinksRoot(Mock.Of<ICloudSourceContext>());

            Assert.AreEqual(ConsoleLinksRoot.s_consoleHomeFormatInfo.Caption, objectUnderTest.Caption);
        }

        [TestMethod]
        public void TestConstructor_InitalizesNavigateCommand()
        {
            var objectUnderTest = new ConsoleLinksRoot(Mock.Of<ICloudSourceContext>());

            Assert.IsTrue(objectUnderTest.NavigateCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestConstructor_AddsDirectLinkChildren()
        {
            var objectUnderTest = new ConsoleLinksRoot(Mock.Of<ICloudSourceContext>());

            List<TreeNode> directLinkChildren = objectUnderTest.Children
                .Take(ConsoleLinksRoot.s_primaryConsoleLinkFormats.Count).ToList();
            CollectionAssert.AllItemsAreInstancesOfType(directLinkChildren, typeof(ConsoleLink));
            CollectionAssert.AreEqual(
                ConsoleLinksRoot.s_primaryConsoleLinkFormats.Select(l => l.Caption).ToList(),
                directLinkChildren.Select(c => c.Caption).ToList());
        }

        [TestMethod]
        public void TestConstructor_AddsGroupLinkChildren()
        {
            var objectUnderTest = new ConsoleLinksRoot(Mock.Of<ICloudSourceContext>());

            List<TreeNode> linkGroupChildren = objectUnderTest.Children
                .Skip(ConsoleLinksRoot.s_primaryConsoleLinkFormats.Count).ToList();
            CollectionAssert.AllItemsAreInstancesOfType(linkGroupChildren, typeof(ConsoleLinkGroup));
            CollectionAssert.AreEqual(
                ConsoleLinksRoot.s_groupedConsoleLinkFormats.Select(t => t.Item1).ToList(),
                linkGroupChildren.Select(c => c.Caption).ToList());
        }

        [TestMethod]
        public void TestNavigateCommand_NavigatesToUrlMissingProject()
        {
            var startProcessMock = new Mock<Func<string, Process>>();
            var objectUnderTest = new ConsoleLinksRoot(
                Mock.Of<ICloudSourceContext>(c => c.CurrentProject == null), startProcessMock.Object);

            objectUnderTest.NavigateCommand.Execute(null);

            const string expectedUrl = "https://console.cloud.google.com/?project=";
            startProcessMock.Verify(f => f(expectedUrl), Times.Once);
        }

        [TestMethod]
        public void TestNavigateCommand_NavigatesToUrlOfCurrentProject()
        {
            var startProcessMock = new Mock<Func<string, Process>>();
            var contextMock = new Mock<ICloudSourceContext>();
            var objectUnderTest = new ConsoleLinksRoot(
                contextMock.Object, startProcessMock.Object);

            contextMock.Setup(c => c.CurrentProject.ProjectId).Returns("firstProjectId");
            objectUnderTest.NavigateCommand.Execute(null);
            contextMock.Setup(c => c.CurrentProject.ProjectId).Returns("secondProjectId");
            objectUnderTest.NavigateCommand.Execute(null);

            const string firstExpectedUrl = "https://console.cloud.google.com/?project=firstProjectId";
            const string secondExpectedUrl = "https://console.cloud.google.com/?project=secondProjectId";
            startProcessMock.Verify(f => f(firstExpectedUrl), Times.Once);
            startProcessMock.Verify(f => f(secondExpectedUrl), Times.Once);
        }

        [TestMethod]
        public void TestRefresh_DoesNotThrow()
        {
            var objectUnderTest = new ConsoleLinksRoot(Mock.Of<ICloudSourceContext>());

            objectUnderTest.Refresh();
        }

        [TestMethod]
        public void TestInvalidateProjectOrAccount_DoesNotThrow()
        {
            var objectUnderTest = new ConsoleLinksRoot(Mock.Of<ICloudSourceContext>());

            objectUnderTest.InvalidateProjectOrAccount();
        }
    }
}
