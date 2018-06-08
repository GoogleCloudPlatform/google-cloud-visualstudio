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

using EnvDTE;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GoogleCloudExtension.SourceBrowsing
{
    /// <summary>
    /// A singleton class that help locate files for Git revision files.
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
    internal class OpenGitFile
    {
        // Use () => new GitTemporaryFiles() here, because the constructor is private.
        private static Lazy<OpenGitFile> s_instance = new Lazy<OpenGitFile>(() => new OpenGitFile());

        /// <summary>
        /// Key is git_sha/relative_path, value is the opened document window.
        /// </summary>
        private Dictionary<string, Window> _fileRevisionWindowMap =
            new Dictionary<string, Window>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Key is the opened document window, value is the git_sha/relative_path.
        /// </summary>
        private Dictionary<Window, string> _documentWindows = new Dictionary<Window, string>();

        /// <summary>
        /// Returns the singleton of the <seealso cref="OpenGitFile"/> class.
        /// </summary>
        public static OpenGitFile Current => s_instance.Value;

        /// <summary>
        /// Returns a cached file path that is saved in prior calls.
        /// Or create the temporary file and save the content.
        /// </summary>
        /// <param name="gitSha">The git commint SHA.</param>
        /// <param name="relativePath">Relative path of the target file.</param>
        /// <param name="saveAction">The callback to save the content of the file to the temporary file path.</param>
        /// <returns>The document window that opens the temporary file.</returns>
        public async Task<Window> Open(string gitSha, string relativePath, Func<string, Task> saveAction)
        {
            gitSha.ThrowIfNullOrEmpty(nameof(gitSha));
            relativePath.ThrowIfNullOrEmpty(nameof(relativePath));

            Window window;
            var key = $"{gitSha}/{relativePath}";
            if (!_fileRevisionWindowMap.TryGetValue(key, out window))
            {
                string tmpFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tmpFolder);

                // window.Caption can not be modified, the file name is the caption of the window.
                // Format the file name so that:
                // (1) It clear it's not original file.
                // (2) User still can figure out the original file name by looking at the new name.
                // (3) Different git revision has different name.
                var name = Path.GetFileNameWithoutExtension(relativePath);
                var extention = Path.GetExtension(relativePath);
                var filePath = Path.Combine(tmpFolder, $"tmp.{name}.{gitSha.GetHashCode():x}{extention}");
                await saveAction(filePath);
                window = OpenDocument(filePath, key);

                try
                {
                    File.Delete(filePath);
                    Directory.Delete(tmpFolder);
                }
                catch (IOException)
                {
                    // Ignore I/O exception at deletion.
                }
            }
            return window;
        }

        private OpenGitFile()
        {
            ShellUtils.Default.RegisterWindowCloseEventHandler(OnWindowClose);
        }

        private Window OpenDocument(string filePath, string key)
        {
            var window = ShellUtils.Default.Open(filePath);
            window.Document.ReadOnly = true;
            _fileRevisionWindowMap[key] = window;
            _documentWindows[window] = key;
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
