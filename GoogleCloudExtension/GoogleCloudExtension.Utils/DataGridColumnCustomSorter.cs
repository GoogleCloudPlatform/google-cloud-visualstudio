using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
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
