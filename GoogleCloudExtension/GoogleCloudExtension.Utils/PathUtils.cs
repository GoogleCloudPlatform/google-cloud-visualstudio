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

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Helper functions for file path operations. 
    /// </summary>
    public static class PathUtils
    {
        /// <summary>
        /// Get the command executable path from PATH environment variable.
        /// </summary>
        /// <param name="commandName">The command name.</param>
        /// <returns>The full path to the command.</returns>
        public static string GetCommandPathFromPATH(string commandName)
        {
            var paths = Environment.GetEnvironmentVariable("PATH")?.Split(';');
            foreach (var path in paths)
            {
                try
                {
                    var fullPath = Path.Combine(path, commandName);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
                catch (Exception ex) when (ex is ArgumentException || ex is ArgumentNullException)
                {
                    Debug.WriteLine($"{path} is considered as invalid path");
                }
            }

            return null;
        }

        /// <summary>
        /// Check if the path is empty.
        /// If the path does not Exists, it returns true too.
        /// </summary>
        /// <param name="path">Folder name</param>
        /// <returns>True: The path is empty or it does not exist.</returns>
        public static bool IsDirectoryEmpty(string path) =>
            !Directory.Exists(path) || !Directory.EnumerateFileSystemEntries(path).Any();

        /// <summary>
        /// Gets the relative path from one directory to another.
        /// </summary>
        /// <param name="fromDirectory">The directory that is the start of</param>
        /// <param name="toDirectory"></param>
        /// <returns></returns>
        public static string GetRelativePath(string fromDirectory, string toDirectory)
        {
            var packageDir = new Uri(toDirectory.EnsureEndSeparator().Replace('\\', '/'));
            var projectDir = new Uri(fromDirectory.EnsureEndSeparator().Replace('\\', '/'));
            return projectDir.MakeRelativeUri(packageDir).ToString().Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
