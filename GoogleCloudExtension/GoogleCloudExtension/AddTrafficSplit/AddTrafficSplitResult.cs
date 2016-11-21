using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.AddTrafficSplit
{
    public class AddTrafficSplitResult
    {
        public bool IpAddressSplit { get; }

        public bool CookieSplit { get; }

        public string Version { get; }

        public int Allocation { get; }

        public AddTrafficSplitResult(
            string version,
            int allocation,
            bool ipAddressSplit,
            bool cookieSplit)
        {
            Version = version;
            Allocation = allocation;
            IpAddressSplit = ipAddressSplit;
            CookieSplit = cookieSplit;
        }
    }
}
