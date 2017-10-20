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

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Contains the status of a single service.
    /// </summary>
    public class ServiceStatus
    {
        /// <summary>
        /// The name of the service.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Whether the service is enabled or not.
        /// </summary>
        public bool Enabled { get; }

        public ServiceStatus(string name, bool enabled)
        {
            Name = name;
            Enabled = enabled;
        }
    }
}
