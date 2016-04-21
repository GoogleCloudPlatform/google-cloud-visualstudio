
namespace GoogleCloudExtension.OAuth
{
    public class OAuthCredentials
    {
        public string ClientId { get; }

        public string ClientSecret { get; }

        public OAuthCredentials(string clientId, string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
        }
    }
}