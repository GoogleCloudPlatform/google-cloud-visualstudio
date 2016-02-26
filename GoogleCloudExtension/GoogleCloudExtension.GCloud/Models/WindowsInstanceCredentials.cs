using Newtonsoft.Json;

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
