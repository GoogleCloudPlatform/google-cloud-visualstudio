
namespace GoogleCloudExtension.OAuth
{
    /// <summary>
    /// Contains the OAUTH credentials to use for OAUTH authentication.
    /// </summary>
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