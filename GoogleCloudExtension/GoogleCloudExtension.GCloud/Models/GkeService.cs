using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    public class GkeService
    {
        [JsonProperty("metadata")]
        public GkeMetadata Metadata { get; set; }

        [JsonProperty("status")]
        public GkeStatus Status { get; set; }
    }
}
