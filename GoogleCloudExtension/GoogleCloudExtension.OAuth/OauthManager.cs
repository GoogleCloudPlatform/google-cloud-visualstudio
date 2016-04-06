using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.OAuth
{
    public static class OAuthManager
    {
        const string OAuthRefreshEndpoint = "https://www.googleapis.com/oauth2/v3/token";

        /// <summary>
        /// Given an app credentials and the refresh token it will create an access token by accessing the
        /// Google OAUTH end point.
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        public static async Task<string> RefreshAccessTokenAsync(OAuthCredentials credentials, string refreshToken)
        {
            WebClient client = new WebClient();
            //client.UploadValuesTaskAsync()

            return null;
        }
    }
}
