// Copyright 2016 Google Inc. All Rights Reserved.
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
using Google.Apis.Logging.v2;
using Google.Apis.Logging.v2.Data;
using Google.Apis.Logging.v2.Data.Extensions;
using Google.Apis.Logging.v2.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsResource = Google.Apis.Logging.v2.Extensions.LogsResource;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that returns data from Stackdriver Logging API.
    /// The API is described at https://cloud.google.com/logging/docs/api/reference/rest
    /// </summary>
    public class LoggingDataSource : DataSourceBase<LoggingService>, ILoggingDataSource
    {
        /// <summary>
        /// Google cloud uses format of projects/{project_id} as projects filter.
        /// </summary>
        private string ProjectFilter => $"projects/{ProjectId}";

        private readonly List<string> _resourceNames;
        private readonly ResourceKeysResource _resourceKeysResource;
        private readonly ResourceTypesResource _resourceTypesResource;
        private readonly LogsResource _logsResource;

        /// <summary>
        /// Initializes an instance of the data source.
        /// </summary>
        /// <param name="projectId">The Google Cloud Platform project id of the current user account .</param>
        /// <param name="credential">The credentials to use for the call.</param>
        /// <param name="appName">The name of the application.</param>
        public LoggingDataSource(string projectId, GoogleCredential credential, string appName)
            : base(projectId, credential, init => new LoggingService(init), appName)
        {
            _resourceNames = new List<string>(new string[] { ProjectFilter });
            _resourceKeysResource = new ResourceKeysResource(Service);
            _resourceTypesResource = new ResourceTypesResource(Service);
            _logsResource = new LogsResource(Service);
        }

        /// <summary>
        /// List all resource keys for the project.
        /// </summary>
        public async Task<IList<ResourceKeys>> ListResourceKeysAsync()
        {
            return await LoadPagedListAsync(
                (token) =>
                {
                    var request = _resourceKeysResource.List(ProjectFilter);
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                x => x.ResourceKeys,
                x => x.NextPageToken);
        }

        /// <summary>
        /// List all resource type values for the given resource type and resource key.
        /// </summary>
        /// <param name="resourceType">Required, the resource type.</param>
        /// <param name="resourceKey">Optional, the resource key as prefix.</param>
        /// <returns>
        /// A task with result of a list of resource keys.
        /// </returns>
        public Task<IList<string>> ListResourceTypeValuesAsync(string resourceType, string resourceKey = null)
        {
            if (resourceType == null)
            {
                throw new ArgumentNullException(nameof(resourceType));
            }
            string parentParam = $"{ProjectFilter}/resourceTypes/{resourceType}";
            return LoadPagedListAsync(
                (token) =>
                {
                    var request = _resourceTypesResource.Values.List(parentParam);
                    request.PageToken = token;
                    request.IndexPrefix = resourceKey;
                    return request.ExecuteAsync();
                },
                x => x.ResourceValuePrefixes,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Returns the list of MonitoredResourceDescriptor.
        /// The size of entire set of MonitoredResourceDescriptor is small.
        /// Batch all in one request in case it spans multiple pages.
        /// </summary>
        public Task<IList<MonitoredResourceDescriptor>> GetResourceDescriptorsAsync()
        {
            return LoadPagedListAsync(
                (token) =>
                {
                    var request = Service.MonitoredResourceDescriptors.List();
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                x => x.ResourceDescriptors,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Returns a list of log names of current Google Cloud project.
        /// Only logs that have entries are listed.
        /// The size of entire set of log names is small. 
        /// Batch all in one request in unlikely case it spans multiple pages.
        /// </summary>
        /// <param name="resourceType">The resource type, i.e gce_instance.</param>
        /// <param name="resourcePrefixList">
        /// Optional, can be null. 
        /// A list of resource prefixes. 
        /// i.e,  for resource type app engine, the prefixe can be the module ids. 
        /// </param>
        public Task<IList<string>> ListProjectLogNamesAsync(string resourceType, IEnumerable<string> resourcePrefixList = null)
        {
            return LoadPagedListAsync(
                (token) =>
                {
                    var request = _logsResource.List(ProjectFilter);
                    request.PageToken = token;
                    request.ResourceType = resourceType;
                    request.ResourceIndexPrefix = resourcePrefixList == null ? null :
                        String.Join("", resourcePrefixList.Select(x => $"/{x}"));
                    return request.ExecuteAsync();
                },
                x => x.LogNames,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Returns the first page of log entries of the project id.
        /// </summary>
        /// <param name="filter">
        /// Optional,
        /// Refert to https://cloud.google.com/logging/docs/view/advanced_filters. 
        /// </param>
        /// <param name="orderBy">
        /// Optional, "timestamp desc" or "timestamp asc"
        /// </param>
        /// <param name="pageSize">
        /// Optional,
        /// If page size is not specified, a server side default value is used. 
        /// </param>
        /// <param name="nextPageToken">
        /// Optional,
        /// The page token from last list request response.
        /// If the value is null, fetch the first page results.
        /// If the value is not null, it is hard requirement that the filter, orderBy and pageSize parameters
        /// must stay same as the prior call.
        /// </param>
        /// <param name="cancelToken">Optional. A cancellation token.</param>
        /// <returns>
        ///     <seealso ref="LogEntryRequestResult" /> object that contains log entries and next page token.
        /// </returns>
        public async Task<LogEntryRequestResult> ListLogEntriesAsync(
            string filter = null, string orderBy = null, int? pageSize = null, string nextPageToken = null,
            CancellationToken cancelToken = default(CancellationToken))
        {
            try
            {
                var requestData = new ListLogEntriesRequest
                {
                    ResourceNames = _resourceNames,
                    Filter = filter,
                    OrderBy = orderBy,
                    PageSize = pageSize
                };

                requestData.PageToken = nextPageToken;
                var response = await Service.Entries.List(requestData).ExecuteAsync(cancelToken);
                return new LogEntryRequestResult(response.Entries, response.NextPageToken);
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to get log entries: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
