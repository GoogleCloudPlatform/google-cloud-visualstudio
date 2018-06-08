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

using EnvDTE;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Projects.DotNetCore;
using GoogleCloudExtension.Services.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Xml.Linq;
using Net4CsprojProject = GoogleCloudExtension.Projects.DotNet4.CsprojProject;
using NetCoreCsprojProject = GoogleCloudExtension.Projects.DotNetCore.CsprojProject;

namespace GoogleCloudExtensionUnitTests.Projects
{
    [TestClass]
    public class ProjectParserTests : ExtensionTestBase
    {
        private Mock<IFileSystem> _fileSystemMock;
        private Mock<Project> _projectMock;

        protected override void BeforeEach()
        {
            _projectMock = new Mock<Project>();
            _fileSystemMock = new Mock<IFileSystem>();
            PackageMock.Setup(p => p.GetMefService<IFileSystem>()).Returns(_fileSystemMock.Object);
        }

        [TestMethod]
        public void TestParseProject_ReturnsNullForBadFileExtension()
        {
            _projectMock.Setup(p => p.FullName).Returns(@"c:\path\to\project.bad");

            IParsedDteProject result = ProjectParser.ParseProject(_projectMock.Object);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestParseProject_ReturnsNullForXProjectExtensionMissingProjectJson()
        {
            _projectMock.Setup(p => p.FullName).Returns(@"c:\path\to\project.xproj");
            _fileSystemMock.Setup(fs => fs.File.Exists(@"c:\path\to\project.json")).Returns(false);

            IParsedDteProject result = ProjectParser.ParseProject(_projectMock.Object);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestParseProject_ReturnsJsonProjectForXProjectExtensionWithProjectJson()
        {
            _projectMock.Setup(p => p.FullName).Returns(@"c:\path\to\project.xproj");
            _fileSystemMock.Setup(fs => fs.File.Exists(@"c:\path\to\project.json")).Returns(true);

            IParsedDteProject result = ProjectParser.ParseProject(_projectMock.Object);

            Assert.IsInstanceOfType(result, typeof(JsonProject));
        }

        [TestMethod]
        public void TestParseProject_ReturnsCoreCsprojProjectForValidCoreCsproj()
        {
            var projectXDocument = new XDocument(
                new XElement(
                    XName.Get("Project"),
                    new XAttribute(XName.Get(ProjectParser.SdkAttributeName), ProjectParser.AspNetCoreSdk),
                    new XElement(
                        XName.Get(ProjectParser.PropertyGroupElementName),
                        new XElement(XName.Get(ProjectParser.TargetFrameworkElementName), "TargetFramework"))));

            const string projectFilePath = @"c:\path\to\project.csproj";
            _projectMock.Setup(p => p.FullName).Returns(projectFilePath);
            _fileSystemMock.Setup(fs => fs.XDocument.Load(projectFilePath)).Returns(projectXDocument);

            IParsedDteProject result = ProjectParser.ParseProject(_projectMock.Object);

            Assert.IsInstanceOfType(result, typeof(NetCoreCsprojProject));
        }

        [TestMethod]
        public void TestParseProject_ReturnsNullForCoreCsprojWrongSdk()
        {
            var projectXDocument = new XDocument(
                new XElement(
                    XName.Get("Project"),
                    new XAttribute(XName.Get(ProjectParser.SdkAttributeName), "Microsoft.Net.Sdk.Wrong")));

            const string projectFilePath = @"c:\path\to\project.csproj";
            _projectMock.Setup(p => p.FullName).Returns(projectFilePath);
            _fileSystemMock.Setup(fs => fs.XDocument.Load(projectFilePath)).Returns(projectXDocument);

            IParsedDteProject result = ProjectParser.ParseProject(_projectMock.Object);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestParseProject_ReturnsNullForOldCsprojMissingProjectTypeGuids()
        {
            var projectXDocument = new XDocument(new XElement(XName.Get("Project", ProjectParser.MsbuildNamespace)));

            const string projectFilePath = @"c:\path\to\project.csproj";
            _projectMock.Setup(p => p.FullName).Returns(projectFilePath);
            _fileSystemMock.Setup(fs => fs.XDocument.Load(projectFilePath)).Returns(projectXDocument);

            IParsedDteProject result = ProjectParser.ParseProject(_projectMock.Object);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestParseProject_ReturnsNullForOldCsprojWithWrongProjectTypeGuid()
        {
            var projectXDocument = new XDocument(
                new XElement(
                    XName.Get("Project", ProjectParser.MsbuildNamespace),
                    new XElement(
                        XName.Get(ProjectParser.PropertyGroupElementName, ProjectParser.MsbuildNamespace),
                        new XElement(
                            XName.Get(ProjectParser.PropertyTypeGuidsElementName, ProjectParser.MsbuildNamespace),
                            Guid.Empty.ToString()))));

            const string projectFilePath = @"c:\path\to\project.csproj";
            _projectMock.Setup(p => p.FullName).Returns(projectFilePath);
            _fileSystemMock.Setup(fs => fs.XDocument.Load(projectFilePath)).Returns(projectXDocument);

            IParsedDteProject result = ProjectParser.ParseProject(_projectMock.Object);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestParseProject_ReturnsNet4CsprojProjectForOldCsprojWithWebAppProjectTypeGuid()
        {
            var projectXDocument = new XDocument(
                new XElement(
                    XName.Get("Project", ProjectParser.MsbuildNamespace),
                    new XElement(
                        XName.Get(ProjectParser.PropertyGroupElementName, ProjectParser.MsbuildNamespace),
                        new XElement(
                            XName.Get(ProjectParser.PropertyTypeGuidsElementName, ProjectParser.MsbuildNamespace),
                            $"{Guid.Empty};{ProjectParser.WebApplicationGuid}"))));

            const string projectFilePath = @"c:\path\to\project.csproj";
            _projectMock.Setup(p => p.FullName).Returns(projectFilePath);
            _fileSystemMock.Setup(fs => fs.XDocument.Load(projectFilePath)).Returns(projectXDocument);

            IParsedDteProject result = ProjectParser.ParseProject(_projectMock.Object);

            Assert.IsInstanceOfType(result, typeof(Net4CsprojProject));
        }
    }
}
