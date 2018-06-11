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

using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Services.Configuration;
using GoogleCloudExtension.Services.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.Services.Configuration
{
    [TestClass]
    public class AppEngineConfigurationTests : ExtensionTestBase
    {
        private const string NewService = "new-service";
        private const string ProjectDirectory = @"c:\Project\Directory";
        private const string ProjectAppYaml = @"c:\Project\Directory\app.yaml";
        private const string TargetDirectory = @"c:\Target\Directory";
        private const string TargetAppYaml = @"c:\Target\Directory\app.yaml";
        private const string NewRuntime = "new-runtime";
        private const string OldService = "old-service";

        private AppEngineConfiguration _objectUnderTest;
        private Mock<IFileSystem> _fileSystemMock;
        private IParsedDteProject _mockedParsedProject;

        protected override void BeforeEach()
        {
            _mockedParsedProject = Mock.Of<IParsedDteProject>(p => p.DirectoryPath == ProjectDirectory);
            _fileSystemMock = new Mock<IFileSystem> { DefaultValue = DefaultValue.Mock };
            _objectUnderTest = new AppEngineConfiguration(_fileSystemMock.ToLazy());
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(AppEngineConfiguration.DefaultServiceName)]
        public void TestSaveServiceToAppYaml_GeneratesNewDefaultFile(string defaultServiceName)
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(false);

            _objectUnderTest.SaveServiceToAppYaml(_mockedParsedProject, defaultServiceName);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(ProjectAppYaml, AppEngineConfiguration.AppYamlDefaultContent));
        }

        [TestMethod]
        public void TestSaveServiceToAppYaml_GeneratesNewServiceSpecificFile()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(false);

            _objectUnderTest.SaveServiceToAppYaml(_mockedParsedProject, NewService);

            string exptectedContents = string.Format(
                AppEngineConfiguration.AppYamlServiceSpecificContentFormat, NewService);
            _fileSystemMock.Verify(fs => fs.File.WriteAllText(ProjectAppYaml, exptectedContents));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(AppEngineConfiguration.DefaultServiceName)]
        public void TestSaveServiceToAppYaml_RemovesServiceFromExistingAppYaml(string defaultServiceName)
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml)).Returns(
                new StringReader(string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, NewService)));
            var resultsWriter = new StringWriter();
            _fileSystemMock.Setup(fs => fs.File.CreateText(ProjectAppYaml)).Returns(resultsWriter);

            _objectUnderTest.SaveServiceToAppYaml(_mockedParsedProject, defaultServiceName);

            Assert.AreEqual(
                resultsWriter.ToString(),
                AppEngineConfiguration.AppYamlDefaultContent.Replace("\n", Environment.NewLine));
        }

        [TestMethod]
        public void TestSaveServiceToAppYaml_AddsServiceToExistingAppYaml()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml))
                .Returns(new StringReader(AppEngineConfiguration.AppYamlDefaultContent));
            var resultsWriter = new StringWriter();
            _fileSystemMock.Setup(fs => fs.File.CreateText(ProjectAppYaml)).Returns(resultsWriter);

            _objectUnderTest.SaveServiceToAppYaml(_mockedParsedProject, NewService);

            Assert.AreEqual(
                resultsWriter.ToString(),
                string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, NewService)
                    .Replace("\n", Environment.NewLine));
        }

        [TestMethod]
        public void TestSaveServiceToAppYaml_UpdatesServiceInExistingAppYaml()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml))
                .Returns(new StringReader(
                    string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, OldService)));
            var resultsWriter = new StringWriter();
            _fileSystemMock.Setup(fs => fs.File.CreateText(ProjectAppYaml)).Returns(resultsWriter);

            _objectUnderTest.SaveServiceToAppYaml(_mockedParsedProject, NewService);

            Assert.AreEqual(
                resultsWriter.ToString(),
                string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, NewService)
                    .Replace("\n", Environment.NewLine));
        }

        [TestMethod]
        public void Test1ArgGenerateAppYaml_GeneratesDefaultContent()
        {
            _objectUnderTest.GenerateAppYaml(_mockedParsedProject);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(ProjectAppYaml, AppEngineConfiguration.AppYamlDefaultContent));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(AppEngineConfiguration.DefaultServiceName)]
        public void Test1ArgGenerateAppYaml_GeneratesDefaultContentForDefaultService(string defaultServiceName)
        {
            _objectUnderTest.GenerateAppYaml(_mockedParsedProject, defaultServiceName);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(ProjectAppYaml, AppEngineConfiguration.AppYamlDefaultContent));
        }

        [TestMethod]
        public void Test1ArgGenerateAppYaml_GeneratesSpecificContentForNonDefaultService()
        {
            _objectUnderTest.GenerateAppYaml(_mockedParsedProject, NewService);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(
                    ProjectAppYaml,
                    string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, NewService)));
        }

        [TestMethod]
        public void TestGetAppEngineService_GetsDefaultFromMissingAppYaml()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(false);

            string service = _objectUnderTest.GetAppEngineService(_mockedParsedProject);

            Assert.AreEqual(AppEngineConfiguration.DefaultServiceName, service);
        }

        [TestMethod]
        public void TestGetAppEngineService_GetsDefaultFromAppYamlMissingService()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml)).Returns(new StringReader(""));

            string service = _objectUnderTest.GetAppEngineService(_mockedParsedProject);

            Assert.AreEqual(AppEngineConfiguration.DefaultServiceName, service);
        }

        [TestMethod]
        public void TestGetAppEngineService_GetsSpecificServiceFromApp()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml)).Returns(new StringReader(
                string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, NewService)));

            string service = _objectUnderTest.GetAppEngineService(_mockedParsedProject);

            Assert.AreEqual(NewService, service);
        }

        [TestMethod]
        public void TestGetAppEngineService_GetsSpecificServiceFromJsonLikeAppYaml()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml))
                .Returns(new StringReader($"{{\"service\": \"{NewService}\"}}"));

            string service = _objectUnderTest.GetAppEngineService(_mockedParsedProject);

            Assert.AreEqual(NewService, service);
        }

        [TestMethod]
        public void TestGetAppEngineRuntime_GetsAspNetCoreFromMissingAppYaml()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(false);

            string runtime = _objectUnderTest.GetAppEngineRuntime(_mockedParsedProject);

            Assert.AreEqual(AppEngineConfiguration.AspNetCoreRuntime, runtime);
        }

        [TestMethod]
        public void TestGetAppEngineRuntime_GetsNullFromAppYamlMissingRuntime()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml)).Returns(new StringReader(""));

            string runtime = _objectUnderTest.GetAppEngineRuntime(_mockedParsedProject);

            Assert.IsNull(runtime);
        }

        [TestMethod]
        public void TestGetAppEngineRuntime_GetsSpecificRuntimeFromAppYaml()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml))
                .Returns(new StringReader($"runtime: {NewRuntime}"));

            string runtime = _objectUnderTest.GetAppEngineRuntime(_mockedParsedProject);

            Assert.AreEqual(NewRuntime, runtime);
        }

        [TestMethod]
        public void TestGetAppEngineRuntime_GetsSpecificRuntimeFromJsonLikeAppYaml()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml))
                .Returns(new StringReader($"{{\"runtime\": \"{NewRuntime}\"}}"));

            string runtime = _objectUnderTest.GetAppEngineRuntime(_mockedParsedProject);

            Assert.AreEqual(NewRuntime, runtime);
        }

        [TestMethod]
        public void TestCheckProjectConfiguration_AppYamlExists()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);

            ProjectConfigurationStatus configStatus = _objectUnderTest.CheckProjectConfiguration(_mockedParsedProject);

            Assert.IsTrue(configStatus.HasAppYaml);
        }

        [TestMethod]
        public void TestCheckProjectConfiguration_AppYamlMissing()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(false);

            ProjectConfigurationStatus configStatus = _objectUnderTest.CheckProjectConfiguration(_mockedParsedProject);

            Assert.IsFalse(configStatus.HasAppYaml);
        }

        [TestMethod]
        public void TestCopyOrCreateAppYaml_CopiesExistingWithCorrectService()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml))
                .Returns(new StringReader($"{{\"service\": \"{NewService}\"}}"));

            _objectUnderTest.CopyOrCreateAppYaml(_mockedParsedProject, TargetDirectory, NewService);

            _fileSystemMock.Verify(fs => fs.File.Copy(ProjectAppYaml, TargetAppYaml, true));
        }

        [TestMethod]
        public void TestCopyOrCreateAppYaml_CopiesExistingWithDefaultService()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml))
                .Returns(new StringReader(""));

            _objectUnderTest.CopyOrCreateAppYaml(
                _mockedParsedProject, TargetDirectory, AppEngineConfiguration.DefaultServiceName);

            _fileSystemMock.Verify(fs => fs.File.Copy(ProjectAppYaml, TargetAppYaml, true));
        }

        [TestMethod]
        public void TestCopyOrCreateAppYaml_WritesUpdatedWithDefaultService()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml)).Returns(
                () => new StringReader(
                    string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, NewService)));
            var resultsWriter = new StringWriter();
            _fileSystemMock.Setup(fs => fs.File.CreateText(TargetAppYaml)).Returns(resultsWriter);

            _objectUnderTest.CopyOrCreateAppYaml(
                _mockedParsedProject, TargetDirectory, AppEngineConfiguration.DefaultServiceName);

            Assert.AreEqual(
                resultsWriter.ToString(),
                AppEngineConfiguration.AppYamlDefaultContent.Replace("\n", Environment.NewLine));
        }

        [TestMethod]
        public void TestCopyOrCreateAppYaml_WritesUpdatedWithService()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml))
                .Returns(() => new StringReader(AppEngineConfiguration.AppYamlDefaultContent));
            var resultsWriter = new StringWriter();
            _fileSystemMock.Setup(fs => fs.File.CreateText(TargetAppYaml)).Returns(resultsWriter);

            _objectUnderTest.CopyOrCreateAppYaml(_mockedParsedProject, TargetDirectory, NewService);

            Assert.AreEqual(
                resultsWriter.ToString(),
                string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, NewService)
                    .Replace("\n", Environment.NewLine));
        }

        [TestMethod]
        public void TestCopyOrCreateAppYaml_WritesUpdatedWithNewService()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(true);
            _fileSystemMock.Setup(fs => fs.File.OpenText(ProjectAppYaml))
                .Returns(() => new StringReader(
                    string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, OldService)));
            var resultsWriter = new StringWriter();
            _fileSystemMock.Setup(fs => fs.File.CreateText(TargetAppYaml)).Returns(resultsWriter);

            _objectUnderTest.CopyOrCreateAppYaml(_mockedParsedProject, TargetDirectory, NewService);

            Assert.AreEqual(
                resultsWriter.ToString(),
                string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, NewService)
                    .Replace("\n", Environment.NewLine));
        }

        [TestMethod]
        public void TestCopyOrCreateAppYaml_GeneratesNewDefault()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(false);

            _objectUnderTest.CopyOrCreateAppYaml(
                _mockedParsedProject, TargetDirectory, AppEngineConfiguration.DefaultServiceName);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(TargetAppYaml, AppEngineConfiguration.AppYamlDefaultContent));
        }

        [TestMethod]
        public void TestCopyOrCreateAppYaml_GeneratesNewServiceSpecific()
        {
            _fileSystemMock.Setup(fs => fs.File.Exists(ProjectAppYaml)).Returns(false);

            _objectUnderTest.CopyOrCreateAppYaml(
                _mockedParsedProject, TargetDirectory, NewService);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(
                    TargetAppYaml,
                    string.Format(AppEngineConfiguration.AppYamlServiceSpecificContentFormat, NewService)));
        }
    }
}
