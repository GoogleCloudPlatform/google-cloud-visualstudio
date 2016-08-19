using Newtonsoft.Json;

namespace GoogleCloudExtension.Accounts
{
    public class WindowsCredentials
    {
        [JsonProperty("user_name")]
        public string UserName { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
