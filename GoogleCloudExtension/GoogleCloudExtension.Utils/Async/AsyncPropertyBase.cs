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
    public abstract class AsyncPropertyBase<T> : Model where T : Task
    {
        /// <summary>
        /// A task that succeeds when the actual task completes. This task will never throw.
        /// </summary>
        public Task SafeTask { get; }

        /// <summary>
        /// The actual task sent to the property.
        /// </summary>
        public T ActualTask { get; }

        /// <summary>
        /// Returns whether the wrapped task is still pending.
        /// </summary>
        public bool IsPending => !ActualTask?.IsCompleted ?? false;

        /// <summary>
        /// Returns true if the wrapped task is completed.
        /// </summary>
        public bool IsCompleted => ActualTask?.IsCompleted ?? false;

        /// <summary>
        /// True if the task completed without cancelation or exception.
        /// </summary>
        public bool IsSuccess => IsCompleted && !IsCanceled && !IsError;

        /// <summary>
        /// True if the task ended execution due to being canceled.
        /// </summary>
        public bool IsCanceled => ActualTask?.IsCanceled ?? false;


        /// <summary>
        /// Returns true if the wrapped task is in error.
        /// </summary>
        public bool IsError => ActualTask?.IsFaulted ?? false;

        /// <summary>
        /// Gets the relevant exception, if it exists.
        /// </summary>
        public string ErrorMessage =>
            ActualTask?.Exception?.InnerException?.Message ??
            ActualTask?.Exception?.InnerExceptions?.Select(e => e?.Message)
                .FirstOrDefault(m => !string.IsNullOrEmpty(m)) ??
            ActualTask?.Exception?.Message;

        protected AsyncPropertyBase(T task)
        {
            ActualTask = task;
            SafeTask = WaitTask();
        }

        private async Task WaitTask()
        {
            if (ActualTask != null)
            {
                try
                {
                    await ActualTask;
                }
                catch
                {
                    // Check exceptions using task.
                }

                OnTaskComplete();
                RaisePropertiesChanged();
            }
        }

        private void RaisePropertiesChanged()
        {
            SafeRaisePropertyChanged(nameof(IsPending));
            SafeRaisePropertyChanged(nameof(IsCompleted));
            if (IsSuccess)
            {
                SafeRaisePropertyChanged(nameof(IsSuccess));
            }

            if (IsCanceled)
            {
                SafeRaisePropertyChanged(nameof(IsCanceled));
            }

            if (IsError)
            {
                SafeRaisePropertyChanged(nameof(IsError));
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                SafeRaisePropertyChanged(nameof(ErrorMessage));
            }
        }

        private void SafeRaisePropertyChanged(string propertyName)
        {
            try
            {
                RaisePropertyChanged(propertyName);
            }
            catch
            {
                // Keep SafeTask safe. Ignore event handler exceptions.
            }
        }

        protected virtual void OnTaskComplete()
        {
        }
    }
}