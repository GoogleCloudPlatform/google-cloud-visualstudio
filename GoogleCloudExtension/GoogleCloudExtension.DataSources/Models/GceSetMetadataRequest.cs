using System.Collections.Generic;
using GoogleCloudExtension.DataSources.Models;
using Newtonsoft.Json;

namespace GoogleCloudExtension.DataSources.Models
{
    public sealed class GceSetMetadataRequest
    {
        [JsonProperty("kind")]
        public string Kind => "compute#metadata";

        [JsonProperty("fingerprint")]
        public string Fingerprint { get; set; }

        [JsonProperty("items")]
        public IList<MetadataEntry> Items { get; set; }
    }
}