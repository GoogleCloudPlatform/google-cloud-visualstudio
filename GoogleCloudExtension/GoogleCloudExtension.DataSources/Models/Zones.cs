using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    public class Zones
    {
        [JsonProperty("items")]
        public IList<Zone> Items { get; set; }
    }
}