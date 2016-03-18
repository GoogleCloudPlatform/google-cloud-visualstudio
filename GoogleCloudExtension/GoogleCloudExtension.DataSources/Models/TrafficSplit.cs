using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    public class TrafficSplit
    {
        [JsonProperty("allocations")]
        public IDictionary<string, double> Allocations { get; set; }
    }
}