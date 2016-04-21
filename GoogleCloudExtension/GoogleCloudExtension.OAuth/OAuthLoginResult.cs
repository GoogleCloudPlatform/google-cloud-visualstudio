using GoogleCloudExtension.OAuth.Models;

namespace GoogleCloudExtension.OAuth
{
    public class OAuthLoginResult
    {
        public string RefreshToken { get; }

        public AccessToken AccessToken { get; }

        public OAuthLoginResult(string refreshToken, AccessToken accessToken)
        {
            RefreshToken = refreshToken;
            AccessToken = accessToken;
        }

        internal OAuthLoginResult(AccessTokenModel model)
        {
            RefreshToken = model.RefreshToken;
            AccessToken = new AccessToken(model);
        }
    }
}
