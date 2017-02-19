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

using Google.Apis.Clouderrorreporting.v1beta1.Data;
using System;
using System.Collections.Generic;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Define comparer for <seealso cref="ServiceContext"/>.
    /// </summary>
    internal class ServiceContextComparer : IEqualityComparer<ServiceContext>
    {
        public bool Equals(ServiceContext x, ServiceContext y)
        {
            // Service won't be null.
            if (string.CompareOrdinal(x.Service, y.Service) != 0)
            {
                return false;
            }
            if (x.Version == null || y.Version == null && x.Version != y.Version)
            {
                return false;
            }
            return string.CompareOrdinal(x.Version, y.Version) == 0;
        }

        public int GetHashCode(ServiceContext obj)
        {
            // Service won't be null.
            int serviceCode = obj.Service.GetHashCode();
            return obj.Version == null ? serviceCode : serviceCode ^ obj.Version.GetHashCode();
        }
    }
}
