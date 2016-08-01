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

using Google.Apis.Compute.v1.Data;

namespace GoogleCloudExtension.FirewallManagement
{
    /// <summary>
    /// This class contains the information about a firewall port.
    /// </summary>
    public class PortInfo
    {
        /// <summary>
        /// The display name for the user.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The port number.
        /// </summary>
        public int Port { get; }

        public PortInfo(string name, int port)
        {
            Name = name;
            Port = port;
        }

        /// <summary>
        /// Returns the tag to be used for the port for the given <paramref name="instance"/>.
        /// </summary>
        /// <param name="instance">The instance that is going to be affected by the port.</param>
        /// <returns></returns>
        public string GetTag(Instance instance) => $"{instance.Name}-tcp-{Port}";
    }
}
