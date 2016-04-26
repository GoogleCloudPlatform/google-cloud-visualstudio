using GoogleCloudExtension.OAuth.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.OAuth
{
    /// <summary>
    /// This is a rather small class that takes care of the OAUTH protocol to get at the refresh token for
    /// a user.
    /// </summary>
    public static class OAuthManager
    {
        /// <summary>
        /// The URL for the oauth service.
        /// </summary>
        private const string OAuthApiUrl = "https://www.googleapis.com/oauth2/v3/token";

        /// <summary>
        /// The redirect url to use in the flow to get the credentials as the title of the window.
        /// </summary>
        private const string OAuthRedirectUrl = "urn:ietf:wg:oauth:2.0:oob";

        /// <summary>
        /// Returns the URL to use to start the OAUTH login flow.
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="scopes"></param>
        public static string GetInitialOAuthUrl(OAuthCredentials credentials, IEnumerable<string> scopes)
        {
            var form = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = credentials.ClientId,
                ["redirect_uri"] = OAuthRedirectUrl,
                ["scope"] = String.Join(" ", scopes),
            };
            return $"https://accounts.google.com/o/oauth2/auth?{ToQueryString(form)}";
        }

        /// <summary>
        /// Returns the refresh code given the <paramref name="credentials"/> and the <paramref name="accessCode"/>.
        /// </summary>
        /// <param name="credentials">The oauth credentials.</param>
        /// <param name="accessCode">The access code returned from the login flow.</param>
        public static async Task<string> EndOAuthFlow(OAuthCredentials credentials, string accessCode)
        {
            var client = new WebClient();
            var form = new NameValueCollection();
            form.Add("code", accessCode);
            form.Add("client_id", credentials.ClientId);
            form.Add("client_secret", credentials.ClientSecret);
            form.Add("redirect_uri", OAuthRedirectUrl);
            form.Add("grant_type", "authorization_code");

            try
            {
                var response = await client.UploadValuesTaskAsync(OAuthApiUrl, form);
                var decoded = Encoding.UTF8.GetString(response);
                var model = JsonConvert.DeserializeObject<RefreshTokenModel>(decoded);
                return model.RefreshToken;
            }
            catch (WebException ex)
            {
                Debug.WriteLine($"Failed to finalize oauth flow: {ex.Message}");
                throw new OAuthException(ex.Message, ex);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Failed to parse result: {ex.Message}");
                throw new OAuthException(ex.Message, ex);
            }
        }

        private static string ToQueryString(IDictionary<string, string> form)
        {
            return String.Join(
                "&",
                form.Select(x => $"{x.Key}={Uri.EscapeUriString(x.Value)}"));
        }
    }
}
