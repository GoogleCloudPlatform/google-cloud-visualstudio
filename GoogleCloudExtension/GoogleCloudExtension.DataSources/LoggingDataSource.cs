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
        /// LogEntry List request parameters.
        /// </summary>
        private class LogEntryRequestParams
        {
            /// <summary>
            /// Optional        
            /// The PageToken for requesting next page of log entries.
            /// </summary>
            public string PageToken;

            /// <summary>
            /// Optional
            /// Refert to https://cloud.google.com/logging/docs/view/advanced_filters. 
            /// </summary>
            public string Filter;

            /// <summary>
            /// Optional
            /// If page size is not specified, a server side default value is used. 
            /// </summary>
            public int? PageSize;
        }


        /// <summary>
        /// Initializes an instance of the data source.
        /// </summary>
        /// <param name="projectId">The Google Cloud Platform project id of the current user account .</param>
        /// <param name="credential">The credentials to use for the call.</param>
        public LoggingDataSource(string projectId, GoogleCredential credential, string appName)
            : base(projectId, credential, init => new LoggingService(init), appName)
        { }

        /// <summary>
        /// Returns the next page of all LogEntry items of the project id.
        /// Please note, when calling this method, the filter, pageSize must be same as prior call.
        /// </summary>
        /// <param name="filter">The filter to use.</param>
        /// <param name="pageToken">The page token returend from last request.</param>
        /// <returns>
        ///     A tuple of :   List of log entries,  optional next page token.
        /// </returns>
        public async Task<Tuple<IList<LogEntry>, string>> GetNextPageLogEntryListAsync(
             string pageToken, string filter = null, int? pageSize = null)
        {
            return await MakeAsyncRequest(new LogEntryRequestParams() {
                Filter = filter,
                PageToken = pageToken,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Returns the first page of log entries of the project id.
        /// </summary>
        /// <param name="filter">Optional. The Google.Apis.Logging.v2.Data.ListLogEntriesRequest Filter</param>
        /// <param name="pageSize">
        ///     Optional. specify the page size. 
        ///     A default value is used on severside if not specified.
        /// </param>
        /// <returns>
        ///     A tuple of :   List of log entries,  optional next page token.
        /// </returns>
        public async Task<Tuple<IList<LogEntry>, string>> GetLogEntryListAsync(
            string filter = null, int? pageSize = null)
        {
            return await MakeAsyncRequest(new LogEntryRequestParams()
            {
                Filter = filter,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Returns the list of MonitoredResourceDescriptor.
        /// The size of entire set of MonitoredResourceDescriptor is small. 
        /// Batch all in one request in case it spans multiple pages.
        /// </summary>
        public async Task<IList<MonitoredResourceDescriptor>> GetResourceDescriptorAsync()
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

        private ListLogEntriesRequest CreateRequestFromParams(LogEntryRequestParams requestParams)
        {
            string projectsFilter = $"projects/{ProjectId}";

            return new ListLogEntriesRequest()
            {
                ResourceNames = new List<string>(new string[] { projectsFilter }),
                Filter = requestParams.Filter,
                PageSize = requestParams.PageSize,
                PageToken = requestParams.PageToken
            };
        }

        private async Task<Tuple<IList<LogEntry>, string>> MakeAsyncRequest(LogEntryRequestParams requestParams)
        {
            try
            {
                ListLogEntriesRequest requestData = CreateRequestFromParams(requestParams);
                var request = Service.Entries.List(requestData);
                ListLogEntriesResponse response = await request.ExecuteAsync();
                return new Tuple<IList<LogEntry>, string>(response?.Entries, response?.NextPageToken);
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to get log entries: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
