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

using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.GCloud
{
    [TestClass]
    public class GCloudContextUnitTests : ExtensionTestBase
    {
        private const string DefaultInstance = "default-instance";
        private const string DefaultZone = "default-zone";
        private const string DefaultUserName = "default-user-name";
        private const string ExpectedUserName = "expected-user-name";
        private const string DefaultAppYamlPath = "default-app-yaml-path";
        private const string DefaultVersion = "default-version";
        private const string DefaultImageTag = "default-image-tag";
        private const string DefaultContentsPath = "default-contents-path";
        private GCloudContext _objectUnderTest;
        private Mock<IProcessService> _processServiceMock;
        private Action<string> _mockedOutputAction;

        protected override void BeforeEach()
        {
            _processServiceMock = new Mock<IProcessService>();
            SetupRunCommandResult(true);
            PackageMock.Setup(p => p.ProcessService).Returns(_processServiceMock.Object);
            _objectUnderTest = new GCloudContext();
            _mockedOutputAction = Mock.Of<Action<string>>();
        }

        [TestMethod]
        public void TestConstructor_SetsCredentialsPath()
        {
            const string expectedCredentialsPath = "expected-credentials-path";
            PackageMock.SetupGet(p => p.CredentialsStore.CurrentAccountPath)
                .Returns(expectedCredentialsPath);

            var objectUnderTest = new GCloudContext();

            Assert.AreEqual(expectedCredentialsPath, objectUnderTest.CredentialsPath);
        }

        [TestMethod]
        public void TestConstructor_SetsProjectId()
        {
            const string expectedProjectId = "expected-project-id";
            PackageMock.SetupGet(p => p.CredentialsStore.CurrentProjectId).Returns(expectedProjectId);

            var objectUnderTest = new GCloudContext();

            Assert.AreEqual(expectedProjectId, objectUnderTest.ProjectId);
        }

        [TestMethod]
        public async Task TestResetWindowsCredentialsAsync_RunsGcloudComputeResetWindowsPassword()
        {
            await _objectUnderTest.ResetWindowsCredentialsAsync(DefaultInstance, DefaultZone, DefaultUserName);

            VerifyCommandOutputArgsContain<WindowsInstanceCredentials>("compute reset-windows-password");
        }

        [TestMethod]
        public async Task TestResetWindowsCredentialsAsync_PassesGivenInstance()
        {
            const string expectedInstance = "expected-instance";
            await _objectUnderTest.ResetWindowsCredentialsAsync(expectedInstance, DefaultZone, DefaultUserName);

            VerifyCommandOutputArgsContain<WindowsInstanceCredentials>(expectedInstance);
        }

        [TestMethod]
        public async Task TestResetWindowsCredentialsAsync_PassesGivenZone()
        {
            const string expectedZone = "expected-zone";
            await _objectUnderTest.ResetWindowsCredentialsAsync(DefaultInstance, expectedZone, DefaultUserName);

            VerifyCommandOutputArgsContain<WindowsInstanceCredentials>($"--zone={expectedZone}");
        }

        [TestMethod]
        public async Task TestResetWindowsCredentialsAsync_PassesGivenUserName()
        {
            await _objectUnderTest.ResetWindowsCredentialsAsync(DefaultInstance, DefaultZone, ExpectedUserName);

            VerifyCommandOutputArgsContain<WindowsInstanceCredentials>($"--user=\"{ExpectedUserName}\"");
        }

        [TestMethod]
        public async Task TestResetWindowsCredentialsAsync_PassesQuiteArg()
        {
            await _objectUnderTest.ResetWindowsCredentialsAsync(DefaultInstance, DefaultZone, DefaultUserName);

            VerifyCommandOutputArgsContain<WindowsInstanceCredentials>("--quiet");
        }

        [TestMethod]
        public async Task TestResetWindowsCredentialsAsync_PassesFormatArg()
        {
            await _objectUnderTest.ResetWindowsCredentialsAsync(DefaultInstance, DefaultZone, DefaultUserName);

            VerifyCommandOutputArgsContain<WindowsInstanceCredentials>("--format=json");
        }

        [TestMethod]
        public async Task TestResetWindowsCredentialsAsync_GetsOutputFromCommand()
        {
            var expectedResult = new WindowsInstanceCredentials(ExpectedUserName, "default-password");
            SetupGetJsonOutput(expectedResult);

            WindowsInstanceCredentials result =
                await _objectUnderTest.ResetWindowsCredentialsAsync(DefaultInstance, DefaultZone, DefaultUserName);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestDeployAppAsync_RunsGcloudAppDeploy()
        {
            await _objectUnderTest.DeployAppAsync(DefaultAppYamlPath, DefaultVersion, false, _mockedOutputAction);

            VerifyCommandArgsContain("app deploy");
        }

        [TestMethod]
        public async Task TestDeployAppAsync_PassesGivenAppYaml()
        {
            const string expectedAppYamlPath = "expected-app-yaml-path";
            await _objectUnderTest.DeployAppAsync(
                expectedAppYamlPath,
                DefaultVersion,
                false,
                _mockedOutputAction);

            VerifyCommandArgsContain($"\"{expectedAppYamlPath}\"");
        }

        [TestMethod]
        public async Task TestDeployAppAsync_PassesGivenVersion()
        {
            const string expectedVersion = "expected-version";
            await _objectUnderTest.DeployAppAsync(
                DefaultAppYamlPath,
                expectedVersion,
                false,
                _mockedOutputAction);

            VerifyCommandArgsContain($"--version={expectedVersion}");
        }

        [TestMethod]
        public async Task TestDeployAppAsync_PassesPromoteForPromoteTrue()
        {
            await _objectUnderTest.DeployAppAsync(
                DefaultAppYamlPath,
                DefaultVersion,
                true,
                _mockedOutputAction);

            VerifyCommandArgsContain("--promote");
        }

        [TestMethod]
        public async Task TestDeployAppAsync_PassesNoPromoteForPromoteFalse()
        {
            await _objectUnderTest.DeployAppAsync(
                DefaultAppYamlPath,
                DefaultVersion,
                false,
                _mockedOutputAction);

            VerifyCommandArgsContain("--no-promote");
        }

        [TestMethod]
        public async Task TestDeployAppAsync_PassesSkipStaging()
        {
            await _objectUnderTest.DeployAppAsync(
                DefaultAppYamlPath,
                DefaultVersion,
                false,
                _mockedOutputAction);

            VerifyCommandArgsContain("--skip-staging");
        }

        [TestMethod]
        public async Task TestDeployAppAsync_PassesQuiet()
        {
            await _objectUnderTest.DeployAppAsync(
                DefaultAppYamlPath,
                DefaultVersion,
                false,
                _mockedOutputAction);

            VerifyCommandArgsContain("--quiet");
        }

        [TestMethod]
        public async Task TestDeployAppAsync_PassesHandler()
        {
            const string expectedOutputLine = "expected-output-line";
            SetupRunCommandInvokeHandler(expectedOutputLine);
            await _objectUnderTest.DeployAppAsync(
                DefaultAppYamlPath,
                DefaultVersion,
                false,
                _mockedOutputAction);

            Mock.Get(_mockedOutputAction).Verify(f => f(expectedOutputLine));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestDeployAppAsync_ReturnsResultFromCommand(bool expectedResult)
        {
            SetupRunCommandResult(expectedResult);

            bool result = await _objectUnderTest.DeployAppAsync(
                DefaultAppYamlPath,
                DefaultVersion,
                false,
                _mockedOutputAction);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestBuildContainerAsync_RunsGcloudContainerBuildsSubmit()
        {
            await _objectUnderTest.BuildContainerAsync(DefaultImageTag, DefaultContentsPath, _mockedOutputAction);

            VerifyCommandArgsContain("container builds submit");
        }

        [TestMethod]
        public async Task TestBuildContainerAsync_PassesGivenImageTag()
        {
            const string expectedImageTag = "expected-image-tag";
            await _objectUnderTest.BuildContainerAsync(expectedImageTag, DefaultContentsPath, _mockedOutputAction);

            VerifyCommandArgsContain($"--tag=\"{expectedImageTag}\"");
        }

        [TestMethod]
        public async Task TestBuildContainerAsync_PassesGivenIContentPath()
        {
            const string expectedContentsPath = "expected-contents-path";
            await _objectUnderTest.BuildContainerAsync(DefaultImageTag, expectedContentsPath, _mockedOutputAction);

            VerifyCommandArgsContain($"\"{expectedContentsPath}\"");
        }

        [TestMethod]
        public async Task TestBuildContainerAsync_PassesHandler()
        {
            const string expectedOutputLine = "expected-output-line";
            SetupRunCommandInvokeHandler(expectedOutputLine);

            await _objectUnderTest.BuildContainerAsync(DefaultImageTag, DefaultContentsPath, _mockedOutputAction);

            Mock.Get(_mockedOutputAction).Verify(f => f(expectedOutputLine));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestBuildContainerAsync_ReturnsResultFromCommand(bool expectedResult)
        {
            SetupRunCommandResult(expectedResult);

            bool result = await _objectUnderTest.BuildContainerAsync(
                DefaultImageTag,
                DefaultContentsPath,
                _mockedOutputAction);

            Assert.AreEqual(expectedResult, result);
        }

        private void VerifyCommandOutputArgsContain<T>(string expectedArg)
        {
            _processServiceMock.Verify(
                p => p.GetJsonOutputAsync<T>(
                    "cmd.exe",
                    It.Is<string>(s => s.Contains(expectedArg)),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>()));
        }

        private void VerifyCommandArgsContain(string expectedArg)
        {
            _processServiceMock.Verify(
                p => p.RunCommandAsync(
                    "cmd.exe",
                    It.Is<string>(s => s.Contains(expectedArg)),
                    It.IsAny<EventHandler<OutputHandlerEventArgs>>(),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>()));
        }

        private void SetupRunCommandResult(bool result)
        {
            _processServiceMock
                .Setup(
                    p => p.RunCommandAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<EventHandler<OutputHandlerEventArgs>>(),
                        It.IsAny<string>(),
                        It.IsAny<IDictionary<string, string>>()))
                .Returns(Task.FromResult(result));
        }

        private void SetupGetJsonOutput<T>(T result)
        {
            _processServiceMock
                .Setup(
                    p => p.GetJsonOutputAsync<T>(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        null,
                        It.IsAny<Dictionary<string, string>>()))
                .Returns(Task.FromResult(result));
        }

        private void SetupRunCommandInvokeHandler(string expectedOutputLine)
        {
            var outputHandlerEventArgs = new OutputHandlerEventArgs(expectedOutputLine, OutputStream.StandardOutput);
            _processServiceMock
                .Setup(
                    p => p.RunCommandAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<EventHandler<OutputHandlerEventArgs>>(),
                        It.IsAny<string>(),
                        It.IsAny<IDictionary<string, string>>()))
                .Callback(
                    (
                        string file,
                        string args,
                        EventHandler<OutputHandlerEventArgs> handler,
                        string workingDir,
                        IDictionary<string, string> env) => handler.Invoke(null, outputHandlerEventArgs))
                .Returns(Task.FromResult(true));
        }
    }
}
