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
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly string _localRoot;
        private readonly string _sha;
        private readonly Lazy<HashSet<string>> _fileTree;
        
        internal GitCommit(GitCommandWrapper gitCommand, string sha, string localRoot)
        {
            _gitCommand = gitCommand.ThrowIfNull(nameof(gitCommand));
            _sha = sha.ThrowIfNullOrEmpty(nameof(sha));
            _localRoot = localRoot.ThrowIfNullOrEmpty(nameof(localRoot));
            _fileTree = new Lazy<HashSet<string>>(() => new HashSet<string>(gitCommand.ListTree(sha)));
        }

        /// <summary>
        /// Search for a Git Commit on a directory. 
        /// The method traversal the directory tree bottom up to check 
        /// if the directory is a valid local repo and contains the git SHA.
        /// </summary>
        /// <param name="path">The path to search for local git repo.</param>
        /// <param name="sha">The git commit SHA to be searched for.</param>
        /// <returns>
        /// A <seealso cref="GitCommit"/> object that represents the git SHA commit at a local repository.
        /// null if not found.
        /// </returns>
        public static GitCommit FindCommit(string path, string sha)
        {
            string dir = path.ThrowIfNullOrEmpty(nameof(path));
            sha.ThrowIfNullOrEmpty(nameof(sha));
            do
            {
                var gitCommand = GitCommandWrapper.GetGitCommandWrapperForPath(dir);
                if (gitCommand != null)
                {
                    return gitCommand.ContainsCommit(sha) ? new GitCommit(gitCommand, sha, dir) : null;
                }
                dir = Directory.GetParent(dir)?.FullName;
            } while (dir != null);
            return null;
        }

        /// <summary>
        /// Get the file of the revision.
        /// This method firstly open the revision of the file in a temporary path.
        /// Comparing the temporary file with the current active version of file.
        /// If the current active file is same as the revision of the file, return the current active file.
        /// Otherwise, return the temporary file.
        /// </summary>
        /// <param name="filePath">The file to be searched for.</param>
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
                    _localRoot));
            }
            if (matchingFiles.Count() > 1)
            {
                var index = PickFileDialog.PickFileWindow.PromptUser(matchingFiles);
                if (index < 0)
                {
                    throw new ActionCancelledException();
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

        private string CurrentPath(string relativePath) => Path.Combine(_localRoot, relativePath);

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
