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
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Diagnostics;

namespace GoogleCloudExtensionUnitTests.CloudExplorerSources.CloudConsoleLinks
{
    [TestClass]
    public class ConsoleLinkTests
    {
        private const string DefaultCaption = "Default Caption";
        private const string DefaultUrl = "url://default";
        private static readonly LinkInfo s_defaultLinkInfo = new LinkInfo(DefaultUrl, DefaultCaption);
        private Mock<Func<string, Process>> _startProcessMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _startProcessMock = new Mock<Func<string, Process>>();
        }
        [TestMethod]
        public void TestConstructor_SetsCaption()
        {
            const string testCaption = "Test Caption";
            var objectUnderTest = new ConsoleLink(new LinkInfo(DefaultUrl, testCaption), Mock.Of<ICloudSourceContext>());

            Assert.AreEqual(testCaption, objectUnderTest.Caption);
        }

        [TestMethod]
        public void TestConstructor_CreatesNavigateCommand()
        {
            var objectUnderTest = new ConsoleLink(s_defaultLinkInfo, Mock.Of<ICloudSourceContext>());

            Assert.IsTrue(objectUnderTest.NavigateCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestNavigateCommand_NavigatesToUrlMissingProject()
        {
            const string testUrlFormat = "url://default?project={0}";
            const string expectedUrl = "url://default?project=";
            var objectUnderTest = new ConsoleLink(
                new LinkInfo(testUrlFormat, DefaultCaption),
                Mock.Of<ICloudSourceContext>(),
                _startProcessMock.Object);

            objectUnderTest.NavigateCommand.Execute(null);

            _startProcessMock.Verify(f => f(expectedUrl), Times.Once);
        }

        [TestMethod]
        public void TestNavigateCommand_NavigatesToUrl()
        {
            const string testUrlFormat = "url://project/{0}/default";
            const string testProjectId = "testProjectId";
            const string expectedUrl = "url://project/testProjectId/default";
            var objectUnderTest = new ConsoleLink(
                new LinkInfo(testUrlFormat, DefaultCaption),
                Mock.Of<ICloudSourceContext>(c => c.CurrentProject.ProjectId == testProjectId),
                _startProcessMock.Object);

            objectUnderTest.NavigateCommand.Execute(null);

            _startProcessMock.Verify(f => f(expectedUrl), Times.Once);
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
            var objectUnderTest = new ConsoleLink(
                new LinkInfo(testUrlFormat, DefaultCaption),
                contextMock.Object,
                _startProcessMock.Object);

            objectUnderTest.NavigateCommand.Execute(null);
            contextMock.Setup(c => c.CurrentProject.ProjectId).Returns(secondProjectId);
            objectUnderTest.NavigateCommand.Execute(null);

            _startProcessMock.Verify(f => f(firstExpectedUrl), Times.Once);
            _startProcessMock.Verify(f => f(secondExpectedUrl), Times.Once);
        }
    }
}
