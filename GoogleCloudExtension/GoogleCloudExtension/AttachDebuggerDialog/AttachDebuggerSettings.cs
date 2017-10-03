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

using Google.Apis.Compute.v1.Data;
using GoogleCloudExtension.SolutionUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.AttachDebuggerDialog
{
    /// <summary>
    /// Keeps the last time chosen username for a given GCE instance.
    /// </summary>
    public class InstanceDefaultUser
    {
        /// <summary>
        /// The GCE instance zone
        /// </summary>
        [JsonProperty("instanceZone")]
        public string InstanceZone { get; set; }

        /// <summary>
        /// The GCE instance name
        /// </summary>
        [JsonProperty("instanceName")]
        public string InstanceName { get; set; }

        /// <summary>
        /// The chosen username
        /// </summary>
        [JsonProperty("user")]
        public string User { get; set; }

        /// <summary>
        /// A helper function to check if it matches to <paramref name="gceInstance"/>
        /// </summary>
        /// <param name="gceInstance">The GCE Instance object.</param>
        /// <returns></returns>
        public bool InstanceMatches(Instance gceInstance) =>
            InstanceZone == gceInstance.Zone && InstanceName == gceInstance.Name;
    }

    /// <summary>
    /// Save user preferences for attach remote debugger feature.
    /// </summary>
    public class AttachDebuggerSettings
    {
        private const int InstancesDefaultUserMaxLength = 10;
        private static readonly Lazy<AttachDebuggerSettings> s_instance = new Lazy<AttachDebuggerSettings>();
        private static readonly Lazy<List<InstanceDefaultUser>> s_lazyDefaultUsersList =
            new Lazy<List<InstanceDefaultUser>>(AttachDebuggerSettingsStore.ReadGceInstanceDefaultUsers);
        private static List<InstanceDefaultUser> _defaultUsersList => s_lazyDefaultUsersList.Value;

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static AttachDebuggerSettings Current => s_instance.Value;

        /// <summary>
        /// Visual Studio debugger engine type. Natvie, Managed, Script etc.
        /// </summary>
        [SolutionSettingKey("google_debugger_engine_type")]
        public string DefaultDebuggerEngineType { get; set; }

        /// <summary>
        /// Choose a default process name to attach to.
        /// </summary>
        [SolutionSettingKey("google_debugger_process_name")]
        public string DefaultDebuggeeProcessName { get; set; }

        /// <summary>
        /// Get the default username for a given GCE instance.
        /// </summary>
        /// <param name="gceInstance">The GCE instance object.</param>
        public string GetInstanceDefaultUser(Instance gceInstance) =>
            _defaultUsersList.FirstOrDefault(x => x.InstanceMatches(gceInstance))?.User;

        /// <summary>
        /// Save instance default username.
        /// </summary>
        /// <param name="gceInstance">The GCE instance.</param>
        /// <param name="user">The chosen username.</param>
        public void SetInstanceDefaultUser(Instance gceInstance, string user)
        {
            _defaultUsersList.RemoveAll(x => x.InstanceMatches(gceInstance));

            // Limit the list size so that it does not grow indefinitely.
            if (_defaultUsersList.Count >= InstancesDefaultUserMaxLength)
            {
                _defaultUsersList.RemoveAt(0);
            }

            _defaultUsersList.Add(
                new InstanceDefaultUser
                {
                    InstanceZone = gceInstance.Zone,
                    InstanceName = gceInstance.Name,
                    User = user
                });
            AttachDebuggerSettingsStore.PersistGceInstanceDefaultUsers(_defaultUsersList);
        }
    }
}
