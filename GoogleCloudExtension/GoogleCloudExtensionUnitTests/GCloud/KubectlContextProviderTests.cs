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
using GoogleCloudExtension.GCloud;
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.GCloud
{
    [TestClass]
    public class KubectlContextProviderTests : ExtensionTestBase
    {
        private const string DefaultClusterName = "default-cluster";
        private const string DefaultRegion = "us-central1";
        private const string DefaultZone = "us-central1-a";
        private const string AdditionalZone = "us-central1-b";
        private Mock<IProcessService> _processServiceMock;
        private KubectlContextProvider _objectUnderTest;
        private Mock<IFileSystem> _fileSystemMock;

        private static IEnumerable<object[]> AllClusters => ZonalClusters.Concat(RegionalClusters);

        private static IEnumerable<object[]> ZonalClusters
        {
            get
            {
                yield return new object[]
                {
                    new Cluster
                    {
                        Name = DefaultClusterName,
                        Location = DefaultZone,
                        Locations = new[] { DefaultZone }
                    }
                };
            }
        }

        private static IEnumerable<object[]> RegionalClusters
        {
            get
            {
                yield return new object[]
                {
                    new Cluster
                    {
                        Name = DefaultClusterName,
                        Location = DefaultZone,
                        Locations = new string[0]
                    }
                };
                yield return new object[]
                {
                    new Cluster
                    {
                        Name = DefaultClusterName,
                        Location = DefaultZone,
                        Locations = null
                    }
                };
                yield return new object[]
                {
                    new Cluster
                    {
                        Name = DefaultClusterName,
                        Location = DefaultRegion,
                        Locations = new[] { DefaultZone }
                    }
                };
                yield return new object[]
                {
                    new Cluster
                    {
                        Name = DefaultClusterName,
                        Location = DefaultRegion,
                        Locations = new[]
                        {
                            DefaultZone,
                            AdditionalZone
                        }
                    }
                };
            }
        }

        [TestInitialize]
        public void BeforeEach()
        {
            _processServiceMock = new Mock<IProcessService>();
            _processServiceMock.SetupRunCommandAsync().ReturnsResult(true);
            _fileSystemMock = new Mock<IFileSystem> { DefaultValueProvider = DefaultValueProvider.Mock };
            _objectUnderTest = new KubectlContextProvider(
                _fileSystemMock.ToLazy(),
                CredentialStoreMock.ToLazy(),
                _processServiceMock.ToLazy());
        }

        [TestMethod]
        [DynamicData(nameof(AllClusters))]
        public async Task TestGetKubectlContextForClusterAsync_RunsGcloudContainerClustersGetCredentials(
            Cluster cluster)
        {
            await _objectUnderTest.GetKubectlContextForClusterAsync(cluster);

            _processServiceMock.VerifyRunCommandAsyncArgs(
                "cmd.exe",
                s => s.Contains("gcloud container clusters get-credentials"));
        }

        [TestMethod]
        [DynamicData(nameof(AllClusters))]
        public async Task TestGetKubectlContextForClusterAsync_RunsCommandAgainstExpectedCluster(Cluster cluster)
        {

            await _objectUnderTest.GetKubectlContextForClusterAsync(cluster);

            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains(cluster.Name));
        }


        [TestMethod]
        [DynamicData(nameof(ZonalClusters))]
        public async Task TestGetKubectlContextForClusterAsync_RunsCommandAgainstExpectedZone(Cluster zonalCluster)
        {
            await _objectUnderTest.GetKubectlContextForClusterAsync(zonalCluster);

            string zoneArg = $"--zone={zonalCluster.Location}";
            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains(zoneArg));
        }

        [TestMethod]
        [DynamicData(nameof(RegionalClusters))]
        public async Task TestGetKubectlContextForClusterAsync_RunsCommandAgainstExpectedRegion(Cluster regionalCluster)
        {
            await _objectUnderTest.GetKubectlContextForClusterAsync(regionalCluster);

            string regionArg = $"--region={regionalCluster.Location}";
            _processServiceMock.VerifyRunCommandAsyncArgs(s => s.Contains(regionArg));
        }

        [TestMethod]
        [DynamicData(nameof(AllClusters))]
        public async Task TestGetKubectlContextForClusterAsync_RunsCommandWithExpectedGoogleCredentialsEnvVar(
            Cluster cluster)
        {
            const string expectedCredentialsPath = "expected-credentials-path";
            CredentialStoreMock.Setup(cs => cs.CurrentAccountPath).Returns(expectedCredentialsPath);

            await _objectUnderTest.GetKubectlContextForClusterAsync(cluster);

            _processServiceMock.VerifyRunCommandAsyncEnvironment(
                env => env[KubectlContext.GoogleApplicationCredentialsVariable] == expectedCredentialsPath);
        }

        [TestMethod]
        [DynamicData(nameof(AllClusters))]
        public async Task TestGetKubectlContextForClusterAsync_RunsCommandWithExpectedUseDefaultCredentialsEnvVar(
            Cluster cluster)
        {

            await _objectUnderTest.GetKubectlContextForClusterAsync(cluster);

            _processServiceMock.VerifyRunCommandAsyncEnvironment(
                env => env[KubectlContext.UseApplicationDefaultCredentialsVariable] == KubectlContext.TrueValue);
        }

        [TestMethod]
        [DynamicData(nameof(AllClusters))]
        public async Task TestGetKubectlContextForClusterAsync_RunsCommandWithKubeConfigEnvVar(Cluster cluster)
        {

            await _objectUnderTest.GetKubectlContextForClusterAsync(cluster);

            _processServiceMock.VerifyRunCommandAsyncEnvironment(
                env => env.ContainsKey(KubectlContext.KubeConfigVariable));
        }

        [TestMethod]
        [DynamicData(nameof(AllClusters))]
        public async Task TestGetKubectlContextForClusterAsync_ThrowsOnCommandFailure(Cluster cluster)
        {
            _processServiceMock.SetupRunCommandAsync().ReturnsResult(false);

            GCloudException e = await Assert.ThrowsExceptionAsync<GCloudException>(
                () => _objectUnderTest.GetKubectlContextForClusterAsync(cluster));

            StringAssert.Contains(e.Message, cluster.Name);
        }
    }
}
