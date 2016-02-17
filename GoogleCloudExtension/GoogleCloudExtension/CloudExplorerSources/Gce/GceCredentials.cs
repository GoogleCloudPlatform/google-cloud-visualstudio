using Newtonsoft.Json;

namespace GoogleCloudExtension.CloudExplorerSources.Gce
{
    public class GceCredentials
    {
        [JsonProperty("user")]
        public string User { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
