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
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class is an INotifyPropertyChanged for an async task.
    /// </summary>
    public class AsyncProperty : Model
    {
        /// <summary>
        /// Returns true if the wrapped task is in error.
        /// </summary>
        public bool IsError => _task?.IsFaulted ?? false;

        /// <summary>
        /// Gets a relevant exception error message, if it exists.
        /// </summary>
        public string ErrorMessage =>
            _task.Exception?.InnerException?.Message ??
            _task.Exception?.InnerExceptions.FirstOrDefault()?.Message ??
            _task.Exception?.Message;

        /// <summary>
        /// True if the task ended execution due to being canceled.
        /// </summary>
        public bool IsCanceled => _task?.IsCanceled ?? false;

        /// <summary>
        /// Returns true if the wrapped task is completed.
        /// </summary>
        public bool IsCompleted => _task?.IsCompleted ?? true;

        /// <summary>
        /// Returns whether the wrapped task is still pending.
        /// </summary>
        public bool IsPending => !_task?.IsCompleted ?? false;

        /// <summary>
        /// True if the task completed without cancelation or exception.
        /// </summary>
        public bool IsSuccess => IsCompleted && !IsCanceled && !IsError;

        private readonly Task _task;

        public AsyncProperty(Task task) : this(task, true) { }

        protected AsyncProperty(Task task, bool wait)
        {
            _task = task;
            if (wait)
            {
                WaitTask(task);
            }
        }

        protected async void WaitTask(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                // Check exceptions using task.
            }
            AwaitCallBack();
            RaiseAllPropertyChanged();
        }

        protected virtual void AwaitCallBack() { }
    }

    /// <summary>
    /// This class is an async model for a single async property, the Value property will
    /// be set to the result of the Task once it is completed.
    /// </summary>
    /// <typeparam name="T">The type of the property</typeparam>
    public class AsyncProperty<T> : AsyncProperty
    {
        private readonly Task<T> _valueSource;
        private readonly Lazy<TaskCompletionSource<T>> _completionSource = new Lazy<TaskCompletionSource<T>>();

        /// <summary>
        /// The value of the property, which will be set once Task where the value comes from
        /// is completed.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Returns a task that will be completed once the wrapped task is completed. This task is
        /// not directly connected to the wrapped task and will never throw and error.
        /// </summary>
        public Task<T> ValueTask => _completionSource.Value.Task;

        public AsyncProperty(Task<T> valueSource, T defaultValue = default(T)) : base(valueSource, false)
        {
            _valueSource = valueSource;
            Value = defaultValue;
            WaitTask(_valueSource);
        }

        public AsyncProperty(T value) : this(Task.FromResult(value)) { }

        protected override void AwaitCallBack()
        {
            if (_valueSource?.IsCompleted ?? false)
            {
                Value = _valueSource.Result;
            }
            _completionSource.Value.SetResult(Value);
        }
    }

    public static class AsyncPropertyUtils
    {
        /// <summary>
        /// Creates an <seealso cref="AsyncProperty{T}"/> from the given task. When the task competes it will
        /// apply <paramref name="func"/> and that will be the final value of the property.
        /// </summary>
        /// <typeparam name="TIn">The type that the task will produce.</typeparam>
        /// <typeparam name="T">The type that the property will produce.</typeparam>
        /// <param name="valueSource">The task where the value comes from.</param>
        /// <param name="func">The function to apply to the result of the task.</param>
        /// <param name="defaultValue">The value to use while the task is executing.</param>
        public static AsyncProperty<T> CreateAsyncProperty<TIn, T>(
            Task<TIn> valueSource, Func<TIn, T> func, T defaultValue = default(T))
        {
            return new AsyncProperty<T>(valueSource.ContinueWith(t => func(t.Result)), defaultValue);
        }

        public static AsyncProperty<T> CreateAsyncProperty<T>(Task<T> valueSource, T defaultValue = default(T))
        {
            return new AsyncProperty<T>(valueSource, defaultValue);
        }

        public static AsyncProperty CreateAsyncProperty(Task sourceTask)
        {
            return new AsyncProperty(sourceTask);
        }
    }
}
