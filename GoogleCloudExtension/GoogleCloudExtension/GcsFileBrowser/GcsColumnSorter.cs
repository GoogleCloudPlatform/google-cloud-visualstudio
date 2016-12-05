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

using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Markup;

namespace GoogleCloudExtension.GcsFileBrowser
{
    /// <summary>
    /// This class sorts the GcsItem instances in the right way, with directories always first.
    /// </summary>
    public class GcsColumnSorter : MarkupExtension, IColumnSorter
    {
        private readonly Lazy<PropertyInfo> _columnProperty;

        public string Column { get; set; }

        public bool SortDirectories { get; set; }

        public GcsColumnSorter(string column)
        {
            Column = column;
            _columnProperty = new Lazy<PropertyInfo>(GetColumnPropertyInfo);
        }

        #region MarkupExtension implementation.

        public override object ProvideValue(IServiceProvider serviceProvider) => this;

        #endregion

        #region IColumnSorter implementation.

        public int Compare(object x, object y, bool descending)
        {
            var lhs = x as GcsRow;
            var rhs = y as GcsRow;
            if (lhs == null || rhs == null)
            {
                Debug.WriteLine($"Unknown types passed to the {nameof(GcsColumnSorter)}");
                return 0;
            }

            // If the items being compared is a directory vs. a file then the directory will
            // always be first.
            if (lhs.IsDirectory != rhs.IsDirectory)
            {
                return lhs.IsDirectory ? -1 : 1;
            }

            // If both items are directories then we always sort them alphabetically.
            if (lhs.IsDirectory && rhs.IsDirectory)
            {
                var comparison = lhs.FileName.CompareTo(rhs.FileName);
                return descending && SortDirectories ? -1 * comparison : comparison;
            }

            // At this point lhs and rhs are files, we will compare them based on the requested column.
            var lhs_value = GetValue(lhs);
            var rhs_value = GetValue(rhs);

            if (lhs_value is string)
            {
                var lhs_str = (string)lhs_value;
                var rhs_str = (string)rhs_value;

                var comparison = lhs_str.CompareTo(rhs_str);
                return descending ? -1 * comparison : comparison;
            }

            if (lhs_value is ulong)
            {
                var lhs_ulong = (ulong)lhs_value;
                var rhs_ulong = (ulong)rhs_value;

                if (lhs_ulong == rhs_ulong)
                {
                    return 0;
                }
                else if (lhs_ulong < rhs_ulong)
                {
                    return descending ? 1 : -1;
                }
                else
                {
                    return descending ? -1 : 1;
                }
            }

            return 0;
        }

        #endregion

        private PropertyInfo GetColumnPropertyInfo() => typeof(GcsRow).GetProperty(Column);

        private object GetValue(GcsRow item)
        {
            return _columnProperty.Value.GetValue(item);
        }
    }
}
