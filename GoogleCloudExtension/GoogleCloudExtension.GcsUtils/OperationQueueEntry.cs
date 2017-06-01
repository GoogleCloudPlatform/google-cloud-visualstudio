using GoogleCloudExtension.DataSources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsUtils
{
    internal interface IOperationQueueEntry
    {
        IGcsFileOperationCallback OperationCallback { get; }

        Task ExecuteOperationAsync(CancellationToken cancellationToken);
    }

    internal class OperationQueueEntry<TOperation> : IOperationQueueEntry where TOperation : GcsOperation
    {

        /// <summary>
        /// The action to use to start the operation.
        /// </summary>
        public TOperation Operation { get; set; }

        /// <summary>
        /// The action to use to start the operation.
        /// </summary>
        public Action<TOperation, CancellationToken> StartOperationAction { get; set;  }

        /// <summary>
        /// The callback for the operation.
        /// </summary>
        IGcsFileOperationCallback IOperationQueueEntry.OperationCallback => Operation;

        /// <summary>
        /// Helper method that starts the operation.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns>Returns a task that will be completed once the operation completes.</returns>
        Task IOperationQueueEntry.ExecuteOperationAsync(CancellationToken cancellationToken)
        {
            StartOperationAction(Operation, cancellationToken);
            return Operation.AwaitOperationAsync();
        }
    }
}
