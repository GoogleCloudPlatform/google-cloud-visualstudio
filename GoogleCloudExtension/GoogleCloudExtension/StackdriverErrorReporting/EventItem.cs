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
using GoogleCloudExtension.Utils;
using System;
using System.Linq;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Wrapper to <seealso cref="ErrorEvent"/> as data model for detail view control.
    /// </summary>
    public class EventItem : Model
    {
        private static readonly string[] s_lineBreaks = new string[] { "\r\n", "\n", "\r" };
        private readonly ErrorEvent _error;

        /// <summary>
        /// Show the summary message of the error event.
        /// </summary>
        public string SummaryMessage { get; }

        /// <summary>
        /// The full message of the error event.
        /// </summary>
        public string Message => _error.Message;

        /// <summary>
        /// The time of the error event.
        /// </summary>
        public object EventTime => _error.EventTime;

        /// <summary>
        /// Initializes a new instance of <seealso cref="EventItem"/> class.
        /// </summary>
        /// <param name="error">A <seealso cref="ErrorEvent"/> object.</param>
        public EventItem(ErrorEvent error)
        {
            _error = error;
            var splits = _error.Message?.Split(s_lineBreaks, StringSplitOptions.RemoveEmptyEntries);
            SummaryMessage = splits?.FirstOrDefault();
        }
    }
}
