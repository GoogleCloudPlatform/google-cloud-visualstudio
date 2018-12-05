﻿// Copyright 2018 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.VsVersion;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.Deployment
{
    [TestClass]
    public class NetCoreAppUtilsTests : ExtensionTestBase
    {
        private const string DefaultConfiguration = "Release";
        private const string DefaultDirectory = "default-directory";
        private const string DefaultDotnetPath = "default-dotnet-path";
        private const string ExpectedProjectDirectory = "expected-project-directory";
        private const string ExpectedDotnetPath = "expected-dotnet-path";
        private const string ExpectedStagingDirectoryPath = "expected-staging-directory-path";

        private NetCoreAppUtils _objectUnderTest;
        private Mock<IProcessService> _processServiceMock;
        private Mock<IFileSystem> _fileSystemMock;
        private Mock<IToolsPathProvider> _toolsPathProviderMock;
        private Mock<IGCloudWrapper> _gcloudWrapperMock;
        private Mock<IEnvironment> _environmentMock;
        private readonly Func<string, OutputStream, Task> _defaultOutputAction = (s, os) => Task.CompletedTask;

        [TestInitialize]
        public void BeforeEach()
        {
            _toolsPathProviderMock = new Mock<IToolsPathProvider>();
            _toolsPathProviderMock.Setup(tpp => tpp.GetDotnetPath()).Returns(DefaultDotnetPath);
            VsVersionUtils.s_toolsPathProviderOverride = _toolsPathProviderMock.Object;

            _processServiceMock = new Mock<IProcessService>();
            _fileSystemMock = new Mock<IFileSystem> { DefaultValueProvider = DefaultValueProvider.Mock };
            _gcloudWrapperMock = new Mock<IGCloudWrapper>();
            _gcloudWrapperMock.Setup(w => w.GenerateSourceContextAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _environmentMock = new Mock<IEnvironment>();

            _objectUnderTest = new NetCoreAppUtils(
                _processServiceMock.ToLazy(),
                _fileSystemMock.ToLazy(),
                _gcloudWrapperMock.ToLazy(),
                _environmentMock.ToLazy());
        }

        [TestCleanup]
        public void AfterEach() => VsVersionUtils.s_toolsPathProviderOverride = null;

        [TestMethod]
        public async Task TestCreateAppBundleAsync_CreatesTargetStageDirectory()
        {
            await _objectUnderTest.CreateAppBundleAsync(
                Mock.Of<IParsedProject>(),
                ExpectedStagingDirectoryPath,
                _defaultOutputAction,
                DefaultConfiguration);

            _fileSystemMock.Verify(fs => fs.Directory.CreateDirectory(ExpectedStagingDirectoryPath));
        }

        [TestMethod]
        public async Task TestCreateAppBundleAsync_RunsDotnetFromToolsPathProvider()
        {
            _toolsPathProviderMock.Setup(tpp => tpp.GetDotnetPath()).Returns(ExpectedDotnetPath);

            await _objectUnderTest.CreateAppBundleAsync(
                Mock.Of<IParsedProject>(),
                DefaultDirectory,
                _defaultOutputAction,
                DefaultConfiguration);

            _processServiceMock.Verify(
                p => p.RunCommandAsync(
                    ExpectedDotnetPath,
                    It.IsAny<string>(),
                    It.IsAny<Func<string, OutputStream, Task>>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>()));
        }

        [TestMethod]
        public async Task TestCreateAppBundleAsync_RunsDotnetPublish()
        {
            await _objectUnderTest.CreateAppBundleAsync(
                Mock.Of<IParsedProject>(),
                "expected-stage-directory",
                _defaultOutputAction,
                "expected-configuration");

            _processServiceMock.Verify(
                p => p.RunCommandAsync(
                    It.IsAny<string>(),
                    "publish -o \"expected-stage-directory\" -c expected-configuration",
                    It.IsAny<Func<string, OutputStream, Task>>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>()));
        }

        [TestMethod]
        public async Task TestCreateAppBundleAsync_PassesOutputActionToDotnetCommand()
        {
            var mockedOutputAction = Mock.Of<Func<string, OutputStream, Task>>(
                f => f(It.IsAny<string>(), It.IsAny<OutputStream>()) == Task.CompletedTask);
            const string expectedOutputLine = "expected-output-line";
            SetupRunDotnetWithOutputLine(expectedOutputLine);

            await _objectUnderTest.CreateAppBundleAsync(
                Mock.Of<IParsedProject>(),
                DefaultDirectory,
                mockedOutputAction,
                DefaultConfiguration);

            Mock.Get(mockedOutputAction).Verify(f => f(expectedOutputLine, It.IsAny<OutputStream>()));
        }

        [TestMethod]
        public async Task TestCreateAppBundleAsync_PassesExternalToolsAndPathEnvToDotnetCommand()
        {
            const string expectedExternalTools = "expected-external-tools";
            _toolsPathProviderMock.Setup(tpp => tpp.GetExternalToolsPath()).Returns(expectedExternalTools);
            const string expectPathVar = "expected-path-var";
            _environmentMock.Setup(e => e.GetEnvironmentVariable("PATH")).Returns(expectPathVar);

            await _objectUnderTest.CreateAppBundleAsync(
                Mock.Of<IParsedProject>(),
                DefaultDirectory,
                _defaultOutputAction,
                DefaultConfiguration);

            _processServiceMock.Verify(
                p => p.RunCommandAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<string, OutputStream, Task>>(),
                    It.IsAny<string>(),
                    It.Is<IDictionary<string, string>>(d => d["PATH"] == "expected-path-var;expected-external-tools")));
        }

        [TestMethod]
        public async Task TestCreateAppBundleAsync_SetsProjectDirectoryToDotnetCommandWorkingDir()
        {
            await _objectUnderTest.CreateAppBundleAsync(
                Mock.Of<IParsedProject>(p => p.DirectoryPath == ExpectedProjectDirectory),
                DefaultDirectory,
                _defaultOutputAction,
                DefaultConfiguration);

            _processServiceMock.Verify(
                p => p.RunCommandAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<string, OutputStream, Task>>(),
                    ExpectedProjectDirectory,
                    It.IsAny<IDictionary<string, string>>()));
        }

        [TestMethod]
        public async Task TestCreateAppBundleAsync_GeneratesSourceContext()
        {
            await _objectUnderTest.CreateAppBundleAsync(
                Mock.Of<IParsedProject>(p => p.DirectoryPath == ExpectedProjectDirectory),
                ExpectedStagingDirectoryPath,
                _defaultOutputAction,
                DefaultConfiguration);

            _gcloudWrapperMock.Verify(
                w => w.GenerateSourceContextAsync(ExpectedProjectDirectory, ExpectedStagingDirectoryPath));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestCreateAppBundleAsync_ReturnsDotnetCommandResult(bool commandResult)
        {

            _processServiceMock.Setup(
                p => p.RunCommandAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<string, OutputStream, Task>>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>())).Returns(Task.FromResult(commandResult));

            bool result = await _objectUnderTest.CreateAppBundleAsync(
                Mock.Of<IParsedProject>(p => p.DirectoryPath == ExpectedProjectDirectory),
                DefaultDirectory,
                _defaultOutputAction,
                DefaultConfiguration);

            Assert.AreEqual(result, commandResult);
        }

        private void SetupRunDotnetWithOutputLine(string outputLine)
        {

            _processServiceMock.Setup(
                    p => p.RunCommandAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<Func<string, OutputStream, Task>>(),
                        It.IsAny<string>(),
                        It.IsAny<IDictionary<string, string>>()))
                .Callback<string, string, Func<string, OutputStream, Task>, string, IDictionary<string, string>>(
                    OutputLineToHandler)
                .Returns(Task.FromResult(true));

            void OutputLineToHandler(
                string file,
                string args,
                Func<string, OutputStream, Task> handler,
                string workingDir,
                IDictionary<string, string> env) => handler(outputLine, OutputStream.None);
        }
    }
}
