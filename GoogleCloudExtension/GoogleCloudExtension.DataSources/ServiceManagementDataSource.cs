using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.ServiceManagement.v1;
using Google.Apis.ServiceManagement.v1.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    public class ServiceManagementDataSource : DataSourceBase<ServiceManagementService>
    {
        public ServiceManagementDataSource(string projectId, GoogleCredential credential, string appName)
            : base(projectId, credential, init => new ServiceManagementService(init), appName)
        { }

        public async Task<IList<ManagedService>> GetServicesAvailableAsync()
        {
            var request = Service.Services.List();

            return await LoadPagedListAsync(
                (token) =>
                {
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                x => x.Services,
                x => x.NextPageToken);
        }

        public async Task<IList<ManagedService>> GetProjectEnabledServicesAsync()
        {
            var request = Service.Services.List();
            request.ConsumerId = $"project:{ProjectId}";

            return await LoadPagedListAsync(
                (token) =>
                {
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                x => x.Services,
                x => x.NextPageToken);
        }

        public async Task<bool> IsServiceEnabledAsync(string serviceName)
        {
            return (await CheckServicesStatusAsync(new List<string> { serviceName })).First().Item2;
        }

        public async Task<IEnumerable<Tuple<string, bool>>> CheckServicesStatusAsync(IEnumerable<string> serviceNames)
        {
            var enabledServices = (await GetProjectEnabledServicesAsync()).Select(x => x.ServiceName);
            return serviceNames.Select(x => new Tuple<string, bool>(x, enabledServices.Contains(x)));
        }

        public async Task<Service> GetServiceAsync(string serviceName)
        {
            try
            {
                var configs = await GetServiceConfigurationsAsync(serviceName);
                return configs.FirstOrDefault();
            }
            catch (GoogleApiException ex)
            {
                throw new DataSourceException($"Failed to read service {serviceName}", ex);
            }
        }

        public async Task EnableServiceAsync(string serviceName)
        {
            var operation = await Service.Services
                .Enable(new EnableServiceRequest { ConsumerId = $"project:{ProjectId}" }, serviceName)
                .ExecuteAsync();

            await OperationsUtils.AwaitOperationAsync(
                operation,
                refreshOperation: x => Service.Operations.Get(x.Name).ExecuteAsync(),
                isFinished: x => x.Done ?? false,
                getErrorData: x => x.Error?.Message);
        }

        public async Task EnableAllServicesAsync(IEnumerable<string> serviceNames)
        {
            foreach (var service in serviceNames)
            {
                await EnableServiceAsync(service);
            }
        }

        public async Task<IList<Service>> GetServiceConfigurationsAsync(string serviceName)
        {
            var request = Service.Services.Configs.List(serviceName);

            return await LoadPagedListAsync(
                (token) =>
                {
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                x => x.ServiceConfigs,
                x => x.NextPageToken);
        }
    }
}
