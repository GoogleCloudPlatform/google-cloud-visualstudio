using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Analytics
{
    internal class AnalyticsEvent
    {
        public string Name { get; }

        public Dictionary<string, string> Metadata { get; }

        public AnalyticsEvent(string name, Dictionary<string, string> metadata)
        {
            Name = name;
            Metadata = metadata;
        }

        public AnalyticsEvent(string name, params string[] metadata):
            this(name, GetMetadataFromParams(metadata))
        { }

        private static Dictionary<string, string> GetMetadataFromParams(string[] args)
        {
            if (args.Length == 0)
            {
                return null;
            }

            if ((args.Length % 2) != 0)
            {
                Debug.WriteLine($"Invalid count of params: {args.Length}");
                return null;
            }

            Dictionary<string, string> result = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i += 2)
            {
                result.Add(args[i], args[i + 1]);
            }
            return result;
        }
    }
}
