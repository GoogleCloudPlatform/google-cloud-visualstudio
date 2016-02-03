using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class GceInstances
    {
        [JsonProperty("items")]
        public IList<GceInstance> Items { get; set; }
    }
}