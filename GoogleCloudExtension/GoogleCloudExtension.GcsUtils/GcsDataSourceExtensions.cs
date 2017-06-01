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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsUtils
{
    /// <summary>
    /// Useful extensions for the <seealso cref="GcsDataSource"/> that are specific to the GCS file browser.
    /// </summary>
    public static class GcsDataSourceExtensions
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
        /// Starts the upload operation using the data from the given operation.
        /// </summary>
        /// <param name="self">The data source.</param>
        /// <param name="operation">The operation to start.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        internal static void StartFileUploadOperation(
            this GcsDataSource self,
            GcsFileTransferOperation operation,
            CancellationToken cancellationToken)
        {
            self.StartFileUploadOperation(
                sourcePath: operation.LocalPath,
                bucket: operation.GcsItem.Bucket,
                name: operation.GcsItem.Name,
                operation: operation,
                token: cancellationToken);
        }

        /// <summary>
        /// Starts the download operation using the data from the given operation.
        /// </summary>
        /// <param name="self">The data source.</param>
        /// <param name="operation">The operation to start.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        internal static void StartFileDownloadOperation(
            this GcsDataSource self,
            GcsFileTransferOperation operation,
            CancellationToken cancellationToken)
        {
            self.StartFileDownloadOperation(
                bucket: operation.GcsItem.Bucket,
                name: operation.GcsItem.Name,
                destPath: operation.LocalPath,
                operation: operation,
                token: cancellationToken);
        }

        /// <summary>
        /// Starts the delete operation using the data from the given operation.
        /// </summary>
        /// <param name="self">The data source.</param>
        /// <param name="operation">The operation to start.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        internal static void StartDeleteOperation(
            this GcsDataSource self,
            GcsDeleteFileOperation operation,
            CancellationToken cancellationToken)
        {
            self.StartDeleteOperation(
                bucket: operation.GcsItem.Bucket,
                name: operation.GcsItem.Name,
                operation: operation,
                token: cancellationToken);
        }

        /// <summary>
        /// Starts the move operation using the data from the given operation.
        /// </summary>
        /// <param name="self">The data source.</param>
        /// <param name="operation">The operation to start.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        internal static void StartMoveOperation(
            this GcsDataSource self,
            GcsMoveFileOperation operation,
            CancellationToken cancellationToken)
        {

        }
    }
}
