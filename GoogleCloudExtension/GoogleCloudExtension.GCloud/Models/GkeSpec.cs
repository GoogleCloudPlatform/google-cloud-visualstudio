using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    /// <summary>
    /// This class contains the spec data for a Kubernetes object.
    /// </summary>
    public class GkeSpec
    {
        [JsonProperty("replicas")]
        public int Replicas { get; set; }
    }
}