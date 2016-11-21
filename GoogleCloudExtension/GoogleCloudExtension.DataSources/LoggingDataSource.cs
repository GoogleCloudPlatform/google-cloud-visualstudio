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
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{

    /// <summary>
    /// Data source that returns data from Stackdriver Logging API.
    /// The API is described at https://cloud.google.com/logging/docs/api/reference/rest/v2/entries/list. 
    /// </summary>
    public class LoggingDataSource : DataSourceBase<LoggingService>
    {
        private class LogEntryRequestParams
        {
            public string PageToken { get; set; }
            public string Filter { get; set; }
            public string ProjectId { get; set; }
            public int? PageSize { get; set; }
        }

        private LogEntryRequestParams _lastSuccessfulRequestParams;

        /// <summary>
        /// The ListLogEntriesRequest page size.
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// Initializes an instance of the data source.
        /// </summary>
        /// <param name="projectId">The Google Cloud Platform project id of the current user account .</param>
        /// <param name="credential">The credentials to use for the call.</param>
        public LoggingDataSource(string projectId, GoogleCredential credential, string appName)
            : base(projectId, credential, init => new LoggingService(init), appName)
        { }


        private ListLogEntriesRequest CreateRequestFromParams(LogEntryRequestParams requestParams)
        {
            string projectsFilter = $"projects/{requestParams.ProjectId}";

            return new ListLogEntriesRequest()
            {
                ResourceNames = new List<string>(new string[] { projectsFilter }),
                Filter = requestParams.Filter,
                PageSize = requestParams.PageSize,
                PageToken = requestParams.PageToken                
            };
        }

        private async Task<IList<LogEntry>> MakeAsyncRequest(LogEntryRequestParams requestParams)
        {
            ListLogEntriesRequest requestData = CreateRequestFromParams(requestParams);
            var request = Service.Entries.List(requestData);
            ListLogEntriesResponse response = await request.ExecuteAsync();
            _lastSuccessfulRequestParams = requestParams;
            _lastSuccessfulRequestParams.PageToken = response.NextPageToken;
            return response.Entries;
        }

        /// <summary>
        /// Returns the next page of all LogEntry items of the project id.
        /// Please note: The values of other method parameters should be identical to those in the previous call.
        /// </summary>
        /// <returns></returns>
        public async Task<IList<LogEntry>> GetNextPageLogEntryListAsync()
        {
            if (_lastSuccessfulRequestParams.ProjectId != ProjectId)
            {
                throw new DataSourceException("The project id has changed. Fail to fetch next page. Do refresh.");
            }

            return await MakeAsyncRequest(_lastSuccessfulRequestParams);
        }

        /// <summary>
        /// Returns the list of all log entries of the project id.
        /// This function returns first page of log entries if one page size does not fit.
        /// </summary>
        /// <param name="filter">The Google.Apis.Logging.v2.Data.ListLogEntriesRequest Filter</param>
        public async Task<IList<LogEntry>> GetLogEntryListAsync(string filter)
        {
            LogEntryRequestParams requestParams = new LogEntryRequestParams()
            {
                ProjectId = ProjectId,
                Filter = filter,
                PageSize = PageSize
            };

            return await MakeAsyncRequest(requestParams);
        }

        /// <summary>
        /// Returns the list of MonitoredResourceDescriptor.
        /// </summary>
        public async Task<IList<MonitoredResourceDescriptor>> GetResourceDescriptorAsync()
        {
            List<MonitoredResourceDescriptor> results = new List<MonitoredResourceDescriptor>();
            var request = Service.MonitoredResourceDescriptors.List();
            ListMonitoredResourceDescriptorsResponse response = null;
            do
            {
                request.PageToken = response?.NextPageToken;
                response = await request.ExecuteAsync();
                if (response?.ResourceDescriptors == null)
                {
                    return results;
                }
                results.AddRange(response?.ResourceDescriptors);
            } while (!string.IsNullOrWhiteSpace(response?.NextPageToken));

            return results;
        }
    }
}
