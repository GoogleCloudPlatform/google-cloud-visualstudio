using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    public class Metadata
    {
        [JsonProperty("items")]
        public IList<MetadataEntry> Items { get; set; }
    }
}