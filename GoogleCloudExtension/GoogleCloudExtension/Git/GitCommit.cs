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
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Git
{
    /// <summary>
    /// Represent a local git commit on a given git SHA.
    /// </summary>
    public class GitCommit
    {
        private readonly GitRepository _repo;

        private readonly AsyncLazy<Dictionary<string, string>> _fileTree;

        /// <summary>
        /// The git sha.
        /// </summary>
        public string Sha { get; }

        /// <summary>
        /// The local repository root.
        /// </summary>
        public string Root => _repo.Root;

        internal GitCommit(GitRepository gitCommand, string sha)
        {
            _repo = gitCommand.ThrowIfNull(nameof(gitCommand));
            Sha = sha.ThrowIfNullOrEmpty(nameof(sha));
            _fileTree = new AsyncLazy<Dictionary<string, string>>(
                GetFileTreeAsync,
                GoogleCloudExtensionPackage.Instance.JoinableTaskFactory);
        }

        /// <summary>
        /// Search for a local git repository that contains a Git Commit.
        /// </summary>
        /// <param name="path">The path to search for local git repo.</param>
        /// <param name="sha">The git commit SHA to be searched for.</param>
        /// <returns>
        /// A <seealso cref="GitCommit"/> object that represents the git SHA commit at a local repository.
        /// null if the search is not successful.
        /// </returns>
        public static async Task<GitCommit> FindCommitAsync(string path, string sha)
        {
            string dir = path.ThrowIfNullOrEmpty(nameof(path));
            sha.ThrowIfNullOrEmpty(nameof(sha));
            var gitCommand = await GitRepository.GetGitCommandWrapperForPathAsync(dir);
            if (gitCommand == null)
            {
                return null;
            }
            return (await gitCommand.ContainsCommitAsync(sha)) ? new GitCommit(gitCommand, sha) : null;
        }

        /// <summary>
        /// Search for all the possible suffix matching relative file paths.
        /// </summary>
        /// <param name="filePath">
        /// The full file path to be searched for.
        /// The path is from the machine that build the assembly.
        /// So the file path has different root path.
        /// </param>
        /// <returns>File path relative to git root. </returns>
        public async Task<IEnumerable<string>> FindMatchingEntryAsync(string filePath)
        {
            Dictionary<string, string> fileTree = await _fileTree.GetValueAsync();
            return SubPaths(filePath)
                .Select(x =>
                {
                    string y;
                    fileTree.TryGetValue(x, out y);
                    return y;
                })
                .Where(x => x != null);
        }

        /// <summary>
        /// Save the relative file content to a temporary file.
        /// </summary>
        /// <param name="tmpFile">Temporary file path.</param>
        /// <param name="relativePath">The file path relative to the git root.</param>
        public async Task SaveTempFileAsync(string tmpFile, string relativePath)
        {
            List<string> content = await _repo.GetRevisionFileAsync(Sha, relativePath);
            using (var sw = new StreamWriter(tmpFile))
            {
                content.ForEach(x => sw.WriteLine(x));
            }
        }

        private async Task<Dictionary<string, string>> GetFileTreeAsync()
        {
            var fileTree = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in await _repo.ListTreeAsync(Sha))
            {
                fileTree.Add(file.Replace('/', '\\'), file);
            }
            return fileTree;
        }

        private static IEnumerable<string> SubPaths(string filePath)
        {
            filePath.ThrowIfNullOrEmpty(nameof(filePath));

            int index = filePath.IndexOf(Path.DirectorySeparatorChar);
            for (; index >= 0; index = filePath.IndexOf(Path.DirectorySeparatorChar, index + 1))
            {
                yield return filePath.Substring(index + 1);
            }
        }
    }
}
