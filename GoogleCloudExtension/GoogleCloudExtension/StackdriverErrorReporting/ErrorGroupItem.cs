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
using EventGroupTimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;

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
        /// <seealso cref="ErrorGroupStats.Count"/>. 
        /// </summary>
        public long Count => ErrorGroup.Count.GetValueOrDefault();

        /// <summary>
        /// Show service context. 
        /// <seealso cref="ErrorGroupStats.AffectedServices"/>.
        /// </summary>
        public object SeenIn => 
            String.Join(
                Environment.NewLine, 
                ErrorGroup.AffectedServices.Select(x => FormatServiceContext(x)));

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

        public object FirstSeen => ErrorGroup.FirstSeenTime;
        public object LastSeen => ErrorGroup.LastSeenTime;
        public long? AffectedUsersCount => ErrorGroup.AffectedUsersCount;

        /// <summary>
        /// Initializes a new instance of <seealso cref="ErrorGroupItem"/> class.
        /// </summary>
        /// <param name="errorGroup">
        /// The error group that represents a group of errors.
        /// <seealso cref="ErrorGroupStats"/>
        /// </param>
        public ErrorGroupItem(ErrorGroupStats errorGroup)
        {
            ErrorGroup = errorGroup;
            string[] lines = ErrorGroup.Representative?.Message?.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Message = lines?[0];
            Stack = lines?[1];
            OnNavigateToDetailCommand = new ProtectedCommand(NavigateToDetailWindow);
        }
        
        /// <summary>
        /// Format the <seealso cref="ServiceContext"/> as string.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <returns></returns>
        private string FormatServiceContext(ServiceContext serviceContext)
        {
            StringBuilder builder = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(serviceContext.ETag))
            {
                builder.AppendLine(serviceContext.ETag);
            }

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
    