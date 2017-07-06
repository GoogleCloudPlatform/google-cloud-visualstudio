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

using System;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils.Async
{
    /// <summary>
    /// This class is an INotifyPropertyChanged for an async task.
    /// </summary>
    public class AsyncProperty : AsyncPropertyBase
    {
        protected override Task Task { get; }

        public AsyncProperty(Task task)
        {
            Task = task;
            WaitTask(task);
        }

        private async void WaitTask(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                // Check exceptions using task.
            }
            RaiseAllPropertyChanged();
        }
    }

    /// <summary>
    /// This class is an async model for a single async property, the Value property will
    /// be set to the result of the Task once it is completed.
    /// </summary>
    /// <typeparam name="T">The type of the property</typeparam>
    public class AsyncProperty<T> : AsyncPropertyBase
    {
        private readonly Task<T> _valueSource;
        private readonly Lazy<TaskCompletionSource<bool>> _completionSource = new Lazy<TaskCompletionSource<bool>>();

        /// <summary>
        /// The value of the property, which will be set once Task where the value comes from
        /// is completed.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Returns a task that will be completed once the wrapped task is completed. This task is
        /// not directly connected to the wrapped task and will never throw and error.
        /// </summary>
        public Task ValueTask => _completionSource.Value.Task;

        protected override Task Task => _valueSource;

        public AsyncProperty(Task<T> valueSource, T defaultValue = default(T))
        {
            _valueSource = valueSource;
            Value = defaultValue;
            AwaitForValue();
        }

        public AsyncProperty(T value)
        {
            Value = value;
            _completionSource.Value.SetResult(true);
        }

        private void AwaitForValue()
        {
            _valueSource.ContinueWith((t) =>
            {
                // Value is initiated with defaultValue at constructor.
                Value = AsyncPropertyUtils.GetTaskResultSafe(t, defaultValue: Value);
                _completionSource.Value.SetResult(true);
                RaiseAllPropertyChanged();
            });
        }
    }
}
