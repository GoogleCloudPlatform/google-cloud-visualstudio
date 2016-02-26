using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    public class Disk
    {
        [JsonProperty("licenses")]
        public IList<string> Licenses { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }
    }
}