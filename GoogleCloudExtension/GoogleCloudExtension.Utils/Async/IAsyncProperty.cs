// Copyright 2018 Google Inc. All Rights Reserved.
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

using System.ComponentModel;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils.Async
{
    public interface IAsyncProperty<out T> where T : Task
    {
        /// <summary>
        /// A task that succeeds when the actual task completes. This task will never throw.
        /// </summary>
        Task SafeTask { get; }

        /// <summary>
        /// The actual task sent to the property.
        /// </summary>
        T ActualTask { get; }

        /// <summary>
        /// Returns whether the wrapped task is still pending.
        /// </summary>
        bool IsPending { get; }

        /// <summary>
        /// Returns true if the wrapped task is completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// True if the task completed without cancelation or exception.
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// True if the task ended execution due to being canceled.
        /// </summary>
        bool IsCanceled { get; }

        /// <summary>
        /// Returns true if the wrapped task is in error.
        /// </summary>
        bool IsError { get; }

        /// <summary>
        /// Gets the relevant exception, if it exists.
        /// </summary>
        string ErrorMessage { get; }

        event PropertyChangedEventHandler PropertyChanged;
    }
}