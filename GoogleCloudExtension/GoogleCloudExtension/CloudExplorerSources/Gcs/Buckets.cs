using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.CloudExplorerSources.Gcs
{
    internal class Buckets
    {
        [JsonProperty("items")]
        public IList<Bucket> Items { get; set; }
    }
}