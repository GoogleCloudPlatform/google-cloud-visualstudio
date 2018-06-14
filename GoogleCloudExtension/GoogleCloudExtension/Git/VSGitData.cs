﻿// Copyright 2017 Google Inc. All Rights Reserved.
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

namespace GoogleCloudExtension.Git
{
    /// <summary>
    /// Get or set Visual Studio git related data.
    /// </summary>
    public static class VsGitData
    {
        private const string VisualStudio2015Version = "14.0";
        private const string VisualStudio2017Version = "15.0";
        private const string Vs14GitKey = @"Software\Microsoft\VisualStudio\14.0\TeamFoundation\GitSourceControl";
        private const string Vs15GitKey = @"Software\Microsoft\VisualStudio\15.0\TeamFoundation\GitSourceControl";

        /// <summary>
        /// Add local repository to Visual Studio registry.
        /// </summary>
        /// <param name="vsVersion">The current Visual Studio version passed in by caller.</param>
        /// <param name="name">The git repository name</param>
        /// <param name="localGitRoot">Git local root</param>
        public static void AddLocalRepositories(string vsVersion, string name, string localGitRoot)
        {
            switch (vsVersion)
            {
                case VisualStudio2015Version:
                    AddRepository(Vs14GitKey, name, localGitRoot);
                    break;
                case VisualStudio2017Version:
                    AddRepository(Vs15GitKey, name, localGitRoot);
                    break;
                default:
                    throw new NotSupportedException($"Version {vsVersion} is not supported.");
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
            switch (vsVersion)
            {
                case VisualStudio2015Version:
                    return RepositoryList(Vs14GitKey);
                case VisualStudio2017Version:
                    return RepositoryList(Vs15GitKey);
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
                    return key?.GetSubKeyNames()
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

        private static void AddRepository(string gitKeyPath, string name, string gitLocalPath)
        {
            try
            {
                using (var key = OpenGitKey(gitKeyPath, "Repositories", writable: true))
                {
                    if (key == null)
                    {
                        return;
                    }

                    using (var newKey = key.CreateSubKey(Guid.NewGuid().ToString()))
                    {
                        newKey.SetValue("Name", name);
                        newKey.SetValue("Path", gitLocalPath);
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
