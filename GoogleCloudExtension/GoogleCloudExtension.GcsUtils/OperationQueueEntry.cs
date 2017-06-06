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
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GcsUtils
{
    /// <summary>
    /// Interface for a queue entry.
    /// </summary>
    internal interface IOperationQueueEntry
    {
        /// <summary>
        /// The callback to use for the operation.
        /// </summary>
        IGcsFileOperationCallback OperationCallback { get; }

        /// <summary>
        /// Starts the operation, returns the task that will completed when the operation is completed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns>A task that will be completed when the operation is finished. No error will be reported
        /// through this task.</returns>
        Task ExecuteOperationAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// A concrete operation entry for the given <typeparamref name="TOperation"/> type.
    /// </summary>
    /// <typeparam name="TOperation">The ype of operation queued, must derive from <seealso cref="GcsOperation"/></typeparam>
    internal class OperationQueueEntry<TOperation> : IOperationQueueEntry where TOperation : GcsOperation
    {
        /// <summary>
        /// The action to use to start the operation.
        /// </summary>
        public TOperation Operation { get; set; }

        /// <summary>
        /// The action to use to start the operation.
        /// </summary>
        public Action<TOperation, CancellationToken> StartOperationAction { get; set; }

        #region IOperationQueueEntry implementation.

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

        #endregion
    }
}
