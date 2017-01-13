using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    public class GkeDeployment
    {
        [JsonProperty("metadata")]
        public GkeMetadata Metadata { get; set; }
    }
}