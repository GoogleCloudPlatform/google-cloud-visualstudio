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

using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsFileBrowser
{
    internal class GcsFileReference
    {
        public string Bucket { get; }

        public string Name { get; }

        public string RelativeName { get; }

        public GcsFileReference(string bucket, string name, string prefix)
        {
            Bucket = bucket;
            Name = name;
            RelativeName = GetRelativeName(name, prefix);
        }

        private static string GetRelativeName(string fullName, string prefix)
        {
            if (fullName.StartsWith(prefix))
            {
                return fullName.Substring(prefix.Length);
            }
            else
            {
                return fullName;
            }
        }
    }

    internal static class GcsDataSourceExtensions
    {
        public static async Task<IEnumerable<GcsFileReference>> GetGcsFilesFromPrefixAsync(
            this GcsDataSource self,
            string bucket,
            string prefix)
        {
            var files = await self.GetObjectLisAsync(bucket, prefix);
            return files.Select(x => new GcsFileReference(bucket, x.Name, prefix)).ToList();
        }

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

        public static async Task CreateDirectoryAsync(this GcsDataSource self, string bucket, string prefix)
        {
            await self.UploadStreamAsync(
                bucket: bucket,
                name: prefix,
                stream: Stream.Null,
                contentType: "application/x-www-form-urlencoded;charset=UTF-8");
        }

        public static IEnumerable<string> ParsePath(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return Enumerable.Empty<string>();
            }
            return name.Substring(0, name.Length - 1).Split('/');
        }

        private static string GetNameLeaf(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return name;
            }

            var cleanName = name.Substring(0, name.Length - 1);
            return cleanName.Split('/').LastOrDefault() ?? "";
        }
    }
}
