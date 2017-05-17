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

using Google.Apis.Storage.v1.Data;
using GoogleCloudExtension.GcsUtils;
using GoogleCloudExtension.Utils;
using System.Linq;

namespace GoogleCloudExtension.GcsFileBrowser
{
    /// <summary>
    /// This class contains the flat data to be shown in the file browser.
    /// </summary>
    public class GcsRow
    {
        private const string NoValuePlaceholder = "-";
        private const string DefaultContentType = "application/octet-stream";

        /// <summary>
        /// The name of the bucket.
        /// </summary>
        public string Bucket { get; private set; }

        /// <summary>
        /// The name of the item, file or directory.
        /// </summary>
        public string BlobName { get; private set; }

        /// <summary>
        /// The leaf name in the path, i.e. "file.txt" if the path is "dir1/dir2/file.txt".
        /// </summary>
        public string LeafName { get; private set; }

        /// <summary>
        /// Whether this is an error message.
        /// </summary>
        public bool IsError { get; private set; }

        /// <summary>
        /// Whether this row represents a file.
        /// </summary>
        public bool IsFile { get; private set; }

        /// <summary>
        /// Whether this row represents a directory.
        /// </summary>
        public bool IsDirectory { get; private set; }

        /// <summary>
        /// The size of the item.
        /// </summary>
        public ulong Size { get; private set; }

        /// <summary>
        /// The size of the item formatted to be easy to read, 10KB, 100MB, etc...
        /// </summary>
        public string FormattedSize { get; private set; }

        /// <summary>
        /// The last modified date for the item.
        /// </summary>
        public string LastModified { get; private set; }

        /// <summary>
        /// The content type for the item.
        /// </summary>
        public string ContentType { get; private set; }

        /// <summary>
        /// The full gs://... path to the item.
        /// </summary>
        public string GcsPath => $"gs://{Bucket}/{BlobName}";

        public GcsRow() { }

        /// <summary>
        /// Creates the row given the directory name.
        /// </summary>
        /// <param name="bucket">The bucket that owns the directory.</param>
        /// <param name="name">The name (path) to the directory.</param>
        /// <returns>The newly created <seealso cref="GcsRow"/>.</returns>
        public static GcsRow CreateDirectoryRow(string bucket, string name) =>
            new GcsRow
            {
                Bucket = bucket,
                BlobName = name,
                LeafName = GcsPathUtils.GetFileName(name),
                FormattedSize = NoValuePlaceholder,
                LastModified = NoValuePlaceholder,
                ContentType = NoValuePlaceholder,
                IsDirectory = true,
            };

        /// <summary>
        /// Creates a file row from the given GCS <seealso cref="Object"/>.
        /// </summary>
        /// <param name="obj">The GCS <seealso cref="Object"/></param>
        /// <returns>The newly created <seealso cref="GcsRow"/>.</returns>
        public static GcsRow CreateFileRow(Object obj) =>
            new GcsRow
            {
                Bucket = obj.Bucket,
                BlobName = obj.Name,
                IsFile = true,
                Size = obj.Size.HasValue ? obj.Size.Value : 0l,
                FormattedSize = obj.Size.HasValue ? FormatSize(obj.Size.Value) : NoValuePlaceholder,
                LastModified = obj.Updated?.ToString() ?? NoValuePlaceholder,
                LeafName = GcsPathUtils.GetFileName(obj.Name),
                ContentType = obj.ContentType ?? DefaultContentType,
            };

        /// <summary>
        /// Creates a row that represents an error.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The newly created <seealso cref="GcsRow"/>.</returns>
        public static GcsRow CreateErrorRow(string message) =>
            new GcsRow
            {
                LeafName = message,
                IsError = true
            };

        /// <summary>
        /// Takes in the <paramref name="size"/> and returns a human readable version of the size
        /// with the KB, MB, etc... suffix.
        /// </summary>
        private static string FormatSize(ulong size)
        {
            return StringFormatUtils.FormatByteSize(size);
        }
    }
}