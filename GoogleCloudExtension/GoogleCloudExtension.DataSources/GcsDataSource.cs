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

        public async void StartUploadOperation(
            string sourcePath,
            string bucket,
            string name,
            IGcsFileOperation operation,
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
            catch (GoogleApiException ex)
            {
                operation.Error(new DataSourceException(ex.Message, ex));
            }
            catch (TaskCanceledException)
            {
                operation.Cancelled();
            }
        }

        public async void StartDownloadOperation(
            string bucket,
            string name,
            string destPath,
            IGcsFileOperation operation,
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
            catch (GoogleApiException ex)
            {
                operation.Error(new DataSourceException(ex.Message, ex));
            }
            catch (TaskCanceledException)
            {
                operation.Cancelled();
            }
        }

        public async void StartDeleteOperation(string bucket, string name, IGcsFileOperation operation, CancellationToken token)
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
            IGcsFileOperation operation)
        {
            operation.Progress((double)downloadProgress.BytesDownloaded / totalSize);
        }

        private static void OnUploadProgress(
            IUploadProgress uploadProgress,
            ulong totalSize,
            IGcsFileOperation operation)
        {
            operation.Progress((double)uploadProgress.BytesSent / totalSize);
        }
    }
}