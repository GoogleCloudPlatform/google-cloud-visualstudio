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

using Google.Apis.Appengine.v1.Data;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources
{
    public static class GaeServiceExtensions
    {
        /// <summary>
        /// The value of a traffic split by IP address for a service.
        /// </summary>
        public static readonly string ShardByIp = "IP";

        /// <summary>
        /// The value of a traffic split by cookies for a service.
        /// </summary>
        public static readonly string ShardByCookie = "COOKIE";

        /// <summary>
        /// Get the traffic allocation of a version.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="versionId"></param>
        /// <returns>The traffic allocation if it exists, 0.0 otherwise.</returns>
        public static double GetTrafficAllocation(this Service service, string versionId)
        {
            IDictionary<string, double?> allocations = service.Split.Allocations;
            return allocations.TryGetValue(versionId, out double? allocation) ? allocation.GetValueOrDefault() : 0.0;
        }
    }
}
