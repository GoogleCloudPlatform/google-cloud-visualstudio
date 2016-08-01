// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
    /// This is a rather small class that takes care of the OAUTH 2.0 protocol to get at the refresh token for
    /// a user.
    /// </summary>
    internal static class OAuthManager
    {
        /// <summary>
        /// The URL for the oauth service.
        /// </summary>
        private const string OAuthApiUrl = "https://www.googleapis.com/oauth2/v3/token";

        /// <summary>
        /// Keys for the OAUTH 2.0 protocol.
        /// </summary>
        private const string ClientIdKey = "client_id";
        private const string ClientSecretKey = "client_secret";
        private const string GrantTypeKey = "grant_type";
        private const string RedirectUriKey = "redirect_uri";
        private const string RefreshTokenKey = "refresh_token";
        private const string ResponseTypeKey = "response_type";
        private const string ScopeKey = "scope";

        /// <summary>
        /// Values for the OAUTH 2.0 protocol.
        /// </summary>
        private const string AuthorizationCodeValue = "authorization_code";
        private const string CodeValue = "code";

        /// <summary>
        /// The separator for the scopes.
        /// </summary>
        private const string ScopesSeparator = " ";

        /// <summary>
        /// Returns the URL to use to start the OAUTH login flow.
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="scopes"></param>
        public static string GetInitialOAuthUrl(OAuthCredentials credentials, string redirectUrl, IEnumerable<string> scopes)
        {
            var form = new Dictionary<string, string>
            {
                { ResponseTypeKey, CodeValue },
                { ClientIdKey, credentials.ClientId },
                { RedirectUriKey, redirectUrl },
                { ScopeKey, String.Join(ScopesSeparator, scopes) },
            };
            return $"https://accounts.google.com/o/oauth2/auth?{ToQueryString(form)}";
        }

        /// <summary>
        /// Returns the refresh code given the <paramref name="credentials"/> and the <paramref name="accessCode"/>.
        /// </summary>
        /// <param name="credentials">The oauth credentials.</param>
        /// <param name="accessCode">The access code returned from the login flow.</param>
        public static async Task<string> EndOAuthFlow(OAuthCredentials credentials, string redirectUrl, string accessCode)
        {
            var client = new WebClient();
            var form = new NameValueCollection
            {
                { CodeValue, accessCode },
                { ClientIdKey, credentials.ClientId },
                { ClientSecretKey, credentials.ClientSecret },
                { RedirectUriKey, redirectUrl },
                { GrantTypeKey, AuthorizationCodeValue },
            };

            try
            {
                var response = await client.UploadValuesTaskAsync(OAuthApiUrl, form);
                var decoded = Encoding.UTF8.GetString(response);
                var model = JsonConvert.DeserializeObject<IDictionary<string, string>>(decoded);
                return model[RefreshTokenKey];
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

        private static string ToQueryString(IDictionary<string, string> form) =>
            String.Join(
                "&",
                form.Select(x => $"{x.Key}={Uri.EscapeDataString(x.Value)}"));
    }
}
