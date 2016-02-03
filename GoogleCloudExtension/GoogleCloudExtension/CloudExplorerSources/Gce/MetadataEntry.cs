using Newtonsoft.Json;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class MetadataEntry
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}