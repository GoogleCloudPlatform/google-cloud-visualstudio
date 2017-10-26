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

using Google.Apis.Appengine.v1.Data;
using System.Linq;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class containes extension useable for <seealso cref="Location"/> instances.
    /// </summary>
    public static class GaeLocationExtensions
    {
        /// <summary>
        /// Returns whether the given <seealso cref="Location"/> is enabled for App Engine flexible environment.
        /// </summary>
        public static bool IsFlexEnabled(this Location self)
        {
            object value;
            if (!self.Metadata.TryGetValue("flexibleEnvironmentAvailable", out value))
            {
                return false;
            }

            return (bool)value;
        }

        /// <summary>
        /// Returns the region name from the full name. This name can be used to display to the user and to
        /// specify a region in the GAE API.
        /// </summary>
        public static string GetRegionName(this Location self)
        {
            string[] parts = self.Name.Split('/');
            return parts.Last();
        }
    }
}
