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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that returns data from Stackdriver Logging API.
    /// The API is described at https://cloud.google.com/logging/docs/api/reference/rest
    /// </summary>
    public class LoggingDataSource : DataSourceBase<LoggingService>
    {
        /// <summary>
        /// Initializes an instance of the data source.
        /// </summary>
        /// <param name="projectId">The Google Cloud Platform project id of the current user account .</param>
        /// <param name="credential">The credentials to use for the call.</param>
        public LoggingDataSource(string projectId, GoogleCredential credential, string appName)
            : base(projectId, credential, init => new LoggingService(init), appName)
        { }

        /// <summary>
        /// Returns the list of MonitoredResourceDescriptor.
        /// The size of entire set of MonitoredResourceDescriptor is small. 
        /// Batch all in one request in case it spans multiple pages.
        /// </summary>
        public async Task<IList<MonitoredResourceDescriptor>> GetResourceDescriptorsAsync()
        {
            return await LoadPagedListAsync(
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
        /// <returns>
        ///     <seealso ref="LogEntryRequestResult" /> object that contains log entries and next page token.
        /// </returns>
        public async Task<LogEntryRequestResult> ListLogEntriesAsync(
            string filter = null, string orderBy = null, int? pageSize = null, string nextPageToken = null)
        {
            try
            {
                string projectsFilter = $"projects/{ProjectId}";
                var requestData = new ListLogEntriesRequest
                {
                    ResourceNames = new List<string>(new string[] { projectsFilter }),
                    Filter = filter,
                    OrderBy = orderBy,
                    PageSize = pageSize
                };

                requestData.PageToken = nextPageToken;
                var response = await Service.Entries.List(requestData).ExecuteAsync();
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
