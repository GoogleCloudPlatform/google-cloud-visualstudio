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
            return new AsyncProperty<T>(
                valueSource.ContinueWith(t => SafeFuncCall(func, SafeGetTaskResult(t), defaultValue)), 
                defaultValue);
        }

        public static AsyncProperty<T> CreateAsyncProperty<T>(Task<T> valueSource, T defaultValue = default(T))
        {
            return new AsyncProperty<T>(valueSource, defaultValue);
        }

        public static AsyncProperty CreateAsyncProperty(Task sourceTask)
        {
            return new AsyncProperty(sourceTask);
        }

        public static TIn SafeGetTaskResult<TIn>(Task<TIn> task)
        {
            try
            {
                return task.Result;
            }
            // catch all, otherwise it terminates Visual Studio
            catch
            {
                return default(TIn);
            }
        }

        private static T SafeFuncCall<TIn, T>(Func<TIn, T> func, TIn t, T defaultValue)
        {
            try
            {
                return func(t);
            }
            // catch all, otherwise it terminates Visual Studio
            catch
            {
                return defaultValue;
            }
        }
    }
}