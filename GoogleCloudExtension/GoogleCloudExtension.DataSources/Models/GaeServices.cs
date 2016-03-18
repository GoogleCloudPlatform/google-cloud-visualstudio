using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    internal class GaeServices
    {
        [JsonProperty("nextPageToken")]
        public string NextPageToken { get; internal set; }

        [JsonProperty("services")]
        public IEnumerable<GaeService> Services { get; internal set; }
    }
}
