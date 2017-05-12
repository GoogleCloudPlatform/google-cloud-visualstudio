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

using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils.Async
{
    /// <summary>
    /// Base class for modeling a task. Implementations should NotifyAllPropertyChanged when the task completes.
    /// </summary>
    public abstract class AsyncPropertyBase : Model
    {
        /// <summary>
        /// The task to trigger a nodify on completion.
        /// </summary>
        protected abstract Task Task { get; }

        /// <summary>
        /// Returns true if the wrapped task is in error.
        /// </summary>
        public bool IsError => Task?.IsFaulted ?? false;

        /// <summary>
        /// Gets the relevant exception, if it exists.
        /// </summary>
        public string ErrorMessage =>
            Task.Exception?.InnerException?.Message ??
            Task.Exception?.InnerExceptions?.FirstOrDefault()?.Message ??
            Task.Exception?.Message;

        /// <summary>
        /// True if the task ended execution due to being canceled.
        /// </summary>
        public bool IsCanceled => Task?.IsCanceled ?? false;

        /// <summary>
        /// Returns true if the wrapped task is completed.
        /// </summary>
        public bool IsCompleted => Task?.IsCompleted ?? true;

        /// <summary>
        /// Returns whether the wrapped task is still pending.
        /// </summary>
        public bool IsPending => !Task?.IsCompleted ?? false;

        /// <summary>
        /// True if the task completed without cancelation or exception.
        /// </summary>
        public bool IsSuccess => IsCompleted && !IsCanceled && !IsError;
    }
}