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

using System;

namespace GoogleCloudExtension.Utils
{
    internal static class MySQLUtils
    {
        /// <summary>
        /// The GUID for the MySQL Database.
        /// </summary>
        public static readonly Guid MySQLDataSource = new Guid("{98FBE4D8-5583-4233-B219-70FF8C7FBBBD}");

        /// <summary>
        /// The GUID for the MySQL Database Provider.
        /// </summary>
        public static readonly Guid MySQLDataProvider = new Guid("{C6882346-E592-4da5-80BA-D2EADCDA0359}");
    }
}
