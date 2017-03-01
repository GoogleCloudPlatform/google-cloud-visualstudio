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
using EventGroupTimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Represents a <seealso cref="ErrorGroupStats"/> object that is displayed in data grid row.
    /// </summary>
    public class ErrorGroupItem : Model
    {
        /// <summary>
        /// Gets the parsed exception.
        /// </summary>
        public ParsedException ParsedException { get; }

        /// <summary>
        /// The error group that represents a group of errors.
        /// </summary>
        public ErrorGroupStats ErrorGroup { get; }

        /// <summary>
        /// Gets the command that navigates to detail window.
        /// </summary>
        public ProtectedCommand OnNavigateToDetailCommand { get; }

        /// <summary>
        /// Show service context. 
        /// <seealso cref="ErrorGroupStats.AffectedServices"/>.
        /// </summary>
        public string SeenIn => GetSeeIn();

        /// <summary>
        /// Optional, displays the context status code.
        /// </summary>
        public int? Status => ErrorGroup.Representative?.Context?.HttpRequest?.ResponseStatusCode;

        /// <summary>
        /// Gets the message to display for the error group.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The first stack frame summary for the error group.
        /// </summary>
        public string FirstStackFrame => ParsedException?.FirstFrameSummary;

        /// <summary>
        /// Gets the affected user count. Could be null.
        /// </summary>
        public long? AffectedUsersCount => ErrorGroup.AffectedUsersCount;

        /// <summary>
        /// Gets the formated <seealso cref="ErrorGroupStats.FirstSeenTime"/>.
        /// </summary>
        public string FirstSeenTime => FormatErrorGroupDateTime(ErrorGroup.FirstSeenTime);

        /// <summary>
        /// Gets the formated <seealso cref="ErrorGroupStats.LastSeenTime"/>.
        /// </summary>
        public string LastSeenTime => FormatErrorGroupDateTime(ErrorGroup.LastSeenTime);

        /// <summary>
        /// Gets the time range of the <seealso cref="ErrorGroup"/>.
        /// </summary>
        public EventGroupTimeRangeEnum EventGroupTimeRange { get; }

        /// <summary>
        /// Gets the list of <seealso cref="TimedCount"/> of the error group.
        /// </summary>
        public IList<TimedCount> TimedCountList => ErrorGroup.TimedCounts;

        public ErrorGroupItem(ErrorGroupStats errorGroup)
        {
            if (errorGroup == null)
            {
                throw new ErrorReportingException(new ArgumentNullException(nameof(errorGroup)));
            }

            ErrorGroup = errorGroup;
            if (ErrorGroup.Representative?.Message != null)
            {
                ParsedException = new ParsedException(ErrorGroup.Representative?.Message, this);
            }
            Message = ParsedException?.Header;
            OnNavigateToDetailCommand = new ProtectedCommand(null);
        }

        /// <summary>
        /// The input in compiling time is object. 
        /// In runtime, it can be either a DateTime object or a UTC time formated string.
        /// 
        /// If it is not DateTime type or the string is failed to convert to UTC time. 
        /// </summary>
        /// <returns>
        /// Formated time string to Local Time, Local Culture, if input is DateTime type or
        ///   the input is string and can be converted to UTC time.
        /// return null otherwise.
        /// </returns>
        private static string FormatErrorGroupDateTime(object datetime)
        {
            DateTime dt = DateTime.MinValue;
            if (datetime is DateTime)
            {
                dt = (DateTime)datetime;
            }
            else if (datetime is string)
            {
                if (!DateTime.TryParse(datetime as string,
                    CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dt))
                {
                    return null;
                }
            }

            return dt.ToLocalTime().ToString("F");
        }

        private string GetSeeIn()
        {
            if (ErrorGroup.AffectedServices == null)
            {
                return null;
            }
            var query = ErrorGroup.AffectedServices.Where(x => x.Service != null).Distinct(new ServiceContextComparer());
            return String.Join(Environment.NewLine, query.Select(x => FormatServiceContext(x)));
        }

        /// <summary>
        /// Format the <seealso cref="ServiceContext"/> to a string.
        /// </summary>
        private string FormatServiceContext(ServiceContext serviceContext)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(serviceContext.Service);        
            if (!String.IsNullOrWhiteSpace(serviceContext.Version))
            {
                builder.Append($":{serviceContext.Version}");
            }

            return builder.ToString();
        }
    }
}
   