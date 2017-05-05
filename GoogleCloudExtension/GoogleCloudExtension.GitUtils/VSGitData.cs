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

namespace GoogleCloudExtension.GitUtils
{
    /// <summary>
    /// Get or set Visual Studio git related data.
    /// </summary>
    public static class VSGitData
    {
        private const string VisualStudio2015Version = "14.0";
        private const string VisualStudio2017Version = "15.0";
        private const string VS14GitKey = @"Software\Microsoft\VisualStudio\14.0\TeamFoundation\GitSourceControl";
        private const string VS15GitKey = @"Software\Microsoft\VisualStudio\15.0\TeamFoundation\GitSourceControl";

        /// <summary>
        /// Get local repositories Visual Studio registry.
        /// </summary>
        /// <param name="vsVersion">The current Visual Studio version passed in by caller.</param>
        /// <returns>
        /// A list of local repository paths.
        /// </returns>
        public static IEnumerable<string> GetLocalRepositories(string vsVersion)
        {
            switch (vsVersion)
            {
                case VisualStudio2015Version:
                    return RepositoryList(VS14GitKey);
                case VisualStudio2017Version:
                    return RepositoryList(VS15GitKey);
                default:
                    throw new NotSupportedException($"Version {vsVersion} is not supported.");
            }
        }

        private static RegistryKey OpenGitKey(string gitKeyPath, string path, bool writable = false)
        {
            return Registry.CurrentUser.OpenSubKey(gitKeyPath + "\\" + path, writable);
        }

        private static IEnumerable<string> RepositoryList(string gitKeyPath)
        {
            try
            { 
                using (var key = OpenGitKey(gitKeyPath, "Repositories"))
                {
                    return key.GetSubKeyNames()
                        .Select(x =>
                        {
                            using (var subkey = key.OpenSubKey(x))
                            {
                                var path = subkey?.GetValue("Path") as string;
                                return path != null && Directory.Exists(path) ? path : null;
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
    }
}
