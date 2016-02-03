using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class InstanceTags
    {
        [JsonProperty("items")]
        public IList<string> Items { get; set; }
    }
}