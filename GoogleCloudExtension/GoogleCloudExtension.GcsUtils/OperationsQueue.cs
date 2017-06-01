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
        private readonly int _maxConcurrentOperations;
        private readonly List<GcsOperation> _operations = new List<GcsOperation>();
        private readonly CancellationToken _cancellationToken;
        private readonly Queue<IOperationQueueEntry> _pendingOperations = new Queue<IOperationQueueEntry>();
        private readonly List<Task> _operationsInFlight = new List<Task>();
        private bool _schedulerActive = false;

        /// <summary>
        /// Returns the list of operations contained in the queue.
        /// </summary>
        public IReadOnlyList<GcsOperation> Operations => _operations;

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
        /// <param name="startOperationAction">The action to use to start the operaitons.</param>
        public void EnqueueOperations<TOperation>(
            IEnumerable<TOperation> operations,
            Action<TOperation, CancellationToken> startOperationAction) where TOperation: GcsOperation
        {
            var operationsSnapshot = operations.ToList();
            _operations.AddRange(operationsSnapshot);
            foreach (var operation in operationsSnapshot)
            {
                _pendingOperations.Enqueue(new OperationQueueEntry<TOperation>
                {
                    Operation = operation,
                    StartOperationAction = startOperationAction
                });
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
                    _operationsInFlight.Add(nextOperation.ExecuteOperationAsync(_cancellationToken));
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
                entry.OperationCallback.Cancelled();
            }
        }
    }
}
