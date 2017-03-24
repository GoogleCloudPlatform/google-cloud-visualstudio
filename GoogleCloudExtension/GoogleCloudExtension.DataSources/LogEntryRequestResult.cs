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

using Google.Apis.Logging.v2.Data;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Wrap up Log Entries list and the next page token as get log entry list methods result.
    /// </summary>
    public class LogEntryRequestResult
    {
        /// <summary>
        /// The returned log entries.  It could be null if no logs found by the filter condition.
        /// </summary>
        public IList<LogEntry> LogEntries { get; }

        /// <summary>
        /// A token is returned if available logs count exceeds the page size.
        /// </summary>
        public string NextPageToken { get; }

        /// <summary>
        /// Initializes an instance of LogEntryRequestResult.
        /// </summary>
        /// <param name="logEntries">A list of LogEntry objects. Null is valid input.</param>
        /// <param name="pageToken">Next page token to retrieve more pages.</param>
        public LogEntryRequestResult(IList<LogEntry> logEntries, string nextPageToken)
        {
            LogEntries = logEntries;
            NextPageToken = nextPageToken;
        }
    }
}
