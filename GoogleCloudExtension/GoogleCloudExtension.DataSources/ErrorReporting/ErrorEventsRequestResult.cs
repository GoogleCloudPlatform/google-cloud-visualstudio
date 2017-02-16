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

using Google.Apis.Clouderrorreporting.v1beta1.Data;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.ErrorReporting
{
    /// <summary>
    /// The result of Stackdriver error reporting list <seealso cref="ErrorEvent"/> request. 
    /// </summary>
    public class ErrorEventsRequestResult
    {
        /// <summary>
        /// Gets a list of <seealso cref="ErrorEvent"/> objects. 
        /// The value could be null or empty, caller to check.
        /// </summary>
        public IList<ErrorEvent> ErrorEvents { get; }

        /// <summary>
        /// A token is returned if available logs count exceeds the page size.
        /// </summary>
        public string NextPageToken { get; }

        /// <summary>
        /// Initializes an instance of <seealso cref="ErrorEventsRequestResult"/> class.
        /// </summary>
        /// <param name="errorEvents">
        /// A list of <seealso cref="ErrorEvent"/> objects. 
        /// Null is valid input.
        /// </param>
        /// <param name="pageToken">Next page token to retrieve more pages.</param>
        public ErrorEventsRequestResult(IList<ErrorEvent> errorEvents, string nextPageToken)
        {
            ErrorEvents = errorEvents;
            NextPageToken = nextPageToken;
        }
    }
}
