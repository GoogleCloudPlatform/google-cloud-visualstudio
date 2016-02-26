using Newtonsoft.Json;

namespace GoogleCloudExtension.DataSources.Models
{
    public class ZoneOperation
    {
        [JsonProperty("id")]
        public string Id { get; set; } 

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("progress")]
        public int Progress { get; set; }

        [JsonProperty("selfLink")]
        public string SelfLink { get; set; }

        [JsonProperty("error")]
        public Error Error { get; set; }
    }
}
