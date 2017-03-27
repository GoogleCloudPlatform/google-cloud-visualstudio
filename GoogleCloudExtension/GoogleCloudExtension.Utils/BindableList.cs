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

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This class implements a list that allows the DataContext to flow through from the owner of the
    /// list to the elements in the list. <typeparamref name="T"/> must be a <seealso cref="FrameworkElement"/> so
    /// the DataContext can be set as they are added to the list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BindableList<T> : ObservableCollection<T> where T : FrameworkElement
    {
        private readonly DependencyObject _dataContextSource;

        public BindableList(DependencyObject dataContextSource)
        {
            _dataContextSource = dataContextSource;
        }

        protected override void SetItem(int index, T item)
        {
            base.SetItem(index, item);
            SetupDataContextBinding(item);
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            SetupDataContextBinding(item);
        }

        /// <summary>
        /// Ensures that the DataContext in the item is connected to the source of the data context,
        /// we use a Binding so that updats to the source's DataContext are reflected in the items.
        /// </summary>
        /// <param name="item"></param>
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
