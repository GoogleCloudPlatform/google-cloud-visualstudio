using Newtonsoft.Json;

namespace GoogleCloudExtension.DataSources.Models
{
    public class Email
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}