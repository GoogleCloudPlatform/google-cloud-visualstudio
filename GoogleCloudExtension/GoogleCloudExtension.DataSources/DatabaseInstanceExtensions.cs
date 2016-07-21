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

using System.Collections.Generic;
using System.Linq;
using Google.Apis.SQLAdmin.v1beta4.Data;

namespace GoogleCloudExtension.DataSources
{
    public static class DatabaseInstanceExtensions
    {
        public const string RunnableState = "RUNNABLE";
        public const string SuspendedState = "SUSPENDED";
        public const string PendingCreateState = "PENDING_CREATE";
        public const string MaintenanceState = "MAINTENANCE";
        public const string UnknownState = "UNKNOWN_STATE";

        /// <summary>
        /// Update the authorized networks of a instance.  This completely replaces the current 
        /// list of networks with the new list.
        /// </summary>
        public static void UpdateAuthorizedNetworks(DatabaseInstance instance, IList<AclEntry> acls)
        {
            EnsureAuthorizedNetworksInitialized(instance);
            instance.Settings.IpConfiguration.AuthorizedNetworks = acls;
        }

        /// <summary>
        /// Ensure the Authorized Networks field and all of its parents are initialized for an instance.
        /// </summary>
        private static void EnsureAuthorizedNetworksInitialized(DatabaseInstance instance)
        {
            // Ensure that all nested objects are initilaized.
            if (instance.Settings == null)
            {
                instance.Settings = new Settings();
            }

            if (instance.Settings.IpConfiguration == null)
            {
                instance.Settings.IpConfiguration = new IpConfiguration();
            }

            if (instance.Settings.IpConfiguration.AuthorizedNetworks == null)
            {
                instance.Settings.IpConfiguration.AuthorizedNetworks = new List<AclEntry>();
            }
        }
    }
}
