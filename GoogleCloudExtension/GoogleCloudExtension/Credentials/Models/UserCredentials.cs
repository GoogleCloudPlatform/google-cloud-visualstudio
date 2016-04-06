using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Credentials.Models
{
    public class UserCredentials
    {
        [JsonProperty("account")]
        public string AccountName { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }
}
