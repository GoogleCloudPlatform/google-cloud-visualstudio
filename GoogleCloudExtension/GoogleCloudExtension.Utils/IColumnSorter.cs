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

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// This interface allows sorting on both ascending and descending directions. Which enables
    /// a single instance to do both sortings.
    /// </summary>
    public interface IColumnSorter
    {
        /// <summary>
        /// Compares <paramref name="x"/> vs. <paramref name="y"/> and returns their relative comparison in the
        /// same way that <seealso cref="System.Collections.IComparer"/> does, returning:
        ///   negative, if x is "smaller" that y.
        ///   0, if x is "equal" to y.
        ///   positive, if x is greater than y.
        /// </summary>
        /// <param name="x">The first value to compare, left hand side.</param>
        /// <param name="y">The second value to compare, right hand side.</param>
        /// <param name="descending">True if the sorting is "descending" order, false if "ascending".</param>
        int Compare(object x, object y, bool descending);
    }
}
