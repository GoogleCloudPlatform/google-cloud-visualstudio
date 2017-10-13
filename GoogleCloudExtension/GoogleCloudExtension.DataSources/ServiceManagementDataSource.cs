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
            // TODO: Determine if catching the list of services enabled for a project is worth while.
            var enabledServices = await GetProjectEnabledServicesAsync();
            return enabledServices.Select(x => x.ServiceName).Contains(serviceName);
        }

        public async Task EnableServiceAsync(string serviceName)
        {
            var operation = await Service.Services
                .Enable(new EnableServiceRequest { ConsumerId = ProjectId }, serviceName)
                .ExecuteAsync();

            await Operations.AwaitOperationAsync(
                operation,
                refreshOperation: x => Service.Operations.Get(x.Name).ExecuteAsync(),
                isFinished: x => x.Done ?? false,
                getErrorData: x => x.Error?.Message);
        }
    }
}
