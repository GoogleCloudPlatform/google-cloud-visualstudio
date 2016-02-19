using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud.Models
{
    public sealed class WindowsInstanceCredentials
    {
        [JsonProperty("username")]
        public string User { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
