using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace GoogleCloudExtension.Accounts.Models
{
    public class UserAccount
    {
        [JsonProperty("account")]
        public string AccountName { get; set; }

        [JsonProperty("client_id")]
        public string ClientId { get; set; }

        [JsonProperty("client_secret")]
        public string ClientSecret { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("type")]
        public string Type => "authorized_user";

        public GoogleCredential GetGoogleCredential()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new StreamWriter(stream, Encoding.UTF8, 100, leaveOpen: true))
                {
                    var serialized = JsonConvert.SerializeObject(this);
                    writer.Write(serialized);
                }

                stream.Position = 0;
                return GoogleCredential.FromStream(stream);
            }
        }
    }
}
