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

using EnvDTE;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace GoogleCloudExtension.SourceBrowsing
{
    /// <summary>
    /// A singleton class that help manage temporary files for Git revision files.
    /// 
    /// Giving a git SHA and relative file path, save the file to a temporary path.
    /// Open the file in a Document window, return the window object.
    ///
    /// Windows temporary files won't be cleaned up automatically.
    /// The class deletes the temporary file immediately after the document is opened.
    /// 
    /// The the document window for the same file is still open,
    /// return the document window so as not to open a new window.
    /// </summary>
    internal class GitTemporaryFiles
    {
        // Use () => new GitTemporaryFiles() here, because the constructor is private.
        private static Lazy<GitTemporaryFiles> s_instance = new Lazy<GitTemporaryFiles>(() => new GitTemporaryFiles());

        private readonly string _tmpFolder;

        /// <summary>
        /// Key is git_sha/relative_path, value is the opened document window.
        /// </summary>
        private Dictionary<string, Window> _fileRevisionWindowMap = new Dictionary<string, Window>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Key is the opened document window, value is the git_sha/relative_path.
        /// </summary>
        private Dictionary<Window, string> _documentWindows = new Dictionary<Window, string>();

        /// <summary>
        /// Returns the singleton of the <seealso cref="GitTemporaryFiles"/> class.
        /// </summary>
        public static GitTemporaryFiles Current => s_instance.Value;

        private GitTemporaryFiles()
        {
            _tmpFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tmpFolder);
            ShellUtils.RegisterWindowCloseEventHandler(OnWindowClose);
        }

        /// <summary>
        /// Returns a cached file path that is saved in prior calls.
        /// Or create the temporary file and save the conteint.
        /// </summary>
        /// <param name="gitSha">The git commint SHA.</param>
        /// <param name="relativePath">Relative path of the target file.</param>
        /// <param name="saveAction">The callback to save the content of the file to the temporary file path.</param>
        /// <returns>The document window that opens the temporary file.</returns>
        public Window Open(string gitSha, string relativePath, Action<string> saveAction)
        {
            gitSha.ThrowIfNullOrEmpty(nameof(gitSha));
            relativePath.ThrowIfNullOrEmpty(nameof(relativePath));
            saveAction.ThrowIfNull(nameof(saveAction));

            Window window;
            var key = $"{gitSha}/{relativePath}";
            if (!_fileRevisionWindowMap.TryGetValue(key, out window))
            {
                // window.Caption can not be modified.
                // Set the file name suffix same as original file name that the document window shows it.
                var filePath = Path.Combine(_tmpFolder, $"tmp_{Path.GetFileName(relativePath)}");
                saveAction(filePath);
                window = OpenDocument(filePath, key);
            }
            return window;
        }

        private Window OpenDocument(string filePath, string key)
        {
            var window = ShellUtils.Open(filePath);
            window.Document.ReadOnly = true;
            File.Delete(filePath);
            if (window != null)
            {
                _fileRevisionWindowMap[key] = window;
                _documentWindows[window] = key;
            }
            return window;
        }

        private void OnWindowClose(Window window)
        {
            string key;
            if (_documentWindows.TryGetValue(window, out key))
            {
                _documentWindows.Remove(window);
                _fileRevisionWindowMap.Remove(key);
            }
        }
    }
}