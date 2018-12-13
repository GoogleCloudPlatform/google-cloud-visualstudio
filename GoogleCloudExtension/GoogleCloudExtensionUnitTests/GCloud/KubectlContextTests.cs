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

using Google.Apis.Container.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.GCloud.Models;
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.GCloud
{
    [TestClass]
    public class KubectlContextTests : ExtensionTestBase
    {
        private const string DefaultClusterName = "default-cluster";
        private const string DefaultLocation = "default-zone";
        private const string DefaultDeploymentName = "default-deployment-name";
        private const string DefaultServiceName = "default-service-name";
        private const string DefaultImageTag = "default-image-tag";
        private const int DefaultReplicas = 4;
        private const string ExpectedDeploymentName = "expected-deployment-name";
        private const string ExpectedServiceName = "expected-service-name";
        private const string ExpectedImageTag = "expected-image-tag";
        private const int ExpectedReplicas = 20;
        private Mock<IProcessService> _processServiceMock;
        private KubectlContext _objectUnderTest;
        private string _kubeConfigPath;
        private Func<string, Task> _mockedOutputAction;

        private static readonly Cluster s_defaultCluster = new Cluster
        {
            Name = DefaultClusterName,
            Location = DefaultLocation,
            Locations = new[] { DefaultLocation }
        };

        private Mock<IFileSystem> _fileSystemMock;

        [TestInitialize]
        public async Task BeforeEachAsync()
        {
            _processServiceMock = new Mock<IProcessService>();
            PackageMock.Setup(p => p.ProcessService).Returns(_processServiceMock.Object);
            _fileSystemMock = new Mock<IFileSystem> { DefaultValueProvider = DefaultValueProvider.Mock };

            _kubeConfigPath = Path.GetTempFileName();
            _fileSystemMock.Setup(fs => fs.Path.GetTempFileName()).Returns(_kubeConfigPath);
            _processServiceMock.SetupRunCommandAsync().ReturnsResult(true);

            _objectUnderTest = new KubectlContext(
                _fileSystemMock.Object,
                _processServiceMock.ToLazy(),
                CredentialsStore.Default);
            await _objectUnderTest.InitClusterCredentialsAsync(s_defaultCluster);
            _mockedOutputAction = Mock.Of<Func<string, Task>>();
        }

        [TestMethod]
        public void TestDispose_DeletesConfigPath()
        {
            _objectUnderTest.Dispose();

            _fileSystemMock.Verify(fs => fs.File.Delete(_kubeConfigPath));
        }

        [TestMethod]
        public void TestDispose_NonReEntrant()
        {
            _objectUnderTest.Dispose();
            _objectUnderTest.Dispose();

            _fileSystemMock.Verify(fs => fs.File.Delete(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_RunsKubectlRun()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs("kubectl", s => s.Contains("run"));
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_TargetsPort8080()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains("--port=8080"));
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_RunsTargetsGivenDeploymentName()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                ExpectedDeploymentName,
                DefaultImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains(ExpectedDeploymentName));
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_RunsTargetsGivenImageTag()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                ExpectedImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains($"--image={ExpectedImageTag}"));
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_RunsTargetsGivenReplicas()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                ExpectedReplicas,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains($"--replicas={ExpectedReplicas}"));
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_PassesHandler()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncHandler(_mockedOutputAction);
        }

        [TestMethod]
        public async Task TestCreateDeploymentAsync_PassesKubeconfigParam()
        {
            await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains($"--kubeconfig=\"{_kubeConfigPath}\""));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestCreateDeploymentAsync_ReturnsResult(bool expectedResult)
        {
            _processServiceMock.SetupRunCommandAsync().ReturnsResult(expectedResult);

            bool result = await _objectUnderTest.CreateDeploymentAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                DefaultReplicas,
                _mockedOutputAction);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestExposeServiceAsync_RunsKubectlExposeDeployment()
        {
            await _objectUnderTest.ExposeServiceAsync(
                DefaultDeploymentName,
                false,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs("kubectl", s => s.Contains("expose deployment"));
            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains("--target-port=8080"));
            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains("--port=80"));
        }

        [TestMethod]
        public async Task TestExposeServiceAsync_PassesHandler()
        {
            await _objectUnderTest.ExposeServiceAsync(DefaultDeploymentName, false, _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncHandler(_mockedOutputAction);
        }

        [TestMethod]
        public async Task TestExposeServiceAsync_PassesKubeconfigParam()
        {
            await _objectUnderTest.ExposeServiceAsync(DefaultDeploymentName, false, _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains($"--kubeconfig=\"{_kubeConfigPath}\""));
        }

        [TestMethod]
        public async Task TestExposeServiceAsync_PassesDeploymentName()
        {
            await _objectUnderTest.ExposeServiceAsync(
                ExpectedDeploymentName,
                false,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains(ExpectedDeploymentName));
        }

        [TestMethod]
        public async Task TestExposeServiceAsync_PassesLoadBalancerTypeForPublic()
        {
            await _objectUnderTest.ExposeServiceAsync(
                DefaultDeploymentName,
                true,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains("--type=LoadBalancer"));
        }

        [TestMethod]
        public async Task TestExposeServiceAsync_PassesClusterIPTypeForPrivate()
        {
            await _objectUnderTest.ExposeServiceAsync(
                DefaultDeploymentName,
                false,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains("--type=ClusterIP"));
        }

        [TestMethod]
        public async Task TestGetServicesAsync_PassesKubeconfig()
        {
            _processServiceMock.SetupGetJsonOutput<GkeList<GkeService>>()
                .ReturnsResult(new GkeList<GkeService>());

            await _objectUnderTest.GetServicesAsync();

            _processServiceMock.VerifyGetJsonOutputAsyncArgs<GkeList<GkeService>>(
                s => s.Contains($"--kubeconfig=\"{_kubeConfigPath}\""));
        }

        [TestMethod]
        public async Task TestGetServicesAsync_GetsOutputFromCommand()
        {
            IList<GkeService> expectedResult = Mock.Of<IList<GkeService>>();
            _processServiceMock.SetupGetJsonOutput<GkeList<GkeService>>()
                .ReturnsResult(new GkeList<GkeService> { Items = expectedResult });

            IList<GkeService> result = await _objectUnderTest.GetServicesAsync();

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestGetServicesAsync_ExecutesKubectlGetServices()
        {
            _processServiceMock.SetupGetJsonOutput<GkeList<GkeService>>()
                .ReturnsResult(new GkeList<GkeService>());

            await _objectUnderTest.GetServicesAsync();

            _processServiceMock.VerifyGetJsonOutputAsyncArgs<GkeList<GkeService>>(
                "kubectl",
                s => s.Contains("get services"));
        }

        [TestMethod]
        public async Task TestGetServiceAsync_PassesKubeconfig()
        {

            await _objectUnderTest.GetServiceAsync(DefaultServiceName);

            _processServiceMock.VerifyGetJsonOutputAsyncArgs<GkeService>(
                s => s.Contains($"--kubeconfig=\"{_kubeConfigPath}\""));
        }

        [TestMethod]
        public async Task TestGetServiceAsync_GetsOutputFromCommand()
        {
            var expectedResult = new GkeService();
            _processServiceMock.SetupGetJsonOutput<GkeService>().ReturnsResult(expectedResult);

            GkeService result = await _objectUnderTest.GetServiceAsync(DefaultServiceName);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestGetServiceAsync_ExecutesKubectlGetService()
        {
            _processServiceMock.SetupGetJsonOutput<GkeService>().ReturnsResult(new GkeService());

            await _objectUnderTest.GetServiceAsync(DefaultServiceName);

            _processServiceMock.VerifyGetJsonOutputAsyncArgs<GkeService>("kubectl", s => s.Contains("get service"));
        }

        [TestMethod]
        public async Task TestGetServiceAsync_PassesGivenServiceName()
        {
            _processServiceMock.SetupGetJsonOutput<GkeService>().ReturnsResult(new GkeService());

            await _objectUnderTest.GetServiceAsync(ExpectedServiceName);

            _processServiceMock.VerifyGetJsonOutputAsyncArgs<GkeService>(s => s.Contains(ExpectedServiceName));
        }

        [TestMethod]
        public async Task TestGetServiceAsync_PassesIgnoreNotFound()
        {
            _processServiceMock.SetupGetJsonOutput<GkeService>().ReturnsResult(new GkeService());

            await _objectUnderTest.GetServiceAsync(DefaultServiceName);

            _processServiceMock.VerifyGetJsonOutputAsyncArgs<GkeService>(s => s.Contains("--ignore-not-found"));
        }

        [TestMethod]
        public async Task TestGetDeploymentsAsync_PassesKubeconfig()
        {
            _processServiceMock.SetupGetJsonOutput<GkeList<GkeDeployment>>()
                .ReturnsResult(new GkeList<GkeDeployment>());

            await _objectUnderTest.GetDeploymentsAsync();

            _processServiceMock.VerifyGetJsonOutputAsyncArgs<GkeList<GkeDeployment>>(s => s.Contains($"--kubeconfig=\"{_kubeConfigPath}\""));
        }

        [TestMethod]
        public async Task TestGetDeploymentsAsync_GetsOutputFromCommand()
        {
            var expectedResult = new List<GkeDeployment> { new GkeDeployment() };
            _processServiceMock.SetupGetJsonOutput<GkeList<GkeDeployment>>()
                .ReturnsResult(new GkeList<GkeDeployment> { Items = expectedResult });

            IList<GkeDeployment> result = await _objectUnderTest.GetDeploymentsAsync();

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestGetDeploymentsAsync_ExecutesKubectlGetDeployments()
        {
            _processServiceMock.SetupGetJsonOutput<GkeList<GkeDeployment>>()
                .ReturnsResult(new GkeList<GkeDeployment>());

            await _objectUnderTest.GetDeploymentsAsync();

            _processServiceMock.VerifyGetJsonOutputAsyncArgs<GkeList<GkeDeployment>>(
                "kubectl",
                s => s.Contains("get deployments"));
        }

        [TestMethod]
        public async Task TestDeploymentExistsAsync_PassesKubeconfig()
        {
            _processServiceMock.SetupGetJsonOutput<GkeList<GkeDeployment>>()
                .ReturnsResult(new GkeList<GkeDeployment> { Items = new List<GkeDeployment>() });

            await _objectUnderTest.DeploymentExistsAsync(DefaultDeploymentName);

            _processServiceMock.VerifyGetJsonOutputAsyncArgs<GkeList<GkeDeployment>>(
                s => s.Contains($"--kubeconfig=\"{_kubeConfigPath}\""));
        }

        [TestMethod]
        public async Task TestDeploymentExistsAsync_ReturnsTrueForExistingDeployment()
        {
            var expectedResult = new List<GkeDeployment>
            {
                new GkeDeployment { Metadata = new GkeMetadata { Name = ExpectedDeploymentName } }
            };
            _processServiceMock.SetupGetJsonOutput<GkeList<GkeDeployment>>()
                .ReturnsResult(new GkeList<GkeDeployment> { Items = expectedResult });

            bool result = await _objectUnderTest.DeploymentExistsAsync(ExpectedDeploymentName);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task TestDeploymentExistsAsync_ReturnsFalseForMissingDeployment()
        {
            var expectedResult = new List<GkeDeployment>
            {
                new GkeDeployment { Metadata = new GkeMetadata { Name = ExpectedDeploymentName } }
            };
            _processServiceMock.SetupGetJsonOutput<GkeList<GkeDeployment>>()
                .ReturnsResult(new GkeList<GkeDeployment> { Items = expectedResult });

            bool result = await _objectUnderTest.DeploymentExistsAsync(DefaultDeploymentName);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestDeploymentExistsAsync_ExecutesKubectlGetDeployments()
        {
            _processServiceMock.SetupGetJsonOutput<GkeList<GkeDeployment>>()
                .ReturnsResult(new GkeList<GkeDeployment> { Items = new List<GkeDeployment>() });

            await _objectUnderTest.DeploymentExistsAsync(DefaultDeploymentName);

            _processServiceMock.VerifyGetJsonOutputAsyncArgs<GkeList<GkeDeployment>>(
                "kubectl",
                s => s.Contains("get deployments"));
        }

        [TestMethod]
        public async Task TestUpdateDeploymentImageAsync_RunsKubectlSetImage()
        {
            await _objectUnderTest.UpdateDeploymentImageAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs("kubectl", s => s.Contains("set image"));
        }

        [TestMethod]
        public async Task TestUpdateDeploymentImageAsync_PassesKubeconfig()
        {
            await _objectUnderTest.UpdateDeploymentImageAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains($"--kubeconfig=\"{_kubeConfigPath}\""));
        }

        [TestMethod]
        public async Task TestUpdateDeploymentImageAsync_PassesGivenDeployment()
        {
            await _objectUnderTest.UpdateDeploymentImageAsync(
                ExpectedDeploymentName,
                DefaultImageTag,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains($"deployment/{ExpectedDeploymentName}"));
        }

        [TestMethod]
        public async Task TestUpdateDeploymentImageAsync_PassesGivenDeploymentAsContainer()
        {
            await _objectUnderTest.UpdateDeploymentImageAsync(
                ExpectedDeploymentName,
                DefaultImageTag,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains($"{ExpectedDeploymentName}="));
        }

        [TestMethod]
        public async Task TestUpdateDeploymentImageAsync_PassesGivenImageTag()
        {
            await _objectUnderTest.UpdateDeploymentImageAsync(
                DefaultDeploymentName,
                ExpectedImageTag,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains($"={ExpectedImageTag}"));
        }

        [TestMethod]
        public async Task TestUpdateDeploymentImageAsync_PassesHandler()
        {
            await _objectUnderTest.UpdateDeploymentImageAsync(
                DefaultDeploymentName,
                DefaultImageTag,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncHandler(_mockedOutputAction);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestUpdateDeploymentImageAsync_ReturnsCommandResult(bool expectedResult)
        {
            _processServiceMock.SetupRunCommandAsync().ReturnsResult(expectedResult);

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

            _processServiceMock.VerifyRunCommandAsyncArgs("kubectl", s => s.Contains("scale deployment"));
        }

        [TestMethod]
        public async Task TestScaleDeploymentAsync_PassesKubeconfig()
        {
            await _objectUnderTest.ScaleDeploymentAsync(
                DefaultDeploymentName,
                DefaultReplicas,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains($"--kubeconfig=\"{_kubeConfigPath}\""));
        }

        [TestMethod]
        public async Task TestScaleDeploymentAsync_PassesGivenDeployment()
        {
            await _objectUnderTest.ScaleDeploymentAsync(
                ExpectedDeploymentName,
                DefaultReplicas,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains(ExpectedDeploymentName));
        }

        [TestMethod]
        public async Task TestScaleDeploymentAsync_PassesGivenReplicas()
        {
            await _objectUnderTest.ScaleDeploymentAsync(
                DefaultDeploymentName,
                ExpectedReplicas,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains($"--replicas={ExpectedReplicas}"));
        }

        [TestMethod]
        public async Task TestScaleDeploymentAsync_PassesHandler()
        {
            await _objectUnderTest.ScaleDeploymentAsync(
                DefaultDeploymentName,
                DefaultReplicas,
                _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncHandler(_mockedOutputAction);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestScaleDeploymentAsync_ReturnsCommandResult(bool expectedResult)
        {
            _processServiceMock.SetupRunCommandAsync().ReturnsResult(expectedResult);

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

            _processServiceMock.VerifyRunCommandAsyncArgs("kubectl", s => s.Contains("delete service"));
        }

        [TestMethod]
        public async Task TestDeleteServiceAsync_PassesKubeconfig()
        {
            await _objectUnderTest.DeleteServiceAsync(DefaultServiceName, _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains($"--kubeconfig=\"{_kubeConfigPath}\""));
        }

        [TestMethod]
        public async Task TestDeleteServiceAsync_PassesGivenService()
        {
            await _objectUnderTest.DeleteServiceAsync(ExpectedServiceName, _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains(ExpectedServiceName));
        }

        [TestMethod]
        public async Task TestDeleteServiceAsync_PassesHandler()
        {
            await _objectUnderTest.DeleteServiceAsync(DefaultServiceName, _mockedOutputAction);

            _processServiceMock.VerifyRunCommandAsyncHandler(_mockedOutputAction);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task TestDeleteServiceAsync_ReturnsCommandResult(bool expectedResult)
        {
            _processServiceMock.SetupRunCommandAsync().ReturnsResult(expectedResult);

            bool result = await _objectUnderTest.DeleteServiceAsync(DefaultServiceName, _mockedOutputAction);

            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public async Task TestGetServiceClusterIpAsync_ReturnsClusterIpFromService()
        {
            const string expectedClusterIP = "expectedClusterIP";
            _processServiceMock.SetupGetJsonOutput<GkeService>()
                .ReturnsResult(new GkeService { Spec = new GkeServiceSpec { ClusterIp = expectedClusterIP } });

            string result = await _objectUnderTest.GetServiceClusterIpAsync(DefaultServiceName);

            Assert.AreEqual(expectedClusterIP, result);
        }

        [TestMethod]
        public async Task TestGetServiceClusterIpAsync_ReturnsNullForMissingSpec()
        {
            _processServiceMock.SetupGetJsonOutput<GkeService>().ReturnsResult(new GkeService { Spec = null });

            string result = await _objectUnderTest.GetServiceClusterIpAsync(DefaultServiceName);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestGetServiceClusterIpAsync_ReturnsNullForMissingService()
        {
            _processServiceMock.SetupGetJsonOutput<GkeService>().ReturnsResult(null);

            string result = await _objectUnderTest.GetServiceClusterIpAsync(DefaultServiceName);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestGetPublicServiceIpAsync_ReturnsIpFromServiceLoadBalancerIngress()
        {
            const string expectedIpAddress = "expected-ip-address";
            _processServiceMock.SetupGetJsonOutput<GkeService>()
                .ReturnsResult(
                    new GkeService
                    {
                        Status = new GkeStatus
                        {
                            LoadBalancer = new GkeLoadBalancer
                            {
                                Ingress = new List<GkeLoadBalancerIngress>
                                {
                                    new GkeLoadBalancerIngress { Ip = expectedIpAddress }
                                }
                            }
                        }
                    });

            string result = await _objectUnderTest.GetPublicServiceIpAsync(DefaultServiceName);

            Assert.AreEqual(expectedIpAddress, result);
        }

        [TestMethod]
        public async Task TestGetPublicServiceIpAsync_ReturnsIpFromFirstValidIngress()
        {
            const string expectedIpAddress = "expected-ip-address";
            _processServiceMock.SetupGetJsonOutput<GkeService>()
                .ReturnsResult(
                    new GkeService
                    {
                        Status = new GkeStatus
                        {
                            LoadBalancer = new GkeLoadBalancer
                            {
                                Ingress = new List<GkeLoadBalancerIngress>
                                {
                                    new GkeLoadBalancerIngress(),
                                    new GkeLoadBalancerIngress { Ip = expectedIpAddress },
                                    new GkeLoadBalancerIngress { Ip = "SomeOtherIpAddress" }
                                }
                            }
                        }
                    });

            string result = await _objectUnderTest.GetPublicServiceIpAsync(DefaultServiceName);

            Assert.AreEqual(expectedIpAddress, result);
        }

        [TestMethod]
        public async Task TestGetPublicServiceIpAsync_ReturnsNullForOnlyInvalidIngress()
        {
            _processServiceMock.SetupGetJsonOutput<GkeService>()
                .ReturnsResult(
                    new GkeService
                    {
                        Status = new GkeStatus
                        {
                            LoadBalancer = new GkeLoadBalancer
                            {
                                Ingress = new List<GkeLoadBalancerIngress> { new GkeLoadBalancerIngress() }
                            }
                        }
                    });

            string result = await _objectUnderTest.GetPublicServiceIpAsync(DefaultServiceName);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestGetPublicServiceIpAsync_ReturnsNullForEmptyIngressList()
        {
            _processServiceMock.SetupGetJsonOutput<GkeService>()
                .ReturnsResult(
                    new GkeService
                    {
                        Status = new GkeStatus
                        {
                            LoadBalancer = new GkeLoadBalancer { Ingress = new List<GkeLoadBalancerIngress>() }
                        }
                    });

            string result = await _objectUnderTest.GetPublicServiceIpAsync(DefaultServiceName);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestGetPublicServiceIpAsync_ReturnsNullForNullIngressList()
        {
            _processServiceMock.SetupGetJsonOutput<GkeService>()
                .ReturnsResult(new GkeService { Status = new GkeStatus { LoadBalancer = new GkeLoadBalancer() } });

            string result = await _objectUnderTest.GetPublicServiceIpAsync(DefaultServiceName);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestGetPublicServiceIpAsync_ReturnsNullForNullLoadBalancer()
        {
            _processServiceMock.SetupGetJsonOutput<GkeService>()
                .ReturnsResult(new GkeService { Status = new GkeStatus() });

            string result = await _objectUnderTest.GetPublicServiceIpAsync(DefaultServiceName);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestGetPublicServiceIpAsync_ReturnsNullForNullStatus()
        {
            _processServiceMock.SetupGetJsonOutput<GkeService>().ReturnsResult(new GkeService());

            string result = await _objectUnderTest.GetPublicServiceIpAsync(DefaultServiceName);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task TestGetPublicServiceIpAsync_ReturnsNullForNullService()
        {
            _processServiceMock.SetupGetJsonOutput<GkeService>().ReturnsResult(null);

            string result = await _objectUnderTest.GetPublicServiceIpAsync(DefaultServiceName);

            Assert.IsNull(result);
        }
    }
}
