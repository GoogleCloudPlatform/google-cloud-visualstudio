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
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GitUtils
{
    /// <summary>
    /// Helper methods for clone, create Google Cloud Source Repositories.
    /// </summary>
    public static class CsrGitUtils
    {
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
        /// A <seealso cref="GitRepository"/> object if clone is successful.
        /// Or null if it fails for some reason.
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
        /// <param name="useHttpPath">
        /// True, store credential for the url path. i.e https://abc.com/urlpath
        /// False: store credential for host.
        /// </param>
        /// <returns>
        /// True: if credential is stored successfully.
        /// Otherwise false.
        /// </returns>
        public static bool StoreCredential(string url, string refreshToken, bool useHttpPath = false)
        {
            url.ThrowIfNullOrEmpty(nameof(url));
            refreshToken.ThrowIfNullOrEmpty(nameof(url));

            Uri uri = new Uri(url);
            var uriPartial = useHttpPath ? UriPartial.Path : UriPartial.Authority;
            return WindowsCredentialManager.Write(
                $"git:{uri.GetLeftPart(uriPartial)}",
                username: "VisualStudioUser",
                password: refreshToken,
                credentialType: WindowsCredentialManager.CredentialType.Generic,
                persistenceType: WindowsCredentialManager.CredentialPersistence.LocalMachine);
        }

        /// <summary>
        /// Set global git config useHttpPath for CSR host.
        /// Refer to https://git-scm.com/docs/gitcredentials
        /// </summary>
        public static async Task SetUseHttpPathAsync() =>
            await GitRepository.RunGitCommandAsync(
                $"config --global credential.{CsrUrlAuthority}.useHttpPath true",
                Directory.GetCurrentDirectory());
    }
}