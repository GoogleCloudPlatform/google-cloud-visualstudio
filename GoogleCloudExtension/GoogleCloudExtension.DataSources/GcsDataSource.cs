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

using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Storage.v1;
using Google.Apis.Storage.v1.Data;
using Google.Apis.Upload;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources
{
    /// <summary>
    /// Data source that returns information about Google Cloud Storage buckets for a particular project and credentials.
    /// </summary>
    public class GcsDataSource : DataSourceBase<StorageService>
    {
        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="credential"></param>
        /// <param name="appName"></param>
        public GcsDataSource(string projectId, GoogleCredential credential, string appName)
            : base(projectId, credential, init => new StorageService(init), appName)
        { }

        /// <summary>
        /// Fetches the list of buckets for the given project.
        /// </summary>
        /// <returns>The list of buckets.</returns>
        public Task<IList<Bucket>> GetBucketListAsync()
        {
            return LoadPagedListAsync(
                (token) =>
                {
                    if (String.IsNullOrEmpty(token))
                    {
                        Debug.WriteLine($"{nameof(GcsDataSource)}, {nameof(GetBucketListAsync)}: Fetching first page.");
                        return Service.Buckets.List(ProjectId).ExecuteAsync();
                    }
                    else
                    {
                        Debug.WriteLine($"{nameof(GcsDataSource)}, {nameof(GetBucketListAsync)}: Fetching page: {token}");
                        var request = Service.Buckets.List(ProjectId);
                        request.PageToken = token;
                        return request.ExecuteAsync();
                    }
                },
                x => x.Items,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Lists all of the objects that exist under the given <paramref name="prefix"/>.
        /// </summary>
        /// <param name="bucket">The bucket that owns the objects.</param>
        /// <param name="prefix">The prefix to start looking, can be null if no prefix is to be used.</param>
        /// <returns>A list of all of the objects found, can be empty.</returns>
        public async Task<IEnumerable<Google.Apis.Storage.v1.Data.Object>> GetObjectLisAsync(string bucket, string prefix)
        {
            var request = Service.Objects.List(bucket);
            request.Prefix = prefix;

            return await LoadPagedListAsync(
                (token) =>
                {
                    request.PageToken = token;
                    return request.ExecuteAsync();
                },
                x => x.Items,
                x => x.NextPageToken);
        }

        /// <summary>
        /// Lists all of the objects "directly" under the given <paramref name="prefix"/> together with all of the
        /// sub-prefixes directly under the same <paramref name="prefix."/>. This method acts as if listing a directory
        /// in the file system.
        /// </summary>
        /// <param name="bucket">The bucket that owns the files.</param>
        /// <param name="prefix">The prefix to look for, can be null or empty.</param>
        /// <returns>The "directory" defined by <paramref name="prefix"/>.</returns>
        public async Task<GcsDirectory> GetDirectoryListAsync(string bucket, string prefix)
        {
            var request = Service.Objects.List(bucket);
            request.Prefix = prefix;
            request.Delimiter = "/";

            try
            {
                string pageToken = null;
                List<string> prefixes = new List<string>();
                List<Google.Apis.Storage.v1.Data.Object> items = new List<Google.Apis.Storage.v1.Data.Object>();
                do
                {
                    request.PageToken = pageToken;
                    var response = await request.ExecuteAsync();

                    if (response.Prefixes != null)
                    {
                        prefixes.AddRange(response.Prefixes);
                    }

                    if (response.Items != null)
                    {
                        items.AddRange(response.Items);
                    }

                    pageToken = response.NextPageToken;
                } while (!String.IsNullOrEmpty(pageToken));

                return new GcsDirectory(items, prefixes);
            }
            catch (GoogleApiException ex)
            {
                Debug.WriteLine($"Failed to read GCS directory {prefix} in bucket {bucket}");
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Uploads the given <paramref name="stream"/> to the <paramref name="bucket"/> with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="bucket">The bucket that will own the file.</param>
        /// <param name="name">The name to use.</param>
        /// <param name="stream">The stream with the contents.</param>
        /// <param name="contentType">The content type to use, optional.</param>
        public async Task UploadStreamAsync(string bucket, string name, Stream stream, string contentType = null)
        {
            try
            {
                var request = Service.Objects.Insert(
                    new Google.Apis.Storage.v1.Data.Object
                    {
                        Name = name,
                        Size = (ulong)stream.Length,
                        ContentType = contentType,
                    },
                    bucket,
                    stream,
                    null);
                await request.UploadAsync();
            }
            catch (GoogleApiException ex)
            {
                throw new DataSourceException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Starts a file upload operation reporting the status and progress to the given <paramref name="operation"/>.
        /// </summary>
        /// <param name="sourcePath">The path to the file to open, should be a full path.</param>
        /// <param name="bucket">The bucket that will own the file.</param>
        /// <param name="name">The name to use.</param>
        /// <param name="operation">The operation that will receive the status and progress notifications.</param>
        /// <param name="token">The cancellation token to cancel the operation.</param>
        public async void StartFileUploadOperation(
            string sourcePath,
            string bucket,
            string name,
            IGcsFileOperationCallback operation,
            CancellationToken token)
        {
            try
            {
                using (var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
                {
                    var totalSize = (ulong)stream.Length;
                    var request = Service.Objects.Insert(
                        new Google.Apis.Storage.v1.Data.Object
                        {
                            Name = name,
                            Size = totalSize,
                        },
                        bucket,
                        stream,
                        null);
                    request.ProgressChanged += (p) => OnUploadProgress(p, totalSize, operation);
                    var response = await request.UploadAsync(token);
                    operation.Completed();
                }
            }
            catch (IOException ex)
            {
                operation.Error(new DataSourceException(ex.Message, ex));
            }
            catch (GoogleApiException ex)
            {
                operation.Error(new DataSourceException(ex.Message, ex));
            }
            catch (TaskCanceledException ex)
            {
                operation.Cancelled();
            }
        }

        /// <summary>
        /// Starts a file download operation, reporting the status and progress to the given <paramref name="operation"/>.
        /// </summary>
        /// <param name="bucket">The bucket that owns the file.</param>
        /// <param name="name">The file name.</param>
        /// <param name="destPath">Where to save the file, this should be a full path.</param>
        /// <param name="operation">The operation that will receive the status and progress notifications.</param>
        /// <param name="token">The cancellation token to cancel the operation.</param>
        public async void StartFileDownloadOperation(
            string bucket,
            string name,
            string destPath,
            IGcsFileOperationCallback operation,
            CancellationToken token)
        {
            try
            {
                using (var stream = new FileStream(destPath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    // Find out the total size of the file to download.
                    var request = Service.Objects.Get(bucket, name);
                    var obj = await request.ExecuteAsync(token);
                    ulong totalSize = obj.Size ?? 0;

                    // Hookup the progress indicator.
                    request.MediaDownloader.ProgressChanged += (p) => OnDownloadProgress(p, totalSize, operation);
                    var response = await request.DownloadAsync(stream, token);
                    operation.Completed();
                }
            }
            catch (IOException ex)
            {
                operation.Error(new DataSourceException(ex.Message, ex));
            }
            catch (GoogleApiException ex)
            {
                operation.Error(new DataSourceException(ex.Message, ex));
            }
            catch (TaskCanceledException)
            {
                operation.Cancelled();
            }
        }

        /// <summary>
        /// Starts a delete operation, sending the notifications to the given <paramref name="operation"/>.
        /// </summary>
        /// <param name="bucket">The bucket that owns the file.</param>
        /// <param name="name">The name of the file.</param>
        /// <param name="operation">The operation that will recieve the status.</param>
        /// <param name="token">The cancellation token to cancel the operation.</param>
        public async void StartDeleteOperation(string bucket, string name, IGcsFileOperationCallback operation, CancellationToken token)
        {
            try
            {
                var response = await Service.Objects.Delete(bucket, name).ExecuteAsync();
                operation.Completed();
            }
            catch (GoogleApiException ex)
            {
                operation.Error(new DataSourceException(ex.Message, ex));
            }
            catch (TaskCanceledException)
            {
                operation.Cancelled();
            }
        }

        private void OnDownloadProgress(
            IDownloadProgress downloadProgress,
            ulong totalSize,
            IGcsFileOperationCallback operation)
        {
            operation.Progress((double)downloadProgress.BytesDownloaded / totalSize);
        }

        private static void OnUploadProgress(
            IUploadProgress uploadProgress,
            ulong totalSize,
            IGcsFileOperationCallback operation)
        {
            operation.Progress((double)uploadProgress.BytesSent / totalSize);
        }
    }
}