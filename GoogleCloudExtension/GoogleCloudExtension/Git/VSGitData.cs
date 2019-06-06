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

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using GoogleCloudExtension.VsVersion;

namespace GoogleCloudExtension.Git
{
    /// <summary>
    /// Get or set Visual Studio git related data.
    /// </summary>
    public static class VsGitData
    {

        /// <summary>
        /// Add local repository to Visual Studio registry.
        /// </summary>
        /// <param name="vsVersion">The current Visual Studio version passed in by caller.</param>
        /// <param name="name">The git repository name</param>
        /// <param name="localGitRoot">Git local root</param>
        public static void AddLocalRepositories(string vsVersion, string name, string localGitRoot)
        {
            AddRepository(GetGitKey(vsVersion), name, localGitRoot);
        }

        private static string GetGitKey(string vsVersion)
        {
            switch (vsVersion)
            {
                case VsVersionUtils.VisualStudio2015Version:
                case VsVersionUtils.VisualStudio2017Version:
                case VsVersionUtils.VisualStudio2019Version:
                    return $@"Software\Microsoft\VisualStudio\{vsVersion}\TeamFoundation\GitSourceControl";
                default:
                    throw new ArgumentException($"Version {vsVersion} is not supported.", nameof(vsVersion));

            }
        }

        /// <summary>
        /// Get local repositories from Visual Studio registry.
        /// </summary>
        /// <param name="vsVersion">The current Visual Studio version passed in by caller.</param>
        /// <returns>
        /// A list of local repository paths.
        /// </returns>
        public static IEnumerable<string> GetLocalRepositories(string vsVersion)
        {
            return RepositoryList(GetGitKey(vsVersion));
        }

        private static RegistryKey OpenGitKey(string gitKeyPath, string path, bool writable = false)
        {
            return Registry.CurrentUser.OpenSubKey(gitKeyPath + "\\" + path, writable);
        }

        private static IEnumerable<string> RepositoryList(string gitKeyPath)
        {
            try
            {
                using (RegistryKey key = OpenGitKey(gitKeyPath, "Repositories"))
                {
                    return key?.GetSubKeyNames()
                        .Select(x =>
                        {
                            using (RegistryKey subkey = key.OpenSubKey(x))
                            {
                                if (subkey?.GetValue("Path") is string path && Directory.Exists(path))
                                {
                                    return path;
                                }
                                else
                                {
                                    return null;
                                }
                            }
                        })
                        .Where(x => x != null)
                        .ToList();
                }
            }
            catch (Exception ex) when (
                ex is SecurityException ||
                ex is ObjectDisposedException || // The RegistryKey is closed (closed keys cannot be accessed).
                ex is UnauthorizedAccessException ||
                ex is IOException
                )
            {
                return null;
            }
        }

        private static void AddRepository(string gitKeyPath, string name, string gitLocalPath)
        {
            try
            {
                using (RegistryKey key = OpenGitKey(gitKeyPath, "Repositories", writable: true))
                {

                    using (RegistryKey newKey = key?.CreateSubKey(Guid.NewGuid().ToString()))
                    {
                        newKey?.SetValue("Name", name);
                        newKey?.SetValue("Path", gitLocalPath);
                    }
                }
            }
            catch (Exception ex) when (
                ex is SecurityException ||
                ex is ObjectDisposedException || // The RegistryKey is closed (closed keys cannot be accessed).
                ex is UnauthorizedAccessException ||
                ex is IOException
                )
            { }
        }
    }
}
