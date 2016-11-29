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
    /// LogEntry request parameters.
    /// </summary>
    public class LogEntryRequestParams
    {
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

        /// <summary>
        /// Optional "timestamp desc" or "timestamp asc"
        /// </summary>
        public string OrderBy;
    }

    /// <summary>
    /// Wrap up Log Entries list and the next page token as get log entry list methods result.
    /// </summary>
    public class LogEntryRequestResult
    {
        /// <summary>
        /// The returned log entries.  It could be null if no logs found by the filter condition.
        /// </summary>
        public IList<LogEntry> LogEntries;

        /// <summary>
        /// A token is returned if available logs count exceeds the page size.
        /// </summary>
        public string NextPageToken;
    }

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
        /// Returns the next page of all LogEntry items of the project id.
        /// Please note, when calling this method, the filter, pageSize must be same as prior call.
        /// </summary>
        /// <param name="requestParams">The request parameters.</param>
        /// <param name="nextPageToken">
        /// The page token from last list request response.
        /// </param>
        /// <returns>
        ///     LogEntryRequestResult object that contains log entries and next page token.
        /// </returns>
        public async Task<LogEntryRequestResult> GetNextPageLogEntryListAsync(
            LogEntryRequestParams requestParams, string nextPageToken)
        {
            Debug.Assert(requestParams != null);
            Debug.Assert(nextPageToken != null);
            if (requestParams == null)
            {
                throw new ArgumentNullException(nameof(requestParams));
            }

            if (string.IsNullOrWhiteSpace(nextPageToken))
            {
                throw new ArgumentException(nameof(nextPageToken));
            }

            return await ListLogEntriesAsync(requestParams, nextPageToken);
        }

        /// <summary>
        /// Returns the first page of log entries of the project id.
        /// </summary>
        /// <param name="requestParams">
        /// The request parameters.
        /// NextPageToken i
        /// </param>
        /// <returns>
        ///     LogEntryRequestResult object that contains log entries and next page token.
        /// </returns>
        public async Task<LogEntryRequestResult> GetLogEntryListAsync(LogEntryRequestParams requestParams)
        {
            Debug.Assert(requestParams != null);
            if (requestParams == null)
            {
                throw new ArgumentNullException(nameof(requestParams));
            }

            return await ListLogEntriesAsync(requestParams);
        }

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

        private ListLogEntriesRequest CreateRequestFromParams(LogEntryRequestParams requestParams)
        {
            string projectsFilter = $"projects/{ProjectId}";

            return new ListLogEntriesRequest()
            {
                ResourceNames = new List<string>(new string[] { projectsFilter }),
                Filter = requestParams.Filter,
                PageSize = requestParams.PageSize,
                OrderBy = requestParams.OrderBy
            };
        }

        private async Task<LogEntryRequestResult> ListLogEntriesAsync(
            LogEntryRequestParams requestParams, string nextPageToken = null)
        {
            try
            {
                ListLogEntriesRequest requestData = CreateRequestFromParams(requestParams);
                requestData.PageToken = nextPageToken;
                var response = await Service.Entries.List(requestData).ExecuteAsync();
                return new LogEntryRequestResult()
                {
                    LogEntries = response.Entries,
                    NextPageToken = response.NextPageToken
                };
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to get log entries: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
