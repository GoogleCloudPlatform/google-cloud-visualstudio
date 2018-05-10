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
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(nameof(name));
            }

            Name = name;
            Enabled = enabled;
        }

        /// <summary>
        /// Checks if this object is equal to the one provided.
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as ServiceStatus;
            if (other == null)
            {
                return false;
            }

            return Name == other.Name && Enabled == other.Enabled;
        }

        /// <summary>
        /// Returns the hashcode for the object, required when implementing equality.
        /// Implementation adapted from:
        ///   https://stackoverflow.com/questions/263400/what-is-the-best-algorithm-for-an-overridden-system-object-gethashcode
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Name.GetHashCode();
                hash = hash * 23 + Enabled.GetHashCode();
                return hash;
            }
        }
    }
}
