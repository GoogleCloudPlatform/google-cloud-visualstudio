﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsFileBrowser
{
    /// <summary>
    /// Useful extensions for the <seealso cref="GcsDataSource"/> that are specific to the GCS file browser.
    /// </summary>
    internal static class GcsDataSourceExtensions
    {
        /// <summary>
        /// Create a directory placeholder blob with the given prefix.
        /// </summary>
        /// <param name="self">The data source.</param>
        /// <param name="bucket">The bucket that owns the files.</param>
        /// <param name="prefix">The directory path.</param>
        public static async Task CreateDirectoryAsync(this GcsDataSource self, string bucket, string prefix)
        {
            await self.UploadStreamAsync(
                bucket: bucket,
                name: prefix,
                stream: Stream.Null,
                contentType: "application/x-www-form-urlencoded;charset=UTF-8");
        }

        /// <summary>
        /// Parses a diretory name into its steps.
        /// </summary>
        /// <param name="name">The directory name.</param>
        /// <returns>The <seealso cref="IEnumerable{String}"/> with the steps.</returns>
        public static IEnumerable<string> ParseDiretoryPath(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return Enumerable.Empty<string>();
            }
            return name.Substring(0, name.Length - 1).Split('/');
        }
    }
}
