using Newtonsoft.Json;
using System;

namespace GoogleCloudExtension.DataSources.Models
{
    public class GcpProject
    {
        [JsonProperty("projectId")]
        public string Id { get; set; }

        [JsonProperty("projectNumber")]
        public string Number { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("createTime")]
        public DateTime CreateTime { get; set; }
    }
}