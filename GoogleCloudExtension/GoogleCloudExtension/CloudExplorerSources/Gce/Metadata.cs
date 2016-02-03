using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class Metadata
    {
        [JsonProperty("items")]
        public IList<MetadataEntry> Items { get; set; }
    }
}