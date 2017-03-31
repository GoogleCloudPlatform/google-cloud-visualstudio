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
using System.Collections.Generic;
using System.IO;

namespace GoogleCloudExtension.SourceBrowsing
{
    /// <summary>
    /// Helper methods for managing temporary files of git revision.
    /// 
    /// Giving a git SHA and relative file path, returns a temporary file name.
    ///
    /// Windows temp files won't be cleaned up automatically.
    /// The class cleanup files on VS exit event.
    /// </summary>
    internal class GitTempFiles
    {
        private const string TempSubFolder = "GoogleToolsForVS";

        // Use () => new GitTempFiles() here, because the constructor is private.
        private static Lazy<GitTempFiles> s_instance = new Lazy<GitTempFiles>(() => new GitTempFiles());

        private Lazy<string> _folder = new Lazy<string>(
            () => Path.Combine(Path.GetTempPath(), TempSubFolder));

        // Cache the temporary file name.
        // Key is git_sha/relative_path. 
        // Value is the temporary file path.
        private Dictionary<string, string> _tmpFilesMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Singleton of the <seealso cref="GitTempFiles"/> class.
        /// </summary>
        public static GitTempFiles Current => s_instance.Value;

        private GitTempFiles()
        {
            if (!File.Exists(_folder.Value))
            {
                Directory.CreateDirectory(_folder.Value);
            }
            Clean();
            ShellUtils.RegisterShutdownEventHandler(Clean);
        }

        /// <summary>
        /// Returns a cached file path that is saved in prior calls.
        /// Or create the temporary file and save the conteint.
        /// </summary>
        /// <param name="gitSha">The git commint SHA.</param>
        /// <param name="relativePath">Relative path of the target file.</param>
        /// <param name="save">The callback to save the content of the file to the temporary file path.</param>
        /// <returns>Temporary file path.</returns>
        public string GetOrSave(string gitSha, string relativePath, Action<string> save)
        {
            gitSha.ThrowIfNullOrEmpty(nameof(gitSha));
            relativePath.ThrowIfNullOrEmpty(nameof(relativePath));
            save.ThrowIfNull(nameof(save));

            var key = $"{gitSha}/{relativePath}";
            if (_tmpFilesMap.ContainsKey(key))
            {
                return _tmpFilesMap[key];
            }
            else
            {
                var filePath = NewTempFileName(Path.GetFileName(relativePath));
                save(filePath);
                _tmpFilesMap[key] = filePath;
                return filePath;
            }
        }

        private void Clean()
        {
            try
            {
                Array.ForEach(Directory.GetFiles(_folder.Value), File.Delete);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                || ex is DirectoryNotFoundException || ex is PathTooLongException)
            { }
        }

        private string NewTempFileName(string name) => 
            Path.Combine(_folder.Value, $"{Guid.NewGuid()}_{name}");
    }
}
