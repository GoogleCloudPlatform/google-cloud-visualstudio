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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;

namespace GoogleCloudExtension.Utils
{
    public class BindableList<T> : IList where T : FrameworkElement
    {
        private readonly IList<T> _storage = new ObservableCollection<T>();
        private readonly DependencyObject _dataContextSource;

        public IList<T> Collection => _storage;

        #region IList

        bool IList.IsReadOnly => _storage.IsReadOnly;

        bool IList.IsFixedSize => false;

        int ICollection.Count => _storage.Count;

        object ICollection.SyncRoot => this;

        bool ICollection.IsSynchronized => false;

        object IList.this[int index]
        {
            get
            {
                return _storage[index];
            }

            set
            {
                _storage[index] = (T)value;
            }
        }

        int IList.Add(object value)
        {
            T item = (T)value;
            SetupDataContextBinding(item);
            _storage.Add(item);
            return _storage.Count;
        }

        bool IList.Contains(object value) => _storage.Contains((T)value);

        void IList.Clear() => _storage.Clear();

        int IList.IndexOf(object value) => _storage.IndexOf((T)value);

        void IList.Insert(int index, object value)
        {
            T item = (T)value;
            SetupDataContextBinding(item);
            _storage.Insert(index, item);
        }

        void IList.Remove(object value) => _storage.Remove((T)value);

        void IList.RemoveAt(int index) => _storage.RemoveAt(index);

        void ICollection.CopyTo(Array array, int index) => _storage.CopyTo((T[])array, index);

        IEnumerator IEnumerable.GetEnumerator() => _storage.GetEnumerator();

        #endregion 

        public BindableList(DependencyObject dataContextSource)
        {
            _dataContextSource = dataContextSource;
        }

        private void SetupDataContextBinding(T item)
        {
            Binding dataContextBinding = new Binding
            {
                Path = new PropertyPath("DataContext"),
                Source = _dataContextSource
            };
            BindingOperations.SetBinding(item, FrameworkElement.DataContextProperty, dataContextBinding);
        }
    }
}
