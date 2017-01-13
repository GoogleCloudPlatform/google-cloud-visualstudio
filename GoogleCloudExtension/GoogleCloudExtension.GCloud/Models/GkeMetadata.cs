using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    public class GkeMetadata
    {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}