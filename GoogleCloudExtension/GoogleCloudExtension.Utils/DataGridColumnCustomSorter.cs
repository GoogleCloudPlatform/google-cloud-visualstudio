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

using System.Collections;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Implementation of <seealso cref="IComparer"/> that delegates the comparison
    /// to a <seealso cref="IColumnSorter"/>.
    /// </summary>
    internal class DataGridColumnCustomSorter : IComparer
    {
        private readonly IColumnSorter _sorter;
        private readonly bool _descending;

        public DataGridColumnCustomSorter(IColumnSorter sorter, bool descending)
        {
            _sorter = sorter;
            _descending = descending;
        }

        #region IComparer

        public int Compare(object x, object y)
        {
            return _sorter.Compare(x, y, _descending);
        }

        #endregion
    }
}
