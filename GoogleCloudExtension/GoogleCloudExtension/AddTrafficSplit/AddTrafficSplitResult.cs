using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.AddTrafficSplit
{
    public class AddTrafficSplitResult
    {
        public string Version { get; }

        public int Allocation { get; }

        public AddTrafficSplitResult(
            string version,
            int allocation)
        {
            Version = version;
            Allocation = allocation;
        }
    }
}
