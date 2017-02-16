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

using Google.Apis.Clouderrorreporting.v1beta1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources.ErrorReporting;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Linq;
using System.Text;
using EventGroupTimeRangeEnum = Google.Apis.Clouderrorreporting.v1beta1.ProjectsResource.GroupStatsResource.ListRequest.TimeRangePeriodEnum;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    public class ErrorGroupItem : Model
    { 
        public ErrorGroupStats ErrorGroup { get; set; }
        public ProtectedCommand NavigateDetailCommand { get; }
        public string Error => ErrorGroup.Representative.Message;
        public long Count => ErrorGroup.Count.GetValueOrDefault();
        public object FirstSeen => ErrorGroup.FirstSeenTime;
        public object LastSeen => ErrorGroup.LastSeenTime;
        public object SeenIn => String.Join(Environment.NewLine, 
            ErrorGroup.AffectedServices.Select(x => FormatServiceContext(x)));
        public string Message { get; }
        public string Stack { get; }
        public long? AffectedUsersCount => ErrorGroup.AffectedUsersCount;
        public int? Status => ErrorGroup.Representative?.Context?.HttpRequest?.ResponseStatusCode;

        // TODO: not necessary?  remove.
        public EventGroupTimeRangeEnum EventGroupTimeRange { get; }

        public ErrorGroupItem(ErrorGroupStats errorGroup, EventGroupTimeRangeEnum groupTimeRange)
        {
            EventGroupTimeRange = groupTimeRange;
            ErrorGroup = errorGroup;
            string[] lines = ErrorGroup.Representative.Message.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            Message = lines?[0];
            Stack = lines?[1];
            NavigateDetailCommand = new ProtectedCommand(NavigateDetail);
        }
        
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

        private void NavigateDetail()
        {
            Debug.WriteLine($"{Message} is clicked");
            if (ErrorReportingDetailToolWindowCommand.Instance == null)
            {
                var detailWindow = DetailWindow.Instance;
                detailWindow.Show();
                detailWindow.ViewModel.UpdateView(this, ErrorReportingViewModel.Instance.TimeRangeButtonsModel.SelectedTimeRangeItem); 
            }
            else
            {
                var detailWindow = ErrorReportingDetailToolWindowCommand.ShowWindow();
                detailWindow.ViewModel.UpdateView(this, ErrorReportingViewModel.Instance.TimeRangeButtonsModel.SelectedTimeRangeItem);
            }
        }
    }
}
    