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

using System;
using System.IO;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Services.Configuration;
using GoogleCloudExtension.Services.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.Services.Configuration
{
    [TestClass]
    public class AppEngineConfigurationTests : ExtensionTestBase
    {
        private const string Service = "new-service";
        private AppEngineConfiguration _objectUnderTest;
        private Mock<IFileSystem> _fileSystemMock;
        private Mock<IParsedDteProject> _parsedProjectMock;

        protected override void BeforeEach()
        {
            _parsedProjectMock = new Mock<IParsedDteProject>();
            _fileSystemMock = new Mock<IFileSystem> { DefaultValue = DefaultValue.Mock };
            _objectUnderTest = new AppEngineConfiguration(_fileSystemMock.ToLazy());
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(AppEngineConfiguration.DefaultServiceName)]
        public void TestSaveServiceToAppYaml_GeneratesNewDefaultFile(string defaultServiceName)
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);
            _fileSystemMock.Setup(fs => fs.File.Exists(targetAppYaml)).Returns(false);

            _objectUnderTest.SaveServiceToAppYaml(_parsedProjectMock.Object, defaultServiceName);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(targetAppYaml, AppEngineConfiguration.AppYamlDefaultContent));
        }

        [TestMethod]
        public void TestSaveServiceToAppYaml_GeneratesNewServiceSpecificFile()
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);
            _fileSystemMock.Setup(fs => fs.File.Exists(targetAppYaml)).Returns(false);

            _objectUnderTest.SaveServiceToAppYaml(_parsedProjectMock.Object, Service);

            string exptectedContents = string.Format(
                AppEngineConfiguration.AppYamlServiceSpecificContentFormat, Service);
            _fileSystemMock.Verify(fs => fs.File.WriteAllText(targetAppYaml, exptectedContents));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(AppEngineConfiguration.DefaultServiceName)]
        public void TestSaveServiceToAppYaml_RemovesServiceFromExistingAppYaml(string defaultServiceName)
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            string existingContents = string.Format(
                AppEngineConfiguration.AppYamlServiceSpecificContentFormat, Service);

            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);
            _fileSystemMock.Setup(fs => fs.File.Exists(targetAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(targetAppYaml))
                .Returns(new StringReader(existingContents));
            var resultWriter = new StringWriter();
            _fileSystemMock.Setup(fs => fs.File.CreateText(targetAppYaml)).Returns(resultWriter);

            _objectUnderTest.SaveServiceToAppYaml(_parsedProjectMock.Object, defaultServiceName);

            Assert.AreEqual(
                resultWriter.ToString(),
                AppEngineConfiguration.AppYamlDefaultContent.Replace("\n", Environment.NewLine));
        }

        [TestMethod]
        public void Test1ArgGenerateAppYaml_GeneratesDefaultContent()
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);

            _objectUnderTest.GenerateAppYaml(_parsedProjectMock.Object);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(targetAppYaml, AppEngineConfiguration.AppYamlDefaultContent));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(AppEngineConfiguration.DefaultServiceName)]
        public void Test1ArgGenerateAppYaml_GeneratesDefaultContentForDefaultService(string defaultServiceName)
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);

            _objectUnderTest.GenerateAppYaml(_parsedProjectMock.Object, defaultServiceName);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(targetAppYaml, AppEngineConfiguration.AppYamlDefaultContent));
        }

        [TestMethod]
        public void Test1ArgGenerateAppYaml_GeneratesSpecificContentForNonDefaultService()
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);

            _objectUnderTest.GenerateAppYaml(_parsedProjectMock.Object, Service);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(
                    targetAppYaml,
                    string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, Service)));
        }

        [TestMethod]
        public void TestGetAppEngineService_GetsDefaultFromMissingAppYaml()
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);
            _fileSystemMock.Setup(fs => fs.File.Exists(targetAppYaml)).Returns(false);

            string service = _objectUnderTest.GetAppEngineService(_parsedProjectMock.Object);

            Assert.AreEqual(AppEngineConfiguration.DefaultServiceName, service);
        }

        [TestMethod]
        public void TestGetAppEngineService_GetsDefaultFromAppYamlMissingService()
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);
            _fileSystemMock.Setup(fs => fs.File.Exists(targetAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(targetAppYaml)).Returns(new StringReader(""));

            string service = _objectUnderTest.GetAppEngineService(_parsedProjectMock.Object);

            Assert.AreEqual(AppEngineConfiguration.DefaultServiceName, service);
        }

        [TestMethod]
        public void TestGetAppEngineService_GetsSpecificServiceFromAppYaml()
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);
            _fileSystemMock.Setup(fs => fs.File.Exists(targetAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(targetAppYaml)).Returns(new StringReader(
                string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, Service)));

            string service = _objectUnderTest.GetAppEngineService(_parsedProjectMock.Object);

            Assert.AreEqual(Service, service);
        }

        [TestMethod]
        public void TestGetAppEngineService_GetsSpecificServiceFromJsonLikeAppYaml()
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);
            _fileSystemMock.Setup(fs => fs.File.Exists(targetAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(targetAppYaml))
                .Returns(new StringReader($"{{\"service\": \"{Service}\"}}"));

            string service = _objectUnderTest.GetAppEngineService(_parsedProjectMock.Object);

            Assert.AreEqual(Service, service);
        }

        [TestMethod]
        public void TestGetAppEngineRuntime_GetsAspNetCoreFromMissingAppYaml()
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);
            _fileSystemMock.Setup(fs => fs.File.Exists(targetAppYaml)).Returns(false);

            string runtime = _objectUnderTest.GetAppEngineRuntime(_parsedProjectMock.Object);

            Assert.AreEqual(AppEngineConfiguration.AspNetCoreRuntime, runtime);
        }

        [TestMethod]
        public void TestGetAppEngineRuntime_GetsNullFromAppYamlMissingRuntime()
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);
            _fileSystemMock.Setup(fs => fs.File.Exists(targetAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(targetAppYaml)).Returns(new StringReader(""));

            string runtime = _objectUnderTest.GetAppEngineRuntime(_parsedProjectMock.Object);

            Assert.IsNull(runtime);
        }

        [TestMethod]
        public void TestGetAppEngineRuntime_GetsSpecificRuntimeFromAppYaml()
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);
            _fileSystemMock.Setup(fs => fs.File.Exists(targetAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(targetAppYaml)).Returns(new StringReader("runtime: new-runtime"));

            string runtime = _objectUnderTest.GetAppEngineRuntime(_parsedProjectMock.Object);

            Assert.AreEqual("new-runtime", runtime);
        }

        [TestMethod]
        public void TestGetAppEngineRuntime_GetsSpecificRuntimeFromJsonLikeAppYaml()
        {
            const string projectDirectory = @"c:\Project\Directory";
            const string targetAppYaml = @"c:\Project\Directory\app.yaml";
            _parsedProjectMock.Setup(p => p.DirectoryPath).Returns(projectDirectory);
            _fileSystemMock.Setup(fs => fs.File.Exists(targetAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(targetAppYaml))
                .Returns(new StringReader("{\"runtime\": \"new-runtime\"}"));

            string runtime = _objectUnderTest.GetAppEngineRuntime(_parsedProjectMock.Object);

            Assert.AreEqual("new-runtime", runtime);
        }
    }
}
