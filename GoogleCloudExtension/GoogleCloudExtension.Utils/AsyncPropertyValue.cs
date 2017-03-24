// Copyright 2016 Google Inc. All Rights Reserved.
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

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class is an async model for a single async property, the Value property will
    /// be set to the result of the Task once it is completed.
    /// </summary>
    /// <typeparam name="T">The type of the property</typeparam>
    public class AsyncPropertyValue<T> : Model
    {
        private readonly Task<T> _valueSource;
        private readonly Lazy<TaskCompletionSource<bool>> _completionSource = new Lazy<TaskCompletionSource<bool>>();

        /// <summary>
        /// The value of the property, which will be set once Task where the value comes from
        /// is completed.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Returns whether the wrapped task is still pending.
        /// </summary>
        public bool IsPending => !_valueSource?.IsCompleted ?? false;

        /// <summary>
        /// Returns true if the wrapped task is completed.
        /// </summary>
        public bool IsCompleted => _valueSource?.IsCompleted ?? true;

        /// <summary>
        /// Returns true if the wrapped task is in error.
        /// </summary>
        public bool IsError => _valueSource?.IsFaulted ?? false;

        /// <summary>
        /// Returns a task that will be completed once the wrapped task is completed. This task is
        /// not directly connected to the wrapped task and will never throw and error.
        /// </summary>
        public Task ValueTask => _completionSource.Value.Task;

        public AsyncPropertyValue(Task<T> valueSource, T defaultValue = default(T))
        {
            _valueSource = valueSource;
            Value = defaultValue;
            AwaitForValue();
        }

        public AsyncPropertyValue(T value)
        {
            Value = value;
        }

        private void AwaitForValue()
        {
            _valueSource.ContinueWith((t) =>
            {
                if (t.IsCompleted)
                {
                    Value = t.Result;
                }
                _completionSource.Value.SetResult(true);
                RaiseAllPropertyChanged();
            });
        }
    }

    public static class AsyncPropertyValueUtils
    {
        /// <summary>
        /// Creates an <seealso cref="AsyncPropertyValue{T}"/> from the given task. When the task competes it will
        /// apply <paramref name="func"/> and that will be the final value of the property.
        /// </summary>
        /// <typeparam name="TIn">The type that the task will produce.</typeparam>
        /// <typeparam name="T">The type that the property will produce.</typeparam>
        /// <param name="valueSource">The task where the value comes from.</param>
        /// <param name="func">The function to apply to the result of the task.</param>
        /// <param name="defaultValue">The value to use while the task is executing.</param>
        public static AsyncPropertyValue<T> CreateAsyncProperty<TIn, T>(Task<TIn> valueSource, Func<TIn, T> func, T defaultValue = default(T))
        {
            return new AsyncPropertyValue<T>(valueSource.ContinueWith(t => func(t.Result)), defaultValue);
        }

        public static AsyncPropertyValue<T> CreateAsyncProperty<T>(Task<T> valueSource, T defaultValue = default(T))
        {
            return new AsyncPropertyValue<T>(valueSource, defaultValue);
        }
    }
}
