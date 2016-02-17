using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    public class InstanceTags
    {
        [JsonProperty("items")]
        public IList<string> Items { get; set; }
    }
}