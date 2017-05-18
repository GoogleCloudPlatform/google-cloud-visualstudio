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
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;

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
        [JsonProperty("zone")]
        public string Zone { get; set; }

        /// <summary>
        /// The GCE instance name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The chosen username
        /// </summary>
        [JsonProperty("user")]
        public string DefaultUser { get; set; }

        /// <summary>
        /// A helper function to compare to <paramref name="gceInstance"/>
        /// </summary>
        /// <param name="gceInstance">The GCE Instance object.</param>
        /// <returns></returns>
        public bool InstanceEqual(Instance gceInstance) =>
                Zone == gceInstance.Zone &&
                Name == gceInstance.Name;            
    }

    /// <summary>
    /// Save user preferences for attach remote debugger feature.
    /// </summary>
    public class AttachDebuggerOptionsPage : DialogPage
    {
        private const int InstancesDefaultUserMaxLength = 10;

        private List<InstanceDefaultUser> _defaultUsers;

        /// <summary>
        /// Saves the list of instance default username as string.
        /// List of objects can not be saved into storage. 
        /// Serialize/Deserialize the list of <seealso cref="_defaultUsers"/> into string.
        /// </summary>
        [Browsable(false)]
        public string DefaultUsersString { get; set; }

        /// <summary>
        /// Natvie, Managed, Script etc.
        /// </summary>
        public string DefaultDebuggerEngineType { get; set; }

        /// <summary>
        /// Read the list of default users from store.
        /// List of objects can not be saved into storage. 
        /// Serialize/Deserialize the list of <seealso cref="_defaultUsers"/> into string.
        /// </summary>
        public List<InstanceDefaultUser> DeserializeDefaultUsersList()
        {
            if (_defaultUsers == null && DefaultUsersString != null)
            {
                try
                {
                    _defaultUsers = JsonConvert.DeserializeObject<List<InstanceDefaultUser>>(DefaultUsersString);
                }
                catch (JsonSerializationException)
                { }
            }
            return _defaultUsers;
        }

        /// <summary>
        /// Get the default username for a given GCE instance.
        /// </summary>
        /// <param name="gceInstance">The GCE instance object.</param>
        public string GetInstanceDefaultUser(Instance gceInstance) =>
            _defaultUsers?.FirstOrDefault(x => x.InstanceEqual(gceInstance))?.DefaultUser;

        /// <summary>
        /// Save instance default username.
        /// </summary>
        /// <param name="gceInstance">The GCE instance.</param>
        /// <param name="user">The chosen username.</param>
        public void SetInstanceDefaultUser(Instance gceInstance, string user)
        {
            if (_defaultUsers == null)
            {
                _defaultUsers = new List<InstanceDefaultUser>();
            }
            _defaultUsers.RemoveAll(x => x.InstanceEqual(gceInstance));

            // Limit the list size so that it does not grow indefinitely.
            if (_defaultUsers.Count >= InstancesDefaultUserMaxLength)
            {
                _defaultUsers.RemoveAt(0);
            }

            _defaultUsers.Add(
                new InstanceDefaultUser
                {
                    Zone = gceInstance.Zone,
                    Name = gceInstance.Name,
                    DefaultUser = user
                });

            SaveSettingsToStorage();
        }

        /// <summary>
        /// Save settings
        /// </summary>
        public override void SaveSettingsToStorage()
        {
            if (_defaultUsers != null)
            {
                DefaultUsersString = JsonConvert.SerializeObject(_defaultUsers);
            }
            base.SaveSettingsToStorage();
        }
    }
}
