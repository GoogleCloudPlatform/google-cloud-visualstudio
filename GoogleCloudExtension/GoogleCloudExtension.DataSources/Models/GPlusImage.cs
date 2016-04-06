using Newtonsoft.Json;

namespace GoogleCloudExtension.DataSources.Models
{
    public class GPlusImage
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}