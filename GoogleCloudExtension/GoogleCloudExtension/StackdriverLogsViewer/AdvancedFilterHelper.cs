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

using System;
using System.Diagnostics;
using System.Text;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    internal static class AdvancedFilterHelper
    {
        private const string GCEInstanceResourceType = "gce_instance";
        private const string GAEAppResourceType = "gae_app";

        /// <summary>
        /// Show logs that only contains the GCP VM Instance id label,
        /// that is under resource type of gce_instance
        /// </summary>
        /// <param name="window">A <seealso cref="LogsViewerToolWindow"/> object. </param>
        /// <param name="instanceId">The VM instance Id.</param>
        public static void FilterVMInstanceLog(this LogsViewerToolWindow window, string instanceId)
        {
            if (window?.ViewModel == null || String.IsNullOrWhiteSpace(instanceId))
            {
                Debug.WriteLine("Invalid input at FilterVMInstanceLog");
            }

            StringBuilder filter = new StringBuilder();
            filter.AppendLine($"resource.type=\"{GCEInstanceResourceType}\"");
            filter.AppendLine($"resource.labels.instance_id=\"{instanceId}\"");
            window.ViewModel.FilterLog(filter.ToString());
        }

        /// <summary>
        /// Show logs that only contains the GAE service lable,
        /// that is under resource type of gae_app.        
        /// </summary>
        /// <param name="window">A <seealso cref="LogsViewerToolWindow"/> object. </param>
        /// <param name="serviceId">GAE service id</param>
        /// <param name="version">GAE service version</param>
        public static void FilterGceServiceLog(this LogsViewerToolWindow window, string serviceId, string version=null)
        {
            if (window?.ViewModel == null || String.IsNullOrWhiteSpace(serviceId))
            {
                Debug.WriteLine("Invalid input at FilterVMInstanceLog");
            }

            StringBuilder filter = new StringBuilder();
            filter.AppendLine($"resource.type=\"{GAEAppResourceType}\"");
            filter.AppendLine($"resource.labels.module_id=\"{serviceId}\"");
            if (!String.IsNullOrWhiteSpace(version))
            {
                filter.AppendLine($"resource.labels.version_id=\"{version}\"");
            }
            window.ViewModel.FilterLog(filter.ToString());
        }
    }
}
