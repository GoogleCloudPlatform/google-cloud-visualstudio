using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    public class GceInstances
    {
        [JsonProperty("items")]
        public IList<GceInstance> Items { get; set; }
    }
}