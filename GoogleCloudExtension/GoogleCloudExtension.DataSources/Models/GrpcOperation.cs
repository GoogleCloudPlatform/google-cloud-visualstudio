using Newtonsoft.Json;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    internal class GrpcOperation
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("done")]
        public bool Done { get; set; }

        [JsonProperty("error")]
        public IDictionary<string, object> Error { get; set; }

        [JsonProperty("response")]
        public IDictionary<string, object> Response { get; set; }
    }
}
