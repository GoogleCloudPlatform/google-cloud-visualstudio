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
using LibGit2Sharp;
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
        private readonly Commit _commit;
        private readonly Repository _repo;
        private readonly string _localRoot;
        
        internal GitCommit(Commit commit, Repository repo, string localRoot)
        {
            _commit = commit.ThrowIfNull(nameof(commit));
            _repo = repo.ThrowIfNull(nameof(repo));
            _localRoot = localRoot.ThrowIfNullOrEmpty(nameof(localRoot));
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
                if (Repository.IsValid(dir))
                {
                    var repo = new Repository(dir);
                    Commit commit = repo.Lookup<Commit>(sha);
                    if (commit != null)
                    {
                        return new GitCommit(commit, repo, dir);
                    }
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
            TreeEntry treeEntry;
            var matchingFiles = FindMatchingEntry(filePath).ToList();
            if (matchingFiles.Count == 0)
            {
                throw new Exception(String.Format(
                    Resources.SourceVersionUtilsFailedToLocateFileInRepoMessage,
                    filePath,
                    _commit.Sha,
                    _localRoot));
            }
            if (matchingFiles.Count() > 1)
            {
                var index = PickFileDialog.PickFileWindow.PromptUser(matchingFiles.Select(x => x.Path));
                if (index < 0)
                {
                    throw new ActionCancelledException();
                }

                treeEntry = matchingFiles[index];
            }
            else
            {
                treeEntry = matchingFiles.First();
            }
            var window = GitTempFiles.Current.Open(_commit.Sha, treeEntry.Path, (tmpFile) => SaveTempFile(treeEntry, tmpFile));
            return window;
            //var currentPath = CurrentPath(treeEntry);
            //return _repo.RetrieveStatus(treeEntry.Path) == FileStatus.Unaltered && FileCompare(currentPath, tmpPath) ? currentPath : tmpPath;            
        }

        private void SaveTempFile(TreeEntry treeEntry, string filePath)
        {
            var blob = (Blob)treeEntry.Target;
            using (var contentStream = blob.GetContentStream())
            {
                Debug.Assert(blob.Size == contentStream.Length);
                contentStream.Seek(0, SeekOrigin.Begin);
                using (var fileStream = File.Create(filePath))
                {
                    contentStream.CopyTo(fileStream);
                }
            }
        }

        private string CurrentPath(TreeEntry treeEntry) => Path.Combine(_localRoot, treeEntry.Path);

        private IEnumerable<TreeEntry> FindMatchingEntry(string filePath) =>
            SubPaths(filePath).Select(x => _commit[x]).Where(y => y != null);

        private static IEnumerable<string> SubPaths(string filePath)
        {
            filePath.ThrowIfNullOrEmpty(nameof(filePath));

            int index = filePath.IndexOf(Path.DirectorySeparatorChar);
            for (; index >= 0; index = filePath.IndexOf(Path.DirectorySeparatorChar, index+1))
            {
                yield return filePath.Substring(index + 1);
            }
        }

        /// <summary>
        /// This method compare two files line by line.
        /// </summary>
        /// <returns>
        /// True: the two files content are same.
        /// False: thw two files differs.
        /// </returns>
        private bool FileCompare(string file1, string file2)
        {
            string line1, line2;
            using (var fs1 = new StreamReader(file1))
            using (var fs2 = new StreamReader(file2))
            {
                do
                {
                    line1 = fs1.ReadLine();
                    line2 = fs2.ReadLine();
                    if (line1 != line2)
                    {
                        return false;
                    }
                }
                while (!fs1.EndOfStream && !fs2.EndOfStream);
                return fs1.EndOfStream && fs2.EndOfStream;
            }
        }
    }
}
