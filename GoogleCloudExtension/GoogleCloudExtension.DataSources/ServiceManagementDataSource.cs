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

using Google.Apis.Auth.OAuth2;
using Google.Apis.ServiceManagement.v1;
using Google.Apis.ServiceManagement.v1.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class provides access to the ServiceManagement API, which allows the caller to list the
    /// APIs and services enabled for a particular GCP project. This data source will also allow the caller
    /// to enable and disable APIs for the given GCP project.
    /// </summary>
    public class ServiceManagementDataSource : DataSourceBase<ServiceManagementService>
    {
        public ServiceManagementDataSource(string projectId, GoogleCredential credential, string appName)
            : base(projectId, credential, init => new ServiceManagementService(init), appName)
        { }

        // For testing.
        internal ServiceManagementDataSource(ServiceManagementService mockedService, string projectId)
            : base(projectId, null, init => mockedService, null)
        { }

        /// <summary>
        /// Returns the <seealso cref="ServiceStatus"/> for each service in the given collection.
        /// </summary>
        /// <param name="serviceNames">The collection of names to check.</param>
        /// <returns>A task that will contain the collection of <seealso cref="ServiceStatus"/> with the status of each service.</returns>
        public async Task<IEnumerable<ServiceStatus>> CheckServicesStatusAsync(IEnumerable<string> serviceNames)
        {
            IList<ManagedService> enabledServices = await GetProjectEnabledServicesAsync();
            IEnumerable<string> enabledServiceNames = enabledServices.Select(x => x.ServiceName);
            return serviceNames.Select(x => new ServiceStatus(x, enabledServiceNames.Contains(x)));
        }

        /// <summary>
        /// Enables all of the services in the given collection.
        /// </summary>
        /// <param name="serviceNames">The collection of service names to enable.</param>
        /// <returns>A task that will be completed once all services are enabled.</returns>
        public async Task EnableAllServicesAsync(IEnumerable<string> serviceNames)
        {
            foreach (var service in serviceNames)
            {
                Operation operation = await Service.Services
                    .Enable(new EnableServiceRequest { ConsumerId = $"project:{ProjectId}" }, service)
                    .ExecuteAsync();

                await operation.AwaitOperationAsync(
                    refreshOperation: x => Service.Operations.Get(x.Name).ExecuteAsync(),
                    isFinished: x => x.Done ?? false,
                    getErrorData: x => x.Error,
                    getErrorMessage: err => err.Message);
            }
        }

        private async Task<IList<ManagedService>> GetProjectEnabledServicesAsync()
        {
            var request = Service.Services.List();
            request.ConsumerId = $"project:{ProjectId}";

            return await LoadPagedListAsync(
                token =>
                {
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                x => x.Services,
                x => x.NextPageToken);
        }
    }
}
