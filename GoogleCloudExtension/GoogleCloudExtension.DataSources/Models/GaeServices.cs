using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
