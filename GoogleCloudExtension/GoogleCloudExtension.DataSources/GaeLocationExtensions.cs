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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class containes extension useable for <seealso cref="Location"/> instances.
    /// </summary>
    public static class GaeLocationExtensions
    {
        public static bool IsFlexEnabled(this Location self)
        {
            object value;
            if (!self.Metadata.TryGetValue("flexibleEnvironmentAvailable", out value))
            {
                return false;
            }

            return (bool)value;
        }

        public static string GetDisplayName(this Location self)
        {
            string[] parts = self.Name.Split('/');
            return parts.Last();
        }
    }
}
