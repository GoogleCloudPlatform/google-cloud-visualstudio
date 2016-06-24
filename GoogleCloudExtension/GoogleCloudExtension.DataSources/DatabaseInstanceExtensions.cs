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

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// This class implement extension methods which implement behaviors on top of the database instance models.
    /// </summary>
    public static class DatabaseInstanceExtensions
    {
        public const string RunnableState = "RUNNABLE";
        public const string SuspendedState = "SUSPENDED";
        public const string PendingCreateState = "PENDING_CREATE";
        public const string MaintenanceState = "MAINTENANCE";
        public const string UnknownState = "UNKNOWN_STATE";
    }
}
