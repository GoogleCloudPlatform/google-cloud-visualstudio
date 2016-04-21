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
    }
}
