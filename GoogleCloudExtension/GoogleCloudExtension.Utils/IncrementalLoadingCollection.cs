using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    internal class CollectionSink<T> : IObserver<T>
    {
        private readonly IList<T> _destination;

        public CollectionSink(IList<T> destination)
        {
            _destination = destination;
        }

        #region IObserver implementation.

        void IObserver<T>.OnCompleted()
        {
            throw new NotImplementedException();
        }

        void IObserver<T>.OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        void IObserver<T>.OnNext(T value)
        {
            _destination.Add(value);
        }

        #endregion
    }

    public class IncrementalLoadingCollection<T> : IObserver<IEnumerable<T>>
    {
        private readonly IObservable<IEnumerable<T>> _pageSource;
        private readonly TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();
        private readonly ObservableCollection<T> _collection = new ObservableCollection<T>();

        public ReadOnlyObservableCollection<T> Data { get; }

        public Task DataLoadedTask => _completionSource.Task;
        
        public IncrementalLoadingCollection(IObservable<IEnumerable<T>> pageSource)
        {
            _pageSource = pageSource;
            _pageSource.Subscribe(this);

            Data = new ReadOnlyObservableCollection<T>(_collection);
        }

        #region IObserver implementation.

        void IObserver<IEnumerable<T>>.OnNext(IEnumerable<T> value)
        {
            foreach (var item in value)
            {
                _collection.Add(item);
            }
        }

        void IObserver<IEnumerable<T>>.OnError(Exception error)
        {
            _completionSource.SetException(error);
        }

        void IObserver<IEnumerable<T>>.OnCompleted()
        {
            _completionSource.SetResult(true);
        }

        #endregion
    }
}
