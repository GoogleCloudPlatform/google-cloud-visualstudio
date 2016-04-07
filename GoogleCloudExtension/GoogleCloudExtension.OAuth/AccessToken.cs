using GoogleCloudExtension.OAuth.Models;
using System;

namespace GoogleCloudExtension.OAuth
{
    public class AccessToken
    {
        public string Token { get; }

        public DateTime ValidUntil { get; }

        public AccessToken(string token, TimeSpan validFor)
        {
            Token = token;
            ValidUntil = DateTime.Now + validFor;
        }

        internal AccessToken(AccessTokenModel accessTokenModel)
        {
            Token = accessTokenModel.Token;
            ValidUntil = DateTime.Now + new TimeSpan(0, 0, accessTokenModel.ExpiresIn);
        }
    }
}