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

        /// <param name="instance"></param>
        /// <param name="ipAddress"></param>
        /// <returns>True if IP address is an authorized network of the instance</returns>
        public static bool IpAddressAuthorized(DatabaseInstance instance, string ipAddress)
        {
            IList<AclEntry> networks = instance?.Settings?.IpConfiguration?.AuthorizedNetworks;
            return networks != null && networks.Any(x => x.Value.Equals(ipAddress));
        }

        /// <summary>
        /// Removes the given IP address from the instance's authorized networks if it exists.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="ipAddress"></param>
        public static void RemoveAuthorizedNetwork(DatabaseInstance instance, string ipAddress)
        {
            IList<AclEntry> networks = instance?.Settings?.IpConfiguration?.AuthorizedNetworks;
            AclEntry entry = networks?.FirstOrDefault(x => x.Value.Equals(ipAddress));
            if (networks != null && entry != null)
            {
                networks.Remove(entry);
            }
        }

        /// <summary>
        /// Adds the given IP address to the instance's authorized networks.
        /// Makes no check that the IP address is or is not already an authorized network.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="ipAddress"></param>
        public static void AddAuthorizedNetwork(DatabaseInstance instance, string ipAddress)
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

            // Add the new authorized network.
            AclEntry acl = new AclEntry
            {
                Value = ipAddress
            };
            instance.Settings.IpConfiguration.AuthorizedNetworks.Add(acl);
        }
    }
}
