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
using System.Diagnostics;
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
        private T _value;

        /// <summary>
        /// The value of the property, which will be set once Task where the value comes from
        /// is completed.
        /// </summary>
        public T Value
        {
            get { return _value; }
            private set { SetValueAndRaise(ref _value, value); }
        }

        public bool IsPending => !_valueSource.IsCompleted;

        public bool IsCompleted => _valueSource.IsCompleted;

        public bool IsError => _valueSource.IsFaulted;

        public AsyncPropertyValue(Task<T> valueSource, T defaultValue = default(T))
        {
            _valueSource = valueSource;
            _value = defaultValue;
            AwaitForValue();
        }

        private async void AwaitForValue()
        {
            try
            {
                Debug.WriteLine("Waiting for value...");
                _value = await _valueSource;
                Debug.WriteLine("Done waiting for value...");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get value: {ex.Message}");
            }
            finally
            {
                RaiseAllPropertyChanged();
            }
        }

        public static AsyncPropertyValue<T> CreateAsyncProperty<TIn>(Task<TIn> valueSource, Func<TIn, T> func, T defaultValue = default(T))
        {
            return new AsyncPropertyValue<T>(valueSource.ContinueWith(t => func(t.Result)));
        }

        public static AsyncPropertyValue<T> CreateAsyncProperty<T>(T value)
        {
            return new AsyncPropertyValue<T>(Task.FromResult(value));
        }
    }
}
