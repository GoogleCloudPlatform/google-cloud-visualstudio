using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    internal class GaeVersions
    {
        [JsonProperty("versions")]
        public IEnumerable<GaeVersion> Versions { get; set; }

        [JsonProperty("nextPageToken")]
        public string NextPageToken { get; set; }
    }
}
