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
    public class FileCopyEngine
    {
        private readonly GcsDataSource _dataSource;

        public FileCopyEngine(GcsDataSource dataSource)
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
            CancellationTokenSource cancellationTokenSource)
        {
            var uploadOperations = CreateUploadOperations(sources, bucket: bucket, bucketPath: bucketPath);
            foreach (var operation in uploadOperations)
            {
                _dataSource.StartFileUploadOperation(
                    sourcePath: operation.Source,
                    bucket: operation.Bucket,
                    name: operation.Destination,
                    operation: operation,
                    token: cancellationTokenSource.Token);
            }
            return uploadOperations;
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
                           baseGcsPath: GcsPathJoin(bucketPath, info.Name));
                   }
                   else
                   {
                       return new GcsFileOperation[]
                          {
                            new GcsFileOperation(
                                source: info.FullName,
                                bucket: bucket,
                                destination: GcsPathJoin(bucketPath, info.Name)),
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
                        destination: GcsPathJoin(baseGcsPath, Path.GetFileName(file)))),
                Directory.EnumerateDirectories(sourceDir)
                    .Select(subDir => CreateUploadOperationsForDirectory(
                        subDir,
                        bucket: bucket,
                        baseGcsPath: GcsPathJoin(baseGcsPath, Path.GetFileName(subDir))))
                    .SelectMany(x => x));
        }

        static private string GcsPathJoin(string root, string child)
        {
            StringBuilder result = new StringBuilder(root);
            if (!String.IsNullOrEmpty(root) &&  root.Last() != '/')
            {
                result.Append('/');
            }
            result.Append(child);
            return result.ToString();
        }

    }
}
