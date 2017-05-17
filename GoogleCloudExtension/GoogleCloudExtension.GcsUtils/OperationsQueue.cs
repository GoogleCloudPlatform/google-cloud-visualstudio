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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsUtils
{
    /// <summary>
    /// This class represents a queue of operaations that are to be completed. This class will
    /// ensure that that the right concurrency level is maintained and no more than the specified
    /// number of operations are started at any given time. If the overall operation is cancelled any
    /// non started operations will be cancelled as well.
    /// </summary>
    public class OperationsQueue
    {
        /// <summary>
        /// Each entry in the queue connects the operation to be started with the code to start it.
        /// </summary>
        private class OperationQueueEntry
        {
            /// <summary>
            /// The operation to be started.
            /// </summary>
            public GcsFileOperation Operation { get; set; }

            /// <summary>
            /// The action to use to start the operation.
            /// </summary>
            public Action<GcsFileOperation, CancellationToken> Action { get; set; }

            /// <summary>
            /// Helper method that starts the operation.
            /// </summary>
            /// <param name="cancellationToken">The cancellation token for the operation.</param>
            /// <returns>Returns a task that will be completed once the operation completes.</returns>
            public Task StartOperationAsync(CancellationToken cancellationToken)
            {
                Action(Operation, cancellationToken);
                return Operation.AwaitOperationAsync();
            }
        }

        private readonly int _maxConcurrentOperations;
        private readonly List<GcsFileOperation> _operations = new List<GcsFileOperation>();
        private readonly CancellationToken _cancellationToken;
        private readonly Queue<OperationQueueEntry> _pendingOperations = new Queue<OperationQueueEntry>();
        private readonly List<Task> _operationsInFlight = new List<Task>();
        private bool _schedulerActive = false;

        /// <summary>
        /// Returns the list of operations contained in the queue.
        /// </summary>
        public IReadOnlyList<GcsFileOperation> Operations => _operations;

        public OperationsQueue(CancellationToken cancellationToken)
            : this(cancellationToken, ServicePointManager.DefaultConnectionLimit)
        { }

        public OperationsQueue(CancellationToken cancellationToken, int maxConcurrentOperations)
        {
            _maxConcurrentOperations = maxConcurrentOperations;
            _cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Queue the given list of <paramref name="operations"/> but does not start them yet.
        /// </summary>
        /// <param name="operations">The operations to start.</param>
        /// <param name="queueAction">The action to use to start the operaitons.</param>
        public void QueueOperations(
            IEnumerable<GcsFileOperation> operations,
            Action<GcsFileOperation, CancellationToken> queueAction)
        {
            _operations.AddRange(operations);
            foreach (var operation in operations)
            {
                _pendingOperations.Enqueue(new OperationQueueEntry { Action = queueAction, Operation = operation });
            }
        }

        /// <summary>
        /// Starts the processing of the queue.
        /// </summary>
        public void StartOperations()
        {
            RunOperationScheduler();
        }

        private async void RunOperationScheduler()
        {
            if (_schedulerActive)
            {
                return;
            }

            try
            {
                _schedulerActive = true;
                Debug.WriteLine("Staring operations scheduler.");
                while (_pendingOperations.Count > 0)
                {
                    // Check if the user cancelled the operation.
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        Debug.WriteLine("The operations were cancelled.");
                        CancelPendingOperations();
                        break;
                    }

                    // If we have maxed out the number of concurrent operations, wait for any of the
                    // operations to complete before staring a new one.
                    if (_operationsInFlight.Count >= _maxConcurrentOperations)
                    {
                        Debug.WriteLine("Operations in flight full, waiting for operation to complete.");
                        var completed = await Task.WhenAny(_operationsInFlight);
                        _operationsInFlight.Remove(completed);
                    }

                    // Starts the next operation in the queue.
                    Debug.WriteLine("Queueing next operation.");
                    var nextOperation = _pendingOperations.Dequeue();
                    _operationsInFlight.Add(nextOperation.StartOperationAsync(_cancellationToken));
                }
                Debug.WriteLine("Ending operations scheduler.");
            }
            finally
            {
                _schedulerActive = false;
            }
        }

        private void CancelPendingOperations()
        {
            foreach (var entry in _pendingOperations)
            {
                var callback = entry.Operation as IGcsFileOperationCallback;
                callback.Cancelled();
            }
        }
    }
}
