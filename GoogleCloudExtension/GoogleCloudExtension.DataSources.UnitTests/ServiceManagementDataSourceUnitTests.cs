// Copyright 2017 Google Inc. All Rights Reserved.
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

using Google.Apis.ServiceManagement.v1;
using Google.Apis.ServiceManagement.v1.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources.UnitTests
{
    /// <summary>
    /// Test class for <seealso cref="ServiceManagementDataSource"/>
    /// </summary>
    [TestClass]
    public class ServiceManagementDataSourceUnitTests : DataSourceUnitTestsBase
    {
        private const string ProjectName = "Project";
        private const string Service1 = "Service1";
        private const string Service2 = "Service2";
        private const string Service3 = "Service3";
        private const string PageToken = "Token";

        [TestMethod]
        public async Task TestCheckServicesStatusAsyncSinglePage()
        {
            var responses = new[]
            {
                new ListServicesResponse
                {
                    Services = new List<ManagedService>
                    {
                        new ManagedService { ServiceName=Service1 },
                        new ManagedService { ServiceName=Service3 }
                    }
                }
            };
            ServiceManagementService service = GetMockedService(
                (ServiceManagementService s) => s.Services,
                t => t.List(),
                responses);

            var dataSource = new ServiceManagementDataSource(service, ProjectName);
            var actualResults = (await dataSource.CheckServicesStatusAsync(new[] { Service1, Service2, Service3 })).ToList();

            var expectedResults = new[]
            {
                new ServiceStatus(Service1, true),
                new ServiceStatus(Service2, false),
                new ServiceStatus(Service3, true)
            };
            Assert.AreEqual(expectedResults.Length, actualResults.Count);
            CollectionAssert.AreEqual(expectedResults, actualResults);
        }

        [TestMethod]
        public async Task TestCheckServicesStatusAsyncMultiplePage()
        {
            var responses = new[]
            {
                new ListServicesResponse
                {
                    NextPageToken = PageToken,
                    Services = new List<ManagedService>
                    {
                        new ManagedService { ServiceName=Service1 },
                        new ManagedService { ServiceName=Service3 }
                    }
                },
                new ListServicesResponse
                {
                    Services = new List<ManagedService>
                    {
                        new ManagedService { ServiceName=Service2 }
                    }
                }
            };
            ServiceManagementService service = GetMockedService(
                (ServiceManagementService s) => s.Services,
                t => t.List(),
                responses);

            var dataSource = new ServiceManagementDataSource(service, ProjectName);
            var actualResults = (await dataSource.CheckServicesStatusAsync(new[] { Service1, Service2, Service3 })).ToList();

            var expectedResults = new[]
            {
                new ServiceStatus(Service1, true),
                new ServiceStatus(Service2, true),
                new ServiceStatus(Service3, true)
            };
            Assert.AreEqual(expectedResults.Length, actualResults.Count);
            CollectionAssert.AreEqual(expectedResults, actualResults);
        }
    }
}
