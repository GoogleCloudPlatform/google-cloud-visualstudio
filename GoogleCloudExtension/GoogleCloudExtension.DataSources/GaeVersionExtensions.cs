﻿// Copyright 2016 Google Inc. All Rights Reserved.
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
    public static class GaeVersionExtensions
    {
        /// <summary>
        /// The value of a .Net runtime for a GAE version.
        /// </summary>
        public const string AspNetCoreRuntime = "aspnetcore";

        /// <summary>
        /// The serving status for a GAE version serving.
        /// </summary>
        public const string ServingStatus = "SERVING";

        /// <summary>
        /// The serving status for a GAE version not serving.
        /// </summary>
        public const string StoppedStatus = "STOPPED";

        /// <summary>
        /// The identifier for the Flex environment.
        /// </summary>
        public const string FlexibleEnvironment = "flexible";

        /// <summary>
        /// Gets an operation id.
        /// </summary>
        public static string GetOperationId(this Operation operation)
        {
            return operation.Name.Split('/').Last();
        }

        /// <returns>True if the version is serving.</returns>
        public static bool IsServing(this Version version)
        {
            return ServingStatus.Equals(version.ServingStatus);
        }

        /// <returns>True if the version is stopped.</returns>
        public static bool IsStopped(this Version version)
        {
            return StoppedStatus.Equals(version.ServingStatus);
        }
    }
}
