// Copyright 2017 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GitUtils
{
    /// <summary>
    /// Helper methods for clone, create Google Cloud Source Repositories.
    /// </summary>
    public static class CsrGitUtils
    {
        /// <summary>
        /// To use refresh token to access CSR, the user name is required to be this constant name.
        /// </summary>
        private const string CsrRefreshTokenAccessUserName = "VisualStudioUser";

        /// <summary>
        /// The Google Cloud Source Repositories url scheme + host part.
        /// </summary>
        public const string CsrUrlAuthority = "https://source.developers.google.com";

        /// <summary>
        /// Clone a Google Cloud Source Repository locally.
        /// </summary>
        /// <param name="url">The repository remote URL.</param>
        /// <param name="localPath">Local path to save the repository</param>
        /// <returns>
        /// A <seealso cref="GitRepository"/> object.
        /// </returns>
        /// <exception cref="GitCommandException">Throw when git command fails</exception>
        public static async Task<GitRepository> CloneAsync(string url, string localPath)
        {
            if (Directory.Exists(localPath))
            {
                throw new ArgumentException($"{localPath} arleady exists.");
            }

            Directory.CreateDirectory(localPath);

            // git clone https://host/myrepo/ "c:\git\myrepo" --config credential.helper=manager
            string command = $@"clone {url} ""{localPath}"" --config credential.helper=manager";
            var output = await GitRepository.RunGitCommandAsync(command, localPath);
            Debug.WriteLine(output.FirstOrDefault() ?? "");
            return await GitRepository.GetGitCommandWrapperForPathAsync(localPath);
        }

        /// <summary>
        /// Store credential using git-credential-manager
        /// </summary>
        /// <param name="url">The repository url.</param>
        /// <param name="refreshToken">Google cloud credential refresh token.</param>
        /// <param name="pathOption"><seealso cref="StoreCredentialPathOption"/> </param>
        /// <returns>
        /// True: if credential is stored successfully.
        /// Otherwise false.
        /// </returns>
        public static bool StoreCredential(
            string url,
            string refreshToken,
            StoreCredentialPathOption pathOption)
        {
            url.ThrowIfNullOrEmpty(nameof(url));
            refreshToken.ThrowIfNullOrEmpty(nameof(refreshToken));

            Uri uri = new Uri(url);
            UriPartial uriPartial;
            switch (pathOption)
            {
                case StoreCredentialPathOption.UrlPath:
                    uriPartial = UriPartial.Path;
                    break;
                case StoreCredentialPathOption.UrlHost:
                    uriPartial = UriPartial.Authority;
                    break;
                default:
                    throw new ArgumentException(nameof(pathOption));
            }
            return WindowsCredentialManager.Write(
                $"git:{uri.GetLeftPart(uriPartial)}",
                username: CsrRefreshTokenAccessUserName,
                password: refreshToken,
                credentialType: WindowsCredentialManager.CredentialType.Generic,
                persistenceType: WindowsCredentialManager.CredentialPersistence.LocalMachine);
        }

        /// <summary>
        /// Set global git config useHttpPath for CSR host.
        /// Refer to https://git-scm.com/docs/gitcredentials
        /// </summary>
        public static Task SetUseHttpPathAsync() =>
            GitRepository.RunGitCommandAsync(
                $"config --global credential.{CsrUrlAuthority}.useHttpPath true",
                Directory.GetCurrentDirectory());

        /// <summary>
        /// Refer to <seealso cref="StoreCredential(string, string, StoreCredentialPathOption)"/>.
        /// Store credential path option.
        /// </summary>
        public enum StoreCredentialPathOption
        {
            /// <summary>
            /// Store credential for the host. 
            /// Example: https://source.developers.google.com
            /// </summary>
            UrlHost,

            /// <summary>
            /// Store credential for the UrlPath. 
            /// Example: https://source.developers.google.com/p/project-id/r/repo1 
            /// </summary>
            UrlPath
        }
    }
}