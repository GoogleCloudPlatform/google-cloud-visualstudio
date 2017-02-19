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
using System.Diagnostics;
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
        /// The error group that represents a group of errors.
        /// </summary>
        public ErrorGroupStats ErrorGroup { get; }

        /// <summary>
        /// Gets the command that navigates to detail window.
        /// </summary>
        public ProtectedCommand OnNavigateToDetailCommand { get; }

        /// <summary>
        /// The error message displayed in data grid row.
        /// </summary>
        public string Error => ErrorGroup.Representative.Message;

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
        /// The stack as string for the error group.
        /// </summary>
        public string Stack { get; }

        /// <summary>
        /// Gets the affected user count. Could be null.
        /// </summary>
        public long? AffectedUsersCount => ErrorGroup.AffectedUsersCount;

        /// <summary>
        /// Gets the formated <seealso cref="ErrorGroupStats.FirstSeenTime"/>.
        /// </summary>
        public string FirstSeenTime => FormatDateTime(ErrorGroup.FirstSeenTime);

        /// <summary>
        /// Gets the formated <seealso cref="ErrorGroupStats.LastSeenTime"/>.
        /// </summary>
        public string LastSeenTime => FormatDateTime(ErrorGroup.LastSeenTime);

        /// <summary>
        /// Initializes a new instance of <seealso cref="ErrorGroupItem"/> class.
        /// </summary>
        /// <param name="errorGroup">
        /// The error group that represents a group of errors.
        /// <seealso cref="ErrorGroupStats"/>
        /// </param>
        public ErrorGroupItem(ErrorGroupStats errorGroup)
        {
            if (errorGroup == null)
            {
                throw new ErrorReportingException(new ArgumentNullException(nameof(errorGroup)));
            }

            ErrorGroup = errorGroup;
            string[] lines = ErrorGroup.Representative?.Message?.Split(
                    new string[] { "\r\n", "\n" }, 
                    StringSplitOptions.RemoveEmptyEntries);
            Message = lines?[0];
            Stack = lines?[1];
            OnNavigateToDetailCommand = new ProtectedCommand(NavigateToDetailWindow);
        }

        private static string FormatDateTime(object datetime)
        {
            return datetime is DateTime ? 
                ((DateTime)datetime).ToString(Resources.ErrorReportingDateTimeFormat) 
                : datetime?.ToString();
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
        /// Format the <seealso cref="ServiceContext"/> as string.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <returns></returns>
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

        private void NavigateToDetailWindow()
        {
            Debug.WriteLine($"{Message} is clicked");
            var detailWindow = ToolWindowUtils.ShowToolWindow<ErrorReportingDetailToolWindow>();
            detailWindow.ViewModel.UpdateView(this);
        }
    }
}
    