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

using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.ServiceManagement.v1;
using Google.Apis.ServiceManagement.v1.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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

            await OperationUtils.AwaitOperationAsync(
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
