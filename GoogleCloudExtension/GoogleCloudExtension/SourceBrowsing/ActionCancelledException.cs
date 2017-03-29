using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.SourceBrowsing
{
    internal class ActionCancelledException : Exception
    {
        internal ActionCancelledException() : base() { }
    }
}
