using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud.Models
{
    public class GkeService
    {
        [JsonProperty("metadata")]
        public GkeMetadata Metadata { get; set; }

        [JsonProperty("status")]
        public GkeStatus Status { get; set; }
    }
}
