using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace GoogleCloudExtension.DataSources.Models
{
    public class Error
    {
        [JsonProperty("errors")]
        public IList<ErrorEntry> Errors { get; set; }
    }
}