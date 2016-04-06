using GoogleCloudExtension.CredentialsManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.CredentialsManagement
{
    public static class CredentialsManager
    {
        /// <summary>
        /// Returns the access token to use for the current user.
        /// </summary>
        /// <returns></returns>
        public static Task<string> GetAccessTokenAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the access token for the given <paramref name="userCredentials"/>.
        /// </summary>
        /// <param name="userCredentials"></param>
        /// <returns></returns>
        public static Task<string> GetAccessTokenForCredentialsAsync(UserCredentials userCredentials)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the list of credentials known to the extension.
        /// </summary>
        /// <returns></returns>
        public static Task<IEnumerable<UserCredentials>> GetCredentialsListAsync()
        {
            throw new NotImplementedException();
        }
    }
}
