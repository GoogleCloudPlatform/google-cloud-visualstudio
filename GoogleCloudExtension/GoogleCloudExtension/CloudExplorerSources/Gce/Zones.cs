using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class Zones
    {
        [JsonProperty("items")]
        public IList<Zone> Items { get; set; }
    }
}