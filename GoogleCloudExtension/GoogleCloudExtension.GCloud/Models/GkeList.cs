using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GCloud.Models
{
    public class GkeList<T>
    {
        [JsonProperty("items")]
        public IList<T> Items { get; set; }
    }
}
