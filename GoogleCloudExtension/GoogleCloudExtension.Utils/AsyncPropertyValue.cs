using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        public AsyncPropertyValue(Task<T> valueSource, T defaultValue = default(T))
        {
            _value = defaultValue;
            AwaitForValue(valueSource);
        }

        private async void AwaitForValue(Task<T> valueSource)
        {
            try
            {
                Debug.WriteLine("Waiting for value...");
                Value = await valueSource;
                Debug.WriteLine("Done waiting for value...");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to get value: {ex.Message}");
            }
        }

        public static AsyncPropertyValue<T> CreateAsyncProperty<TIn>(Task<TIn> valueSource, Func<TIn, T> func, T defaultValue = default(T))
        {
            return new AsyncPropertyValue<T>(valueSource.ContinueWith(t => func(t.Result)));
        }
    }
}
