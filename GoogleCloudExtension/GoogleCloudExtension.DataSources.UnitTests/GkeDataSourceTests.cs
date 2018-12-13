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

using Google;
using Google.Apis.Container.v1;
using Google.Apis.Container.v1.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources.UnitTests
{
    [TestClass]
    public class GkeDataSourceTests
    {
        private Mock<ContainerService> _serviceMock;
        private Mock<ProjectsResource.LocationsResource.ClustersResource> _clustersResource;
        private GkeDataSource _objectUnderTest;
        private const string DefaultProjectId = "ProjectId";

        [TestInitialize]
        public void BeforeEach()
        {
            _serviceMock = new Mock<ContainerService>();
            _clustersResource = _serviceMock.Resource(s => s.Projects)
                .Resource(p => p.Locations)
                .Resource(l => l.Clusters);
            _objectUnderTest = new GkeDataSource(_serviceMock.Object, DefaultProjectId);
        }

        [TestMethod]
        public async Task TestGetClusterListAsync_ReturnsClusters()
        {
            Cluster[] expectedClusters = { new Cluster { Name = "ClusterName" } };
            _clustersResource.Request(c => c.List(It.IsAny<string>()))
                .ResponseReturns(new ListClustersResponse { Clusters = expectedClusters });

            IList<Cluster> result = await _objectUnderTest.GetClusterListAsync();
            CollectionAssert.AreEqual(expectedClusters, result.ToList());
        }

        [TestMethod]
        public async Task TestGetClusterListAsync_PassesCorrectParameter()
        {
            const string expectedProjectId = "expected-project-id";
            _clustersResource.Request(c => c.List(It.IsAny<string>()))
                .ResponseReturns(new ListClustersResponse());
            _objectUnderTest = new GkeDataSource(_serviceMock.Object, expectedProjectId);

            await _objectUnderTest.GetClusterListAsync();

            _clustersResource.Verify(c => c.List($"projects/{expectedProjectId}/locations/-"));
        }

        [TestMethod]
        public async Task TestGetClusterListAsync_ThrowsDataSourceException()
        {
            _clustersResource.Request(c => c.List(It.IsAny<string>()))
                .Response<ListClustersResponse>()
                .Throws(new GoogleApiException(nameof(ContainerService), "Expected Message"));

            await Assert.ThrowsExceptionAsync<DataSourceException>(() => _objectUnderTest.GetClusterListAsync());
        }
    }
}
