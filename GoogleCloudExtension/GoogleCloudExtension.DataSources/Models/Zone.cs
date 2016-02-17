using Newtonsoft.Json;

namespace GoogleCloudExtension.DataSources.Models
{
    public class Zone
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }
    }
}