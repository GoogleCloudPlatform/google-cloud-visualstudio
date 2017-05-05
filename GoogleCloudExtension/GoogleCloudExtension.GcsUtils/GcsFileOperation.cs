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
using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GoogleCloudExtension.GcsUtils
{
    /// <summary>
    /// This class represents an operation in flight for the GCS file browser.
    /// </summary>
    public class GcsFileOperation : Model, IGcsFileOperationCallback
    {
        private readonly SynchronizationContext _context;
        private double _progress = 0;
        private bool _isError;
        private bool _isCancelled;

        /// <summary>
        /// The current progress of the operation, between 0.0 (started) to 1.0 (completed).
        /// </summary>
        public double Progress
        {
            get { return _progress; }
            private set { SetValueAndRaise(ref _progress, value); }
        }

        /// <summary>
        /// Whether this operation is in error.
        /// </summary>
        public bool IsError
        {
            get { return _isError; }
            private set { SetValueAndRaise(ref _isError, value); }
        }

        public bool IsCancelled
        {
            get { return _isCancelled; }
            private set { SetValueAndRaise(ref _isCancelled, value); }
        }

        /// <summary>
        /// The path to the source of the operation.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// The name of the source, usually last element in the path.
        /// </summary>
        public string SourceName => Path.GetFileName(Source);

        /// <summary>
        /// The name of the bucket where this operation applies.
        /// </summary>
        public string Bucket { get; }

        /// <summary>
        /// The name of the destination for the operation.
        /// </summary>
        public string Destination { get; }

        /// <summary>
        /// Event raised when the operation completes.
        /// </summary>
        public event EventHandler Completed;

        public GcsFileOperation(
            string source,
            string bucket,
            string destination = null)
        {
            _context = SynchronizationContext.Current;

            Source = source;
            Bucket = bucket;
            Destination = destination;
        }

        #region IGcsFileOperation implementation.

        void IGcsFileOperationCallback.Progress(double value)
        {
            _context.Send((x) => Progress = value, null);
        }

        void IGcsFileOperationCallback.Completed()
        {
            _context.Send((x) =>
            {
                Progress = 1.0;
                Completed?.Invoke(this, EventArgs.Empty);
            }, null);
        }

        void IGcsFileOperationCallback.Cancelled()
        {
            Debug.WriteLine($"Operation or {Source} cancelled.");
            _context.Send((x) =>
            {
                Progress = 0.0;
                Completed?.Invoke(this, EventArgs.Empty);
            }, null);
        }

        void IGcsFileOperationCallback.Error(DataSourceException ex)
        {
            IsError = true;
        }

        #endregion
    }
}
