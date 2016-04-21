using System;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Small class that replaces Lazy<typeparamref name="T"/> and that allows reseting the value
    /// so it can be recreated later. This class is threadsafe, only one thread will create the value
    /// and the rest will get the same created instance.
    /// Resetting the value is thread-safe as well.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public class Atomic<T> where T: class
    {
        private readonly object _lock = new object();
        private readonly Func<T> _factory;
        private T _value;
        private bool _created;

        /// <summary>
        /// Constructor that accepts a factory function.
        /// </summary>
        /// <param name="factory">The function to create the value.</param>
        public Atomic(Func<T> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// The value contained in the Atomic<typeparamref name="T"/>, if the value is not yet created
        /// it will create it here.
        /// </summary>
        public T Value
        {
            get
            {
                lock (_lock)
                {
                    if (!_created)
                    {
                        _value = _factory();
                        _created = true;
                    }
                    return _value;
                }
            }
        }

        /// <summary>
        /// Resets the Atomic<typeparamref name="T"/> back to the state where the value is not
        /// created.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _created = false;
                _value = default(T);
            }
        }
    }
}
