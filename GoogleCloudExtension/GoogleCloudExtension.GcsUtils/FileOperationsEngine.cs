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
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsUtils
{
    /// <summary>
    /// This class manages the operations necessary to perform builk operations on GCS.
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
        /// <param name="cancellationToken">The cancellation token source to use for the operations.</param>
        /// <returns>The list of operations started.</returns>
        public OperationsQueue StartUploadOperations(
            IEnumerable<string> sources,
            string bucket,
            string bucketPath,
            CancellationToken cancellationToken)
        {
            var uploadOperations = CreateUploadOperations(sources, bucket: bucket, bucketPath: bucketPath);
            var operationsQueue = new OperationsQueue(cancellationToken);
            operationsQueue.EnqueueOperations(uploadOperations, _dataSource.StartFileUploadOperation);
            operationsQueue.StartOperations();
            return operationsQueue;
        }

        /// <summary>
        /// Starts a set of download operations for files and directories stored on GCS. It will enumerate
        /// all of the directories and subdirectories specified as well as all of the files stored.
        /// </summary>
        /// <param name="sources">The list of files or directories to download.</param>
        /// <param name="destinationDir">Where to store the downloaded files.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns>The list of operations started.</returns>
        public async Task<OperationsQueue> StartDownloadOperationsAsync(
            IEnumerable<GcsItemRef> sources,
            string destinationDir,
            CancellationToken cancellationToken)
        {
            List<GcsFileOperation> downloadOperations = new List<GcsFileOperation>(
                sources
                .Where(x => x.IsFile)
                .Select(x => new GcsFileOperation(
                    localPath: Path.Combine(destinationDir, GcsPathUtils.GetFileName(x.Name)),
                    gcsItem: x)));

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
                        localPath: absoluteFilePath,
                        gcsItem: file));

                    // Collects the list of directories to create.
                    subDirs.Add(Path.GetDirectoryName(absoluteFilePath));
                }
            }

            // Create all of the directories.
            foreach (var dir in subDirs)
            {
                Directory.CreateDirectory(dir);
            }

            var operationsQueue = new OperationsQueue(cancellationToken);
            operationsQueue.EnqueueOperations(downloadOperations, _dataSource.StartFileDownloadOperation);
            operationsQueue.StartOperations();
            return operationsQueue;
        }

        /// <summary>
        /// Enumerates all of the files and directories and starts the operations to delete them.
        /// </summary>
        /// <param name="sources">The list of sources to delete.</param>
        /// <param name="cancellationToken">The cancellation token to use for the operations.</param>
        /// <returns>The list of operations started.</returns>
        public async Task<OperationsQueue> StartDeleteOperationsAsync(
            IEnumerable<GcsItemRef> sources,
            CancellationToken cancellationToken)
        {
            List<GcsFileOperation> deleteOperations = new List<GcsFileOperation>(sources
                .Where(x => x.IsFile)
                .Select(x => new GcsFileOperation(x)));

            foreach (var dir in sources.Where(x => x.IsDirectory))
            {
                var filesInPrefix = await GetGcsFilesFromPrefixAsync(dir.Bucket, dir.Name);
                deleteOperations.AddRange(filesInPrefix.Select(x => new GcsFileOperation(x)));
            }

            var operationsQueue = new OperationsQueue(cancellationToken);
            operationsQueue.EnqueueOperations(deleteOperations, _dataSource.StartDeleteOperation);
            operationsQueue.StartOperations();
            return operationsQueue;
        }

        /// <summary>
        /// Creates the <seealso cref="GcsFileOperation"/> necessary to upload all of the sources from the local
        /// file system to GCS. The sources can be files or directories.
        /// </summary>
        static private IList<GcsFileOperation> CreateUploadOperations(
            IEnumerable<string> paths,
            string bucket,
            string bucketPath)
            => paths
               .Select(x => CreateUploadOPerationsForPath(bucket, bucketPath, x))
               .SelectMany(x => x)
               .ToList();

        private static IEnumerable<GcsFileOperation> CreateUploadOPerationsForPath(
            string bucket,
            string bucketPath,
            string src)
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
                            localPath: info.FullName,
                            gcsItem: new GcsItemRef(bucket: bucket, name: GcsPathUtils.Combine(bucketPath, info.Name)))
                   };
            }
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
            var fileOperations = Directory.EnumerateFiles(sourceDir)
                .Select(file => new GcsFileOperation(
                    localPath: file,
                    gcsItem: new GcsItemRef(
                        bucket: bucket,
                        name: GcsPathUtils.Combine(baseGcsPath, Path.GetFileName(file)))));
            var directoryOperations = Directory.EnumerateDirectories(sourceDir)
                .Select(subDir => CreateUploadOperationsForDirectory(
                    subDir,
                    bucket: bucket,
                    baseGcsPath: GcsPathUtils.Combine(baseGcsPath, Path.GetFileName(subDir))))
                .SelectMany(x => x);

            return Enumerable.Concat(fileOperations, directoryOperations);
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
        /// <param name="bucket">The bucket that contains the files.</param>
        /// <param name="prefixes">The prefixes to query.</param>
        /// <returns>A combined <seealso cref="IEnumerable{GcsFileReference}"/> with all of the files found.</returns>
        private async Task<IEnumerable<GcsItemRef>> GetGcsFilesFromPrefixesAsync(
            string bucket,
            IEnumerable<string> prefixes)
        {
            var filesInPrefixes = await Task.WhenAll(prefixes.Select(x => GetGcsFilesFromPrefixAsync(bucket, x)));
            return filesInPrefixes.SelectMany(x => x);
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
