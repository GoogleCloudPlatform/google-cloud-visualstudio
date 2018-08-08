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
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.CloudExplorerSources.CloudConsoleLinks
{
    [TestClass]
    public class ConsoleLinkTests : ExtensionTestBase
    {
        private const string DefaultCaption = "Default Caption";
        private const string DefaultUrl = "url://default";
        private static readonly LinkInfo s_defaultLinkInfo = new LinkInfo(DefaultUrl, DefaultCaption);
        private Mock<IBrowserService> _browserServiceMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _browserServiceMock = new Mock<IBrowserService>();

            PackageMock.Setup(p => p.GetMefServiceLazy<IBrowserService>()).Returns(_browserServiceMock.ToLazy());
        }

        [TestMethod]
        public void TestConstructor_SetsCaption()
        {
            const string testCaption = "Test Caption";
            var objectUnderTest = new ConsoleLink(Mock.Of<ICloudSourceContext>(), new LinkInfo(DefaultUrl, testCaption));

            Assert.AreEqual(testCaption, objectUnderTest.Caption);
        }

        [TestMethod]
        public void TestConstructor_CreatesNavigateCommand()
        {
            var objectUnderTest = new ConsoleLink(Mock.Of<ICloudSourceContext>(), s_defaultLinkInfo);

            Assert.IsTrue(objectUnderTest.NavigateCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestConstructor_SetsHelpLinkInfo()
        {
            var expectedHelpLinkInfo = new LinkInfo("url://test/url", "Test Caption");
            var objectUnderTest = new ConsoleLink(
                Mock.Of<ICloudSourceContext>(),
                new LinkInfo(DefaultUrl, DefaultCaption),
                expectedHelpLinkInfo);

            Assert.AreEqual(expectedHelpLinkInfo, objectUnderTest.InfoLinkInfo);
        }

        [TestMethod]
        public void TestConstructor_EnablesNavigateHelpCommand()
        {
            var objectUnderTest = new ConsoleLink(
                Mock.Of<ICloudSourceContext>(),
                new LinkInfo(DefaultUrl, DefaultCaption),
                new LinkInfo(DefaultUrl, DefaultCaption));

            Assert.IsTrue(objectUnderTest.NavigateInfoCommand.CanExecute(null));
        }

        [TestMethod]
        public void TestNavigateHelpCommand_NavigatesToUrl()
        {
            const string testUrl = "url://test/url";

            var objectUnderTest = new ConsoleLink(
                Mock.Of<ICloudSourceContext>(),
                new LinkInfo(DefaultUrl, DefaultCaption),
                new LinkInfo(testUrl, "Test Caption"));
            objectUnderTest.NavigateInfoCommand.Execute(null);

            _browserServiceMock.Verify(b => b.OpenBrowser(testUrl));

        }

        [TestMethod]
        public void TestNavigateCommand_NavigatesToUrlMissingProject()
        {
            const string testUrlFormat = "url://default?project={0}";
            const string expectedUrl = "url://default?project=";
            var objectUnderTest = new ConsoleLink(
                Mock.Of<ICloudSourceContext>(),
                new LinkInfo(testUrlFormat, DefaultCaption));

            objectUnderTest.NavigateCommand.Execute(null);

            _browserServiceMock.Verify(b => b.OpenBrowser(expectedUrl), Times.Once);
        }

        [TestMethod]
        public void TestNavigateCommand_NavigatesToUrl()
        {
            const string testUrlFormat = "url://project/{0}/default";
            const string testProjectId = "testProjectId";
            const string expectedUrl = "url://project/testProjectId/default";
            var objectUnderTest = new ConsoleLink(
                Mock.Of<ICloudSourceContext>(c => c.CurrentProject.ProjectId == testProjectId),
                new LinkInfo(testUrlFormat, DefaultCaption));

            objectUnderTest.NavigateCommand.Execute(null);

            _browserServiceMock.Verify(b => b.OpenBrowser(expectedUrl), Times.Once);
        }

        [TestMethod]
        public void TestNavigateCommand_NavigatesToUpdatedUrl()
        {
            const string testUrlFormat = "url://project/{0}/default";
            const string firstProjectId = "firstProjectId";
            const string firstExpectedUrl = "url://project/firstProjectId/default";
            const string secondProjectId = "secondProjectId";
            const string secondExpectedUrl = "url://project/secondProjectId/default";
            var contextMock = new Mock<ICloudSourceContext>();
            contextMock.Setup(c => c.CurrentProject.ProjectId).Returns(firstProjectId);
            var objectUnderTest = new ConsoleLink(contextMock.Object, new LinkInfo(testUrlFormat, DefaultCaption));

            objectUnderTest.NavigateCommand.Execute(null);
            contextMock.Setup(c => c.CurrentProject.ProjectId).Returns(secondProjectId);
            objectUnderTest.NavigateCommand.Execute(null);

            _browserServiceMock.Verify(b => b.OpenBrowser(firstExpectedUrl), Times.Once);
            _browserServiceMock.Verify(b => b.OpenBrowser(secondExpectedUrl), Times.Once);
        }
    }
}
