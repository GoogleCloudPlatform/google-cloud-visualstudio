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
using System.Linq;

namespace GoogleCloudExtension.SourceBrowsing
{
    /// <summary>
    /// Represent a local git commit on a given git SHA.
    /// </summary>
    internal class GitCommit
    {
        private readonly GitCommandWrapper _gitCommand;
        private readonly string _sha;
        private readonly Lazy<HashSet<string>> _fileTree;
        
        internal GitCommit(GitCommandWrapper gitCommand, string sha)
        {
            _gitCommand = gitCommand.ThrowIfNull(nameof(gitCommand));
            _sha = sha.ThrowIfNullOrEmpty(nameof(sha));
            _fileTree = new Lazy<HashSet<string>>(
                () => new HashSet<string>(gitCommand.ListTree(sha).Select(x => x.Replace('/', '\\'))));
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
        public static GitCommit FindCommit(string path, string sha)
        {
            string dir = path.ThrowIfNullOrEmpty(nameof(path));
            sha.ThrowIfNullOrEmpty(nameof(sha));
            var gitCommand = GitCommandWrapper.GetGitCommandWrapperForPath(dir);
            return gitCommand?.ContainsCommit(sha) == true ? new GitCommit(gitCommand, sha) : null;
        }

        /// <summary>
        /// Open the file of the revision.
        /// </summary>
        /// <param name="filePath">
        /// The file to be searched for.
        /// The path is from the machine that build the assembly.
        /// So the file path has different root path.
        /// </param>
        public Window OpenFileRevision(string filePath)
        {
            string relativePath;
            var matchingFiles = FindMatchingEntry(filePath).ToList();
            if (matchingFiles.Count == 0)
            {
                throw new Exception(String.Format(
                    Resources.SourceVersionUtilsFailedToLocateFileInRepoMessage,
                    filePath,
                    _sha,
                    _gitCommand.Root));
            }
            if (matchingFiles.Count() > 1)
            {
                var index = PickFileDialog.PickFileWindow.PromptUser(matchingFiles);
                if (index < 0)
                {
                    return null;
                }

                relativePath = matchingFiles[index];
            }
            else
            {
                relativePath = matchingFiles.First();
            }

            var revisionFileContent = _gitCommand.GetRevisionFile(_sha, relativePath);

            // TODO: use the current checked out HEAD version file if file is not changed.
            var window = GitTemporaryFiles.Current.Open(_sha, relativePath, (tmpFile) => SaveTempFile(tmpFile, revisionFileContent));
            return window;
        }

        private void SaveTempFile(string tmpFile, List<string> content)
        {
            using (var sw = new StreamWriter(tmpFile))
            {
                content.ForEach(x => sw.WriteLine(x));
            }
        }

        private string CurrentPath(string relativePath) => Path.Combine(_gitCommand.Root, relativePath);

        private IEnumerable<string> FindMatchingEntry(string filePath) =>
            SubPaths(filePath).Where(x => _fileTree.Value.Contains(x));

        private static IEnumerable<string> SubPaths(string filePath)
        {
            filePath.ThrowIfNullOrEmpty(nameof(filePath));

            int index = filePath.IndexOf(Path.DirectorySeparatorChar);
            for (; index >= 0; index = filePath.IndexOf(Path.DirectorySeparatorChar, index+1))
            {
                yield return filePath.Substring(index + 1);
            }
        }
    }
}
