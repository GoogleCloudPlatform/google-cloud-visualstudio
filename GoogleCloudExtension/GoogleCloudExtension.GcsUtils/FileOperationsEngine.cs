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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsUtils
{
    /// <summary>
    /// This class manages the operations necessary to copy files from/to GCS buckets.
    /// </summary>
    public class FileOperationsEngine
    {
        private readonly GcsDataSource _dataSource;

        public FileOperationsEngine(GcsDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        /// <summary>
        /// Starts the file upload operations for the given file sources, does not wait for the operations to complete.
        /// </summary>
        /// <param name="sources">The files, or directories, to upload.</param>
        /// <param name="bucket">The destination bucket.</param>
        /// <param name="bucketPath">The destination path in the bucket.</param>
        /// <param name="cancellationTokenSource">The cancellation token source to use for the operations.</param>
        /// <returns></returns>
        public IList<GcsFileOperation> StartUploadOperations(
            IEnumerable<string> sources,
            string bucket,
            string bucketPath,
            CancellationToken cancellationToken)
        {
            var uploadOperations = CreateUploadOperations(sources, bucket: bucket, bucketPath: bucketPath);
            foreach (var operation in uploadOperations)
            {
                _dataSource.StartFileUploadOperation(
                    sourcePath: operation.Source,
                    bucket: operation.Bucket,
                    name: operation.Destination,
                    operation: operation,
                    token: cancellationToken);
            }
            return uploadOperations;
        }

        /// <summary>
        /// Starts a set of download operations for files and directories stored on GCS. It will enumerate
        /// all of the directories and subdirectories specified as well as all of the files stored.
        /// </summary>
        /// <param name="sources">The list of files or directories to download.</param>
        /// <param name="destinationDir">Where to store the downloaded files.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns></returns>
        public async Task<IList<GcsFileOperation>> StartDownloadOperationsAsync(
            IEnumerable<GcsItemRef> sources,
            string destinationDir,
            CancellationToken cancellationToken)
        {
            List<GcsFileOperation> downloadOperations = new List<GcsFileOperation>();

            downloadOperations.AddRange(sources
                .Where(x => x.IsFile)
                .Select(x => new GcsFileOperation(
                    source: x.Name,
                    bucket: x.Bucket,
                    destination: Path.Combine(destinationDir, GcsPathUtils.GetFileName(x.Name)))));

            // Collect all of the files from all of the directories to download, collects all of the
            // local file system directories that need to be created to mirror the source structure.
            var subDirs = new HashSet<string>();
            foreach (var dir in sources.Where(x => x.IsDirectory))
            {
                var files = await GetGcsFilesFromPrefixAsync(dir.Bucket, dir.Name);
                foreach (var file in files)
                {
                    var relativeFilePath = Path.Combine(
                        GcsPathUtils.GetFileName(dir.Name),
                        GetRelativeName(file.Name, dir.Name).Replace('/', '\\'));
                    var absoluteFilePath = Path.Combine(destinationDir, relativeFilePath);

                    // Create the file operation for this file.
                    downloadOperations.Add(new GcsFileOperation(
                        source: file.Name,
                        bucket: file.Bucket,
                        destination: absoluteFilePath));

                    // Collects the list of directories to create.
                    subDirs.Add(Path.GetDirectoryName(absoluteFilePath));
                }
            }

            // Create all of the directories.
            foreach (var dir in subDirs)
            {
                Directory.CreateDirectory(dir);
            }

            // Start all of the download operations.
            foreach (var operation in downloadOperations)
            {
                _dataSource.StartFileDownloadOperation(
                    bucket: operation.Bucket,
                    name: operation.Source,
                    destPath: operation.Destination,
                    operation: operation,
                    token: cancellationToken);
            }

            return downloadOperations;
        }

        public async Task<IList<GcsFileOperation>> StartDeleteOperationsAsync(
            IEnumerable<GcsItemRef> sources,
            CancellationToken cancellationToken)
        {
            List<GcsFileOperation> deleteOperations = new List<GcsFileOperation>();

            deleteOperations.AddRange(sources
                .Where(x => x.IsFile)
                .Select(x => new GcsFileOperation(
                    source: x.Name,
                    bucket: x.Bucket)));

            foreach (var dir in sources.Where(x => x.IsDirectory))
            {
                var filesInPrefix = await GetGcsFilesFromPrefixAsync(dir.Bucket, dir.Name);
                deleteOperations.AddRange(filesInPrefix
                    .Select(x => new GcsFileOperation(source: x.Name, bucket: x.Bucket)));
            }

            foreach (var operation in deleteOperations)
            {
                _dataSource.StartDeleteOperation(
                    bucket: operation.Bucket,
                    name: operation.Source,
                    operation: operation,
                    token: cancellationToken);
            }

            return deleteOperations;
        }

        /// <summary>
        /// Creates the <seealso cref="GcsFileOperation"/> necessary to upload all of the sources from the local
        /// file system to GCS. The sources can be files or directories.
        /// </summary>
        /// <param name="sources">The list of local files or directories to upload.</param>
        /// <returns></returns>
        static private IList<GcsFileOperation> CreateUploadOperations(IEnumerable<string> sources, string bucket, string bucketPath)
        {
            return sources
               .Select(src =>
               {
                   var info = new FileInfo(src);
                   var isDirectory = (info.Attributes & FileAttributes.Directory) != 0;

                   if (isDirectory)
                   {
                       return CreateUploadOperationsForDirectory(
                           sourceDir: info.FullName,
                           bucket: bucket,
                           baseGcsPath: GcsPathUtils.Combine(bucketPath, info.Name));
                   }
                   else
                   {
                       return new GcsFileOperation[]
                          {
                            new GcsFileOperation(
                                source: info.FullName,
                                bucket: bucket,
                                destination: GcsPathUtils.Combine(bucketPath, info.Name)),
                          };
                   }
               })
               .SelectMany(x => x)
               .ToList();
        }

        /// <summary>
        /// Creates the <seealso cref="GcsFileOperation"/> instances for all of the files in the given directory. The
        /// target directory will be based on <paramref name="baseGcsPath"/>.
        /// </summary>
        /// <param name="sourceDir">The local dir to process.</param>
        /// <param name="bucket">The name of the bucket.</param>
        /// <param name="baseGcsPath">The base gcs path where to copy the files.</param>
        static private IEnumerable<GcsFileOperation> CreateUploadOperationsForDirectory(
            string sourceDir,
            string bucket,
            string baseGcsPath)
        {
            return Enumerable.Concat(
                Directory.EnumerateFiles(sourceDir)
                    .Select(file => new GcsFileOperation(
                        source: file,
                        bucket: bucket,
                        destination: GcsPathUtils.Combine(baseGcsPath, Path.GetFileName(file)))),
                Directory.EnumerateDirectories(sourceDir)
                    .Select(subDir => CreateUploadOperationsForDirectory(
                        subDir,
                        bucket: bucket,
                        baseGcsPath: GcsPathUtils.Combine(baseGcsPath, Path.GetFileName(subDir))))
                    .SelectMany(x => x));
        }

        /// <summary>
        /// Get all files that are descendants of the given <paramref name="prefix"/> in the given <paramref name="bucket"/>.
        /// </summary>
        /// <param name="bucket">The bucket that contains the files.</param>
        /// <param name="prefix">The prefix (or directory) under which files are requiested.</param>
        /// <returns>An <seealso cref="IEnumerable{GcsFileReference}"/> with all of the files found.</returns>
        private async Task<IEnumerable<GcsItemRef>> GetGcsFilesFromPrefixAsync(string bucket, string prefix)
        {
            var files = await _dataSource.GetObjectLisAsync(bucket, prefix);
            return files.Select(x => new GcsItemRef(bucket, x.Name)).ToList();
        }

        /// <summary>
        /// Utility method to get all of the files given the list of <paramref name="prefixes"/>.
        /// </summary>
        /// <param name="self">The data source.</param>
        /// <param name="bucket">The bucket that contains the files.</param>
        /// <param name="prefixes">The prefixes to query.</param>
        /// <returns>A combined <seealso cref="IEnumerable{GcsFileReference}"/> with all of the files found.</returns>
        private async Task<IEnumerable<GcsItemRef>> GetGcsFilesFromPrefixesAsync(
            string bucket,
            IEnumerable<string> prefixes)
        {
            var result = new List<GcsItemRef>();
            foreach (var prefix in prefixes)
            {
                result.AddRange(await GetGcsFilesFromPrefixAsync(bucket, prefix));
            }
            return result;
        }

        /// <summary>
        /// Returns the relative name of <paramref name="fullName"/> within the <paramref name="prefix"/> given.
        /// For example, for fullName=a/b/c/d and prefix=a/b/ the value will be c/d.
        /// </summary>
        /// <param name="fullName">The full name to process.</param>
        /// <param name="prefix">The prefix to remove.</param>
        private string GetRelativeName(string fullName, string prefix)
        {
            return fullName.StartsWith(prefix) ? fullName.Substring(prefix.Length) : fullName;
        }
    }
}
