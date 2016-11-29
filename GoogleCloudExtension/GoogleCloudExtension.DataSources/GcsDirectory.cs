using Google.Apis.Storage.v1.Data;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources
{
    public class GcsDirectory
    {
        public IList<Object> Items { get; }

        public IList<string> Prefixes { get; }

        public GcsDirectory(IList<Object> items, IList<string> prefixes)
        {
            Items = items;
            Prefixes = prefixes;
        }
    }
}