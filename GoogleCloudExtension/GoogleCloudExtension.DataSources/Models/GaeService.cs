using Newtonsoft.Json;

namespace GoogleCloudExtension.DataSources.Models
{
    public class GaeService
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("split")]
        public TrafficSplit Split { get; set; }
    }
}
