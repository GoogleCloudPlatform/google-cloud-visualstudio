using System.Net;

namespace GoogleCloudExtension.DataSources
{
    internal static class WebClientExtensions
    {
        /// <summary>
        /// Initializes the client with the right header to use the given oauth token.
        /// </summary>
        /// <param name="client">The client to initialize.</param>
        /// <param name="oauthToken">The oauth token to user.</param>
        public static WebClient SetOauthToken(this WebClient client, string oauthToken)
        {
            client.Headers[HttpRequestHeader.Authorization] = $"Bearer {oauthToken}";
            return client;
        }
    }
}
