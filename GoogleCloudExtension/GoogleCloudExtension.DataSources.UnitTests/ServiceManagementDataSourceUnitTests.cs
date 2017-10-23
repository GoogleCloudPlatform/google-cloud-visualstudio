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
            var serviceStatus = await dataSource.CheckServicesStatusAsync(new[] { Service1, Service2, Service3 });

            var expectedResult = new[]
            {
                new ServiceStatus(Service1, true),
                new ServiceStatus(Service2, false),
                new ServiceStatus(Service3, true)
            };
            Assert.AreEqual(expectedResult.Length, serviceStatus.Count());
            Assert.IsTrue(expectedResult.Zip(serviceStatus, (x, y) => x.Name == y.Name && x.Enabled == y.Enabled).All(x => x));
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
            var serviceStatus = await dataSource.CheckServicesStatusAsync(new[] { Service1, Service2, Service3 });

            var expectedResult = new[]
            {
                new ServiceStatus(Service1, true),
                new ServiceStatus(Service2, true),
                new ServiceStatus(Service3, true)
            };
            Assert.AreEqual(expectedResult.Length, serviceStatus.Count());
            Assert.IsTrue(expectedResult.Zip(serviceStatus, (x, y) => x.Name == y.Name && x.Enabled == y.Enabled).All(x => x));
        }
    }
}
