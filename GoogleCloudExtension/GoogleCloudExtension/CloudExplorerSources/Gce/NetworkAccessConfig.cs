using Newtonsoft.Json;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class NetworkAccessConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("natIP")]
        public string NatIP { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}