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

using System.Diagnostics;
using System.Text;
using Google.Apis.Logging.v2.Data;
using GoogleCloudExtension.StackdriverLogsViewer.SearchMenuItem;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Extensions/helper functions for applying advanced filters to Stackdriver Logs Viewer.
    /// </summary>
    internal static class AdvancedFilterExtension
    {
        private const string SourceLocationQueryName = "sourceLocation";

        /// <summary>
        /// Show logs that only contain the GCE VM Instance id label,
        /// that is under resource type of gce_instance
        /// </summary>
        /// <param name="window">A <seealso cref="LogsViewerToolWindow"/> object. </param>
        /// <param name="instanceId">The VM instance Id.</param>
        public static void FilterVmInstanceLog(this LogsViewerToolWindow window, string instanceId)
        {
            if (window?.ViewModel == null || string.IsNullOrWhiteSpace(instanceId))
            {
                Debug.WriteLine("Invalid input at FilterVMInstanceLog");
                return;
            }

            var filter = new StringBuilder();
            filter.AppendLine($"resource.type=\"{ResourceTypeNameConsts.GceInstanceType}\"");
            filter.AppendLine($"resource.labels.instance_id=\"{instanceId}\"");
            window.ViewModel.FilterLog(filter.ToString());
        }

        /// <summary>
        /// Show logs that only contain the GAE service id label,
        /// that is under resource type of gae_app.
        /// </summary>
        /// <param name="window">A <seealso cref="LogsViewerToolWindow"/> object. </param>
        /// <param name="serviceId">GAE service id. Expect non null value input.</param>
        /// <param name="version">
        /// GAE service version. Null is valid input, that it will then return logs of all versions.
        /// </param>
        public static void FilterGaeServiceLog(this LogsViewerToolWindow window, string serviceId, string version = null)
        {
            if (window?.ViewModel == null || string.IsNullOrWhiteSpace(serviceId))
            {
                Debug.WriteLine("Invalid input at FilterVMInstanceLog");
                return;
            }

            var filter = new StringBuilder();
            filter.AppendLine($"resource.type=\"{ResourceTypeNameConsts.GaeAppType}\"");
            filter.AppendLine($"resource.labels.module_id=\"{serviceId}\"");
            if (!string.IsNullOrWhiteSpace(version))
            {
                filter.AppendLine($"resource.labels.version_id=\"{version}\"");
            }
            window.ViewModel.FilterLog(filter.ToString());
        }

        /// <summary>
        /// Show logs that matches the source information.
        /// </summary>
        /// <param name="window">A <seealso cref="LogsViewerToolWindow"/> object. </param>
        /// <param name="log">A <seealso cref="LogItem"/> object.</param>
        public static void FilterOnSourceLocation(this LogsViewerToolWindow window, LogItem log)
        {
            var filter = new StringBuilder();
            filter.AppendLine($"resource.type=\"{log.Entry.Resource.Type}\"");
            filter.AppendLine($"logName=\"{log.Entry.LogName}\"");
            filter.AppendLine($"{SourceLocationQueryName}.{nameof(LogEntrySourceLocation.File)}=\"{log.SourceFilePath.Replace(@"\", @"\\")}\"");
            filter.AppendLine($"{SourceLocationQueryName}.{nameof(LogEntrySourceLocation.Function)}=\"{log.Function}\"");
            filter.AppendLine($"{SourceLocationQueryName}.{nameof(LogEntrySourceLocation.Line)}=\"{log.SourceLine}\"");
            window.ViewModel.FilterLog(filter.ToString());
        }
    }
}
