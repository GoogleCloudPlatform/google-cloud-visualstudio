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
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.GCloud
{
    [TestClass]
    public class KubectlContextTests : ExtensionTestBase
    {
        private const string DefaultCluster = "default-cluster";
        private const string DefaultZone = "default-zone";
        private const string ExpectedCluster = "expected-cluster";
        private const string DefaultDeploymentName = "default-deployment-name";
        private const string DefaultServiceName = "default-service-name";
        private const string DefaultImageTag = "default-image-tag";
        private const int DefaultReplicas = 4;
        private const string ExpectedDeploymentName = "expected-deployment-name";
        private const string ExpectedServiceName = "expected-service-name";
        private const string ExpectedImageTag = "expected-image-tag";
        private const string ExpectedOutputLine = "expected line";
        private const int ExpectedReplicas = 20;
        private Mock<IProcessService> _processServiceMock;
        private KubectlContext _objectUnderTest;
        private string _kubeConfigPath;
        private Action<string> _mockedOutputAction;

        [TestInitialize]
        public async Task BeforeEachAsync()
        {
            _processServiceMock = new Mock<IProcessService>();
            PackageMock.Setup(p => p.ProcessService).Returns(_processServiceMock.Object);

            _kubeConfigPath = null;
            SetupRunCommandGetEnvironment(
                defaultInitKubectlEnvironment =>
                    _kubeConfigPath = _kubeConfigPath ??
                        defaultInitKubectlEnvironment[KubectlContext.KubeConfigVariable]);

            _objectUnderTest = await KubectlContext.GetForClusterAsync(DefaultCluster, DefaultZone);
            _mockedOutputAction = Mock.Of<Action<string>>();
        }

        [TestMethod]
        public async Task TestGetKubectlContextForClusterAsync_RunsGcloudContainerClustersGetCredentials()
        {
            await KubectlContext.GetForClusterAsync(DefaultCluster, DefaultZone);

            VerifyCommandArgsContain("gcloud container clusters get-credentials");
        }

        [TestMethod]
        public async Task TestGetKubectlContextForClusterAsync_RunsCommandAgainstExpectedCluster()
        {
            await KubectlContext.GetForClusterAsync(ExpectedCluster, DefaultZone);

            VerifyCommandArgsContain(ExpectedCluster);
        }

        [TestMethod]
        public async Task TestGetKubectlContextForClusterAsync_RunsCommandAgainstExpectedZone()
        {
            const string expectedZone = "expected-zone";

            await KubectlContext.GetForClusterAsync(DefaultCluster, expectedZone);

            VerifyCommandArgsContain($"--zone={expectedZone}");
        }

        [TestMethod]
        public async Task TestGetKubectlContextForClusterAsync_RunsCommandWithExpectedGoogleCredentialsEnvVar()
        {
            const string expectedCredentialsPath = "expected-credentials-path";
            CredentialStoreMock.Setup(cs => cs.CurrentAccountPath).Returns(expectedCredentialsPath);
            IDictionary<string, string> environment = new Dictionary<string, string>();
            SetupRunCommandGetEnvironment(commandEnvironment => environment = commandEnvironment);

            await KubectlContext.GetForClusterAsync(DefaultCluster, DefaultZone);

            Assert.AreEqual(expectedCredentialsPath, environment[KubectlContext.GoogleApplicationCredentialsVariable]);
        }

        [TestMethod]
        public async Task TestGetKubectlContextForClusterAsync_RunsCommandWithExpectedUseDefaultCredentialsEnvVar()
        {
            IDictionary<string, string> environment = new Dictionary<string, string>();
            SetupRunCommandGetEnvironment(commandEnvironment => environment = commandEnvironment);

            await KubectlContext.GetForClusterAsync(DefaultCluster, DefaultZone);

            Assert.AreEqual(
                KubectlContext.TrueValue,
                environment[KubectlContext.UseApplicationDefaultCredentialsVariable]);
        }

        [TestMethod]
        public async Task TestGetKubectlContextForClusterAsync_RunsCommandWithKubeConfigEnvVar()
        {
            IDictionary<string, string> environment = new Dictionary<string, string>();
            SetupRunCommandGetEnvironment(commandEnvironment => environment = commandEnvironment);

            await KubectlContext.GetForClusterAsync(DefaultCluster, DefaultZone);

            Assert.IsTrue(environment.ContainsKey(KubectlContext.KubeConfigVariable));
        }

        [TestMethod]
        public async Task TestGetKubectlContextForClusterAsync_ThrowsOnCommandFailure()
        {
            SetupRunCommandResult(false);

            GCloudException e = await Assert.ThrowsExceptionAsync<GCloudException>(
                () => KubectlContext.GetForClusterAsync(ExpectedCluster, DefaultZone));

            StringAssert.Contains(e.Message, ExpectedCluster);
        }

        [TestMethod]
        public async Task TestDispose_DeletesConfigPath()
        {
            IDictionary<string, string> environment = new Dictionary<string, string>();
            SetupRunCommandGetEnvironment(commandEnvironment => environment = commandEnvironment);

            _objectUnderTest = await KubectlContext.GetForClusterAsync(DefaultCluster, DefaultZone);
            _objectUnderTest.Dispose();

            string expectedDeletePath = environment[KubectlContext.KubeConfigVariable];
            PackageMock.Verify(p => p.GetMefService<IFileSystem>().File.Delete(expectedDeletePath));
        }

        [TestMethod]
        public async Task TestDispose_NonReentrant()
        {
            _objectUnderTest = await KubectlContext.GetForClusterAsync(DefaultCluster, DefaultZone);
            _objectUnderTest.Dispose();
            _objectUnderTest.Dispose();

            PackageMock.Verify(p => p.GetMefService<IFileSystem>().File.Delete(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_RunsKubctlRun()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            VerifyKubectlArgsContain("run");
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_TargetsPort8080()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            VerifyKubectlArgsContain("--port=8080");
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_RunsTargetsGivenDeploymentName()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                ExpectedDeploymentName,
                DefaultImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            VerifyKubectlArgsContain(ExpectedDeploymentName);
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_RunsTargetsGivenImageTag()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                ExpectedImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            VerifyKubectlArgsContain($"--image={ExpectedImageTag}");
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_RunsTargetsGivenReplicas()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                ExpectedReplicas,
                _mockedOutputAction);

            VerifyKubectlArgsContain($"--replicas={ExpectedReplicas}");
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_PassesHandler()
        {
            SetupRunKubectlInvokeHandler(ExpectedOutputLine);

            await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            Mock.Get(_mockedOutputAction).Verify(h => h(ExpectedOutputLine));
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_PassesKubeconfigParam()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            VerifyKubectlArgsContain($"--kubeconfig=\"{_kubeConfigPath}\"");
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestCreateDeploymentAsync_ReturnsResult(bool expectedResult)
        {
            SetupRunCommandResult(expectedResult);

            bool result = await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestExposeServiceAsync_RunsKubctlExposeDeployment()
        {
            await _objectUnderTest.ExposeServiceAsync(
                DefaultDeploymentName,
                false,
                _mockedOutputAction);

            VerifyKubectlArgsContain("expose deployment");
            VerifyKubectlArgsContain("--target-port=8080");
            VerifyKubectlArgsContain("--port=80");
        }

        [TestMethod]
        public async Task TestExposeServiceAsync_PassesHandler()
        {
            SetupRunKubectlInvokeHandler(ExpectedOutputLine);

            await _objectUnderTest.ExposeServiceAsync(DefaultDeploymentName, false, _mockedOutputAction);

            Mock.Get(_mockedOutputAction).Verify(h => h(ExpectedOutputLine));
        }

        [TestMethod]
        public async Task TestExposeServiceAsync_PassesKubeconfigParam()
        {
            await _objectUnderTest.ExposeServiceAsync(DefaultDeploymentName, false, _mockedOutputAction);

            VerifyKubectlArgsContain($"--kubeconfig=\"{_kubeConfigPath}\"");
        }

        [TestMethod]
        public async Task TestExposeServiceAsync_PassesDeploymentName()
        {
            await _objectUnderTest.ExposeServiceAsync(
                ExpectedDeploymentName,
                false,
                _mockedOutputAction);

            VerifyKubectlArgsContain(ExpectedDeploymentName);
        }

        [TestMethod]
        public async Task TestExposeServiceAsync_PassesLoadBalancerTypeForPublic()
        {
            await _objectUnderTest.ExposeServiceAsync(
                DefaultDeploymentName,
                true,
                _mockedOutputAction);

            VerifyKubectlArgsContain("--type=LoadBalancer");
        }

        [TestMethod]
        public async Task TestExposeServiceAsync_PassesClusterIPTypeForPrivate()
        {
            await _objectUnderTest.ExposeServiceAsync(
                DefaultDeploymentName,
                false,
                _mockedOutputAction);

            VerifyKubectlArgsContain("--type=ClusterIP");
        }

        [TestMethod]
        public async Task TestGetServicesAsync_PassesKubeconfig()
        {
            SetupGetJsonOutput(new GkeList<GkeService>());

            await _objectUnderTest.GetServicesAsync();

            VerifyGetJsonOutputArgsContain<GkeList<GkeService>>($"--kubeconfig=\"{_kubeConfigPath}\"");
        }

        [TestMethod]
        public async Task TestGetServicesAsync_GetsOutputFromCommand()
        {
            var expectedResult = Mock.Of<IList<GkeService>>();
            SetupGetJsonOutput(new GkeList<GkeService> { Items = expectedResult });

            IList<GkeService> result = await _objectUnderTest.GetServicesAsync();

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestGetServicesAsync_ExecutesKubectlGetServices()
        {
            SetupGetJsonOutput(new GkeList<GkeService>());

            await _objectUnderTest.GetServicesAsync();

            VerifyGetJsonOutputArgsContain<GkeList<GkeService>>("get services");
        }

        [TestMethod]
        public async Task TestGetServiceAsync_PassesKubeconfig()
        {

            await _objectUnderTest.GetServiceAsync(DefaultServiceName);

            VerifyGetJsonOutputArgsContain<GkeService>($"--kubeconfig=\"{_kubeConfigPath}\"");
        }

        [TestMethod]
        public async Task TestGetServiceAsync_GetsOutputFromCommand()
        {
            var expectedResult = new GkeService();
            SetupGetJsonOutput(expectedResult);

            GkeService result = await _objectUnderTest.GetServiceAsync(DefaultServiceName);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestGetServiceAsync_ExecutesKubectlGetService()
        {
            SetupGetJsonOutput(new GkeService());

            await _objectUnderTest.GetServiceAsync(DefaultServiceName);

            VerifyGetJsonOutputArgsContain<GkeService>("get service");
        }

        [TestMethod]
        public async Task TestGetServiceAsync_PassesGivenServiceName()
        {
            SetupGetJsonOutput(new GkeService());

            await _objectUnderTest.GetServiceAsync(ExpectedServiceName);

            VerifyGetJsonOutputArgsContain<GkeService>(ExpectedServiceName);
        }

        [TestMethod]
        public async Task TestGetDeploymentsAsync_PassesKubeconfig()
        {
            SetupGetJsonOutput(new GkeList<GkeDeployment>());

            await _objectUnderTest.GetDeploymentsAsync();

            VerifyGetJsonOutputArgsContain<GkeList<GkeDeployment>>($"--kubeconfig=\"{_kubeConfigPath}\"");
        }

        [TestMethod]
        public async Task TestGetDeploymentsAsync_GetsOutputFromCommand()
        {
            var expectedResult = new List<GkeDeployment> { new GkeDeployment() };
            SetupGetJsonOutput(new GkeList<GkeDeployment> { Items = expectedResult });

            IList<GkeDeployment> result = await _objectUnderTest.GetDeploymentsAsync();

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestGetDeploymentsAsync_ExecutesKubectlGetDeployments()
        {
            SetupGetJsonOutput(new GkeList<GkeDeployment>());

            await _objectUnderTest.GetDeploymentsAsync();

            VerifyGetJsonOutputArgsContain<GkeList<GkeDeployment>>("get deployments");
        }

        [TestMethod]
        public async Task TestDeploymentExistsAsync_PassesKubeconfig()
        {
            SetupGetJsonOutput(new GkeList<GkeDeployment> { Items = new List<GkeDeployment>() });

            await _objectUnderTest.DeploymentExistsAsync(DefaultDeploymentName);

            VerifyGetJsonOutputArgsContain<GkeList<GkeDeployment>>($"--kubeconfig=\"{_kubeConfigPath}\"");
        }

        [TestMethod]
        public async Task TestDeploymentExistsAsync_ReturnsTrueForExistingDeployment()
        {
            var expectedResult = new List<GkeDeployment>
            {
                new GkeDeployment {Metadata = new GkeMetadata {Name = ExpectedDeploymentName}}
            };
            SetupGetJsonOutput(new GkeList<GkeDeployment> { Items = expectedResult });

            bool result = await _objectUnderTest.DeploymentExistsAsync(ExpectedDeploymentName);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestDeploymentExistsAsync_ReturnsFalseForMissingDeployment()
        {
            var expectedResult = new List<GkeDeployment>
            {
                new GkeDeployment {Metadata = new GkeMetadata {Name = ExpectedDeploymentName}}
            };
            SetupGetJsonOutput(new GkeList<GkeDeployment> { Items = expectedResult });

            bool result = await _objectUnderTest.DeploymentExistsAsync(DefaultDeploymentName);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestDeploymentExistsAsync_ExecutesKubectlGetDeployments()
        {
            SetupGetJsonOutput(new GkeList<GkeDeployment> { Items = new List<GkeDeployment>() });

            await _objectUnderTest.DeploymentExistsAsync(DefaultDeploymentName);

            VerifyGetJsonOutputArgsContain<GkeList<GkeDeployment>>("get deployments");
        }

        [TestMethod]
        public async Task TestUpdateDeploymentImageAsync_RunsKubectlSetImage()
        {
            await _objectUnderTest.UpdateDeploymentImageAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                _mockedOutputAction);

            VerifyKubectlArgsContain("set image");
        }

        [TestMethod]
        public async Task TestUpdateDeploymentImageAsync_PassesKubeconfig()
        {
            await _objectUnderTest.UpdateDeploymentImageAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                _mockedOutputAction);

            VerifyKubectlArgsContain($"--kubeconfig=\"{_kubeConfigPath}\"");
        }

        [TestMethod]
        public async Task TestUpdateDeploymentImageAsync_PassesGivenDeployment()
        {
            await _objectUnderTest.UpdateDeploymentImageAsync(
                ExpectedDeploymentName,
                DefaultImageTag,
                _mockedOutputAction);

            VerifyKubectlArgsContain($"deployment/{ExpectedDeploymentName}");
        }

        [TestMethod]
        public async Task TestUpdateDeploymentImageAsync_PassesGivenDeploymentAsContainer()
        {
            await _objectUnderTest.UpdateDeploymentImageAsync(
                ExpectedDeploymentName,
                DefaultImageTag,
                _mockedOutputAction);

            VerifyKubectlArgsContain($"{ExpectedDeploymentName}=");
        }

        [TestMethod]
        public async Task TestUpdateDeploymentImageAsync_PassesGivenImageTag()
        {
            await _objectUnderTest.UpdateDeploymentImageAsync(
                DefaultDeploymentName,
                ExpectedImageTag,
                _mockedOutputAction);

            VerifyKubectlArgsContain($"={ExpectedImageTag}");
        }

        [TestMethod]
        public async Task TestUpdateDeploymentImageAsync_PassesHandler()
        {
            SetupRunKubectlInvokeHandler(ExpectedOutputLine);

            await _objectUnderTest.UpdateDeploymentImageAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                _mockedOutputAction);

            Mock.Get(_mockedOutputAction).Verify(h => h(ExpectedOutputLine));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestUpdateDeploymentImageAsync_ReturnsCommandResult(bool expectedResult)
        {
            SetupRunCommandResult(expectedResult);

            bool result = await _objectUnderTest.UpdateDeploymentImageAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                _mockedOutputAction);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestScaleDeploymentAsync_RunsKubectlScaleDeployment()
        {
            await _objectUnderTest.ScaleDeploymentAsync(
                DefaultDeploymentName,
                DefaultReplicas,
                _mockedOutputAction);

            VerifyKubectlArgsContain("scale deployment");
        }

        [TestMethod]
        public async Task TestScaleDeploymentAsync_PassesKubeconfig()
        {
            await _objectUnderTest.ScaleDeploymentAsync(
                DefaultDeploymentName,
                DefaultReplicas,
                _mockedOutputAction);

            VerifyKubectlArgsContain($"--kubeconfig=\"{_kubeConfigPath}\"");
        }

        [TestMethod]
        public async Task TestScaleDeploymentAsync_PassesGivenDeployment()
        {
            await _objectUnderTest.ScaleDeploymentAsync(
                ExpectedDeploymentName,
                DefaultReplicas,
                _mockedOutputAction);

            VerifyKubectlArgsContain(ExpectedDeploymentName);
        }

        [TestMethod]
        public async Task TestScaleDeploymentAsync_PassesGivenReplicas()
        {
            await _objectUnderTest.ScaleDeploymentAsync(
                DefaultDeploymentName,
                ExpectedReplicas,
                _mockedOutputAction);

            VerifyKubectlArgsContain($"--replicas={ExpectedReplicas}");
        }

        [TestMethod]
        public async Task TestScaleDeploymentAsync_PassesHandler()
        {
            SetupRunKubectlInvokeHandler(ExpectedOutputLine);

            await _objectUnderTest.ScaleDeploymentAsync(
                DefaultDeploymentName,
                DefaultReplicas,
                _mockedOutputAction);

            Mock.Get(_mockedOutputAction).Verify(h => h(ExpectedOutputLine));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestScaleDeploymentAsync_ReturnsCommandResult(bool expectedResult)
        {
            SetupRunCommandResult(expectedResult);

            bool result = await _objectUnderTest.ScaleDeploymentAsync(
                DefaultDeploymentName,
                DefaultReplicas,
                _mockedOutputAction);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestDeleteServiceAsync_RunsKubectlDeleteService()
        {
            await _objectUnderTest.DeleteServiceAsync(DefaultServiceName, _mockedOutputAction);

            VerifyKubectlArgsContain("delete service");
        }

        [TestMethod]
        public async Task TestDeleteServiceAsync_PassesKubeconfig()
        {
            await _objectUnderTest.DeleteServiceAsync(DefaultServiceName, _mockedOutputAction);

            VerifyKubectlArgsContain($"--kubeconfig=\"{_kubeConfigPath}\"");
        }

        [TestMethod]
        public async Task TestDeleteServiceAsync_PassesGivenService()
        {
            await _objectUnderTest.DeleteServiceAsync(ExpectedServiceName, _mockedOutputAction);

            VerifyKubectlArgsContain(ExpectedServiceName);
        }

        [TestMethod]
        public async Task TestDeleteServiceAsync_PassesHandler()
        {
            SetupRunKubectlInvokeHandler(ExpectedOutputLine);

            await _objectUnderTest.DeleteServiceAsync(DefaultServiceName, _mockedOutputAction);

            Mock.Get(_mockedOutputAction).Verify(h => h(ExpectedOutputLine));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestDeleteServiceAsync_ReturnsCommandResult(bool expectedResult)
        {
            SetupRunCommandResult(expectedResult);

            bool result = await _objectUnderTest.DeleteServiceAsync(DefaultServiceName, _mockedOutputAction);

            Assert.AreEqual(expectedResult, result);
        }

        private void VerifyGetJsonOutputArgsContain<T>(string expectedArg)
        {
            _processServiceMock.Verify(
                p => p.GetJsonOutputAsync<T>(
                    "kubectl",
                    It.Is<string>(s => s.Contains(expectedArg)),
                    It.IsAny<string>(),
                    It.IsAny<IDictionary<string, string>>()));
        }

        private void SetupGetJsonOutput<T>(T result)
        {
            _processServiceMock
                .Setup(
                    p => p.GetJsonOutputAsync<T>(
                        "kubectl",
                        It.IsAny<string>(),
                        null,
                        It.IsAny<Dictionary<string, string>>()))
                .Returns(Task.FromResult(result));
        }

        private void VerifyKubectlArgsContain(string expectedArg)
        {
            _processServiceMock.Verify(
                p => p.RunCommandAsync(
                    "kubectl",
                    It.Is<string>(s => s.Contains(expectedArg)),
                    It.IsAny<EventHandler<OutputHandlerEventArgs>>(),
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

        private void SetupRunCommandGetEnvironment(Action<IDictionary<string, string>> setEnv)
        {
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
                        IDictionary<string, string> env) => setEnv(env))
                .Returns(Task.FromResult(true));
        }

        private void SetupRunKubectlInvokeHandler(string expectedOutputLine)
        {
            var outputHandlerEventArgs = new OutputHandlerEventArgs(expectedOutputLine, OutputStream.StandardOutput);
            _processServiceMock
                .Setup(
                    p => p.RunCommandAsync(
                       "kubectl",
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
    }
}

