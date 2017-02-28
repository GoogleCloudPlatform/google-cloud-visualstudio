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
        #region Implement IEqualityComparer interface

        /// <summary>
        /// Compare the Service and Version fields of two <seealso cref="ServiceContext"/> objects.
        /// Service must not be null.
        /// Version is optional, can be null.
        /// </summary>
        public bool Equals(ServiceContext self, ServiceContext other)
        {
            if (self == null || other == null)
            {
                return false;
            }
            return string.CompareOrdinal(self.Service, other.Service) == 0
                && string.CompareOrdinal(self.Version, other.Version) == 0;
        }

        /// <summary>
        /// Consider the hash code same if Service and Version are same.
        /// Even if there are differences in other fields, ignore them.
        /// </summary>
        public int GetHashCode(ServiceContext obj)
        {
            if (obj == null)
            {
                throw new ErrorReportingException(new ArgumentNullException(nameof(obj)));
            }
            int serviceCode = obj.Service.GetHashCode();
            return obj.Version == null ? serviceCode : serviceCode ^ obj.Version.GetHashCode();
        }
        #endregion
    }
}
