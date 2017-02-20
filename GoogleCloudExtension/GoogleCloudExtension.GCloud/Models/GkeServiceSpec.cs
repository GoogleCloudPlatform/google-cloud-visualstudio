using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    public class GkeServiceSpec
    {
        [JsonProperty("clusterIP")]
        public string ClusterIp { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}