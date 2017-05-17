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
    public class OperationsQueue
    {
        private class OperationQueueEntry
        {
            public GcsFileOperation Operation { get; set; }

            public Action<GcsFileOperation, CancellationToken> Action { get; set; }

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

        public IReadOnlyList<GcsFileOperation> Operations => _operations;

        public OperationsQueue(CancellationToken cancellationToken)
            : this(cancellationToken, ServicePointManager.DefaultConnectionLimit)
        { }

        public OperationsQueue(CancellationToken cancellationToken, int maxConcurrentOperations)
        {
            _maxConcurrentOperations = maxConcurrentOperations;
            _cancellationToken = cancellationToken;
        }

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
