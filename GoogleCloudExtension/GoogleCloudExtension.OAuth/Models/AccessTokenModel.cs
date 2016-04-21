using Newtonsoft.Json;

namespace GoogleCloudExtension.OAuth.Models
{
    /// <summary>
    /// This class models all of the responses from the oauth server that we care about when
    /// logging in a user.
    /// </summary>
    internal class AccessTokenModel
    {
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("access_token")]
        public string Token { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
