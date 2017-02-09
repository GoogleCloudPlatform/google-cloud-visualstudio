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

using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsFileBrowser
{
    /// <summary>
    /// This class is a reference to a file stored in a GCS bucket.
    /// </summary>
    internal class GcsFileReference
    {
        /// <summary>
        /// The name of the bucket.
        /// </summary>
        public string Bucket { get; }

        /// <summary>
        /// The name (or full path) of the file within the bucket.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The relative name of the file under a given prefix. Useful for when downloading/uploading
        /// files from/to the bucket.
        /// </summary>
        public string RelativeName { get; }

        public GcsFileReference(string bucket, string name, string prefix)
        {
            Bucket = bucket;
            Name = name;
            RelativeName = GetRelativeName(name, prefix);
        }

        private static string GetRelativeName(string fullName, string prefix) =>
            fullName.StartsWith(prefix) ? fullName.Substring(prefix.Length) : fullName;
    }

    /// <summary>
    /// Useful extensions for the <seealso cref="GcsDataSource"/> that are specific to the GCS file browser.
    /// </summary>
    internal static class GcsDataSourceExtensions
    {
        /// <summary>
        /// Get all files that are descendants of the given <paramref name="prefix"/> in the given <paramref name="bucket"/>.
        /// </summary>
        /// <param name="self">The data source.</param>
        /// <param name="bucket">The bucket that contains the files.</param>
        /// <param name="prefix">The prefix (or directory) under which files are requiested.</param>
        /// <returns>An <seealso cref="IEnumerable{GcsFileReference}"/> with all of the files found.</returns>
        public static async Task<IEnumerable<GcsFileReference>> GetGcsFilesFromPrefixAsync(
            this GcsDataSource self,
            string bucket,
            string prefix)
        {
            var files = await self.GetObjectLisAsync(bucket, prefix);
            return files.Select(x => new GcsFileReference(bucket, x.Name, prefix)).ToList();
        }

        /// <summary>
        /// Utility method to get all of the files given the list of <paramref name="prefixes"/>.
        /// </summary>
        /// <param name="self">The data source.</param>
        /// <param name="bucket">The bucket that contains the files.</param>
        /// <param name="prefixes">The prefixes to query.</param>
        /// <returns>A combined <seealso cref="IEnumerable{GcsFileReference}"/> with all of the files found.</returns>
        public static async Task<IEnumerable<GcsFileReference>> GetGcsFilesFromPrefixesAsync(
            this GcsDataSource self,
            string bucket,
            IEnumerable<string> prefixes)
        {
            var result = new List<GcsFileReference>();
            foreach (var prefix in prefixes)
            {
                result.AddRange(await self.GetGcsFilesFromPrefixAsync(bucket, prefix));
            }
            return result;
        }

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
