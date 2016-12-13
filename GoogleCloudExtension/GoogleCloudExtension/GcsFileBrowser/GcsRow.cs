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

using Google.Apis.Storage.v1.Data;
using System.Linq;

namespace GoogleCloudExtension.GcsFileBrowser
{
    public class GcsRow
    {
        public string Bucket { get; private set; }

        public string Name { get; private set; }

        public string FileName { get; private set; }

        public bool IsError { get; private set; }

        public bool IsFile { get; private set; }

        public bool IsDirectory { get; private set; }

        public ulong Size { get; private set; }

        public string LastModified { get; private set; }

        public string ContentType { get; private set; }

        public string GcsPath => $"gs://{Bucket}/{Name}";

        public GcsRow() { }

        public static GcsRow CreateDirectoryRow(string bucket, string name) =>
            new GcsRow
            {
                Bucket = bucket,
                Name = name,
                FileName = GetLeafName(name),
                IsDirectory = true,
            };

        public static GcsRow CreateFileRow(Object obj) =>
            new GcsRow
            {
                Bucket = obj.Bucket,
                Name = obj.Name,
                IsFile = true,
                Size = obj.Size.HasValue ? obj.Size.Value : 0ul,
                LastModified = obj.Updated?.ToString() ?? "Unknown",
                FileName = GetLeafName(obj.Name),
                ContentType = obj.ContentType ?? "application/octet-stream",
            };

        public static GcsRow CreateErrorRow(string message) =>
            new GcsRow
            {
                FileName = message,
                IsError = true
            };

        private static string GetLeafName(string name)
        {
            var cleanName = name.Last() == '/' ? name.Substring(0, name.Length - 1) : name;
            return cleanName.Split('/').Last();
        }
    }
}