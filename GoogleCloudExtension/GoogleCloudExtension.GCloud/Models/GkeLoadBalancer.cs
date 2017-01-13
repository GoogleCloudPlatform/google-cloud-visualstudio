using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.GCloud.Models
{
    public class GkeLoadBalancer
    {
        [JsonProperty("ingress")]
        public IList<IDictionary<string, string>> Ingress { get; set; }
    }
}