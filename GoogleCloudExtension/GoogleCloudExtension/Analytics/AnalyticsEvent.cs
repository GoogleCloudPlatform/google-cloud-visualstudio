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
        private const string VersionName = "version";

        public string Name { get; }

        public Dictionary<string, string> Metadata { get; }

        public AnalyticsEvent(string name, params string[] metadata)
        {
            Name = name;
            Metadata = GetMetadataFromParams(metadata);
        }

        private static Dictionary<string, string> GetMetadataFromParams(string[] args)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (args.Length == 0)
            {
                if ((args.Length % 2) != 0)
                {
                    Debug.WriteLine($"Invalid count of params: {args.Length}");
                    return null;
                }

                for (int i = 0; i < args.Length; i += 2)
                {
                    result.Add(args[i], args[i + 1]);
                }
            }

            result[VersionName] = GoogleCloudExtensionPackage.ApplicationVersion;
            return result;
        }
    }
}
