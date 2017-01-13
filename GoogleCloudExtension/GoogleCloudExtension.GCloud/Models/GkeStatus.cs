using Newtonsoft.Json;

namespace GoogleCloudExtension.GCloud.Models
{
    public class GkeStatus
    {
        [JsonProperty("loadBalancer")]
        public GkeLoadBalancer LoadBalancer { get; set; }
    }
}