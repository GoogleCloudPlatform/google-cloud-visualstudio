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
        public Action<TOperation, CancellationToken> StartOperationAction { get; set; }

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
