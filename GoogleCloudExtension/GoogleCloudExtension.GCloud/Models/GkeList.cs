using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.GCloud.Models
{
    public class GkeList<T>
    {
        [JsonProperty("items")]
        public IList<T> Items { get; set; }
    }
}
