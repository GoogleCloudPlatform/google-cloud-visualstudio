using System;

namespace GoogleCloudExtension.Utils
{
    public class Disposable : IDisposable
    {
        private readonly Action _action;

        public Disposable(Action action)
        {
            _action = action;
        }

        void IDisposable.Dispose()
        {
            _action?.Invoke();
        }
    }
}
