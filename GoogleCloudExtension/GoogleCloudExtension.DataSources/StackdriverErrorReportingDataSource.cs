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
using Google.Apis.Clouderrorreporting.v1beta1;
using Google.Apis.Clouderrorreporting.v1beta1.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using EventTimeRange = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.EventsResource.ListRequest.TimeRangePeriodEnum;
using TimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that returns Google Cloud Stackdriver Error Reporting group status and events.
    /// </summary>
    public class StackdriverErrorReportingDataSource : DataSourceBase<ClouderrorreportingService>, IStackdriverErrorReportingDataSource
    {
        /// <summary>
        /// Initializes an instance of <seealso cref="StackdriverErrorReportingDataSource"/> class.
        /// </summary>
        /// <param name="projectId">A Google Cloud Platform project id of the current user account.</param>
        /// <param name="credential">The credentials to use for the call.</param>
        /// <param name="appName">The name of the application.</param>
        public StackdriverErrorReportingDataSource(string projectId, GoogleCredential credential, string appName)
                : base(projectId, credential, init => new ClouderrorreportingService(init), appName)
        { }

        /// <summary>
        /// Get a page of <seealso cref="ErrorGroupStats"/> for the given <paramref name="groupId"/>.
        /// The result is divided into pages when the result set is too large. 
        /// This call get one page of results.
        /// </summary>
        /// <param name="timeRange">Specifiy the time range of the query.</param>
        /// <param name="timedCountDuration">
        /// The preferred duration for a single returned `TimedCount`. 
        /// Optional, If not set, no timed counts are returned.
        /// </param>
        /// <param name="groupId">Optional, The error group id.</param>
        /// <param name="nextPageToken">Optional, A next page token provided by a previous response.</param>
        /// <returns>
        /// A task with <seealso cref="ListGroupStatsResponse"/> as result.
        /// </returns>
        public Task<ListGroupStatsResponse> GetPageOfGroupStatusAsync(
            TimeRangeEnum timeRange,
            string timedCountDuration = null,
            string groupId = null,
            string nextPageToken = null)
        {
            var request = Service.Projects.GroupStats.List(ProjectResourceName);
            request.TimeRangePeriod = timeRange;
            request.TimedCountDuration = timedCountDuration;
            request.GroupId = groupId;
            request.PageToken = nextPageToken;
            try
            {
                return request.ExecuteAsync();
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to get GroupStats: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Gets a page of <seealso cref="ErrorEvent"/> for an error group.
        /// The result is divided into pages when the result set is too large. 
        /// This call get one page of results.
        /// </summary>
        /// <param name="errorGroup">An error group. <seealso cref="ErrorGroupStats"/>.</param>
        /// <param name="period">
        /// The time period for the query.
        /// Optional, defaults to 30 days.
        /// </param>
        /// <param name="nextPageToken">optional, A next page token provided by a previous response.</param>
        /// <returns>
        /// A task with <seealso cref="ListEventsResponse"/> as result.
        /// </returns>
        public Task<ListEventsResponse> GetPageOfEventsAsync(
            ErrorGroupStats errorGroup,
            EventTimeRange period = EventTimeRange.PERIOD30DAYS,
            string nextPageToken = null)
        {
            var request = Service.Projects.Events.List(ProjectResourceName);
            request.TimeRangePeriod = period;
            request.PageToken = nextPageToken;
            request.GroupId = errorGroup.Group.GroupId;
            try
            {
                return request.ExecuteAsync();
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to get ErrorEvents: {ex.Message}");
                throw new DataSourceException(ex.Message, ex);
            }
        }
    }
}
