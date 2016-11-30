using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    public interface IColumnSorter
    {
        int Compare(object x, object y, bool descending);
    }
}
