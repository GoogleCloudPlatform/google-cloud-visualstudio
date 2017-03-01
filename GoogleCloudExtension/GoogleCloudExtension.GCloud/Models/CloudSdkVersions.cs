using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud.Models
{
    internal class CloudSdkVersions
    {
        [JsonProperty("Google Cloud SDK")]
        public string SdkVersion { get; set; }
    }
}
