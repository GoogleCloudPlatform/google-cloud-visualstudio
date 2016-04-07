using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources.Models
{
    public class GPlusProfile
    {
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("emails")]
        public IList<Email> Emails { get; set; } 

        [JsonProperty("image")]
        public GPlusImage Image { get; set; }
    }
}
