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

using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace GoogleCloudExtension.SourceBrowsing
{
    public static class GitUtils
    {
        private static string TempFile => System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ".cs";

        public static Commit FindCommit(string projectRoot, string sha)
        {
            string dir = projectRoot;
            do
            {
                dir = Directory.GetParent(dir)?.FullName;
                if (dir == null)
                {
                    break;
                }
                if (!Repository.IsValid(dir))
                {
                    continue;
                }
                var repo = new Repository(dir);
                if (repo != null)
                {
                    Commit commit = repo.Lookup<Commit>(sha);
                    if (commit != null)
                    {
                        var remoteURL = repo.Config.Get<string>("remote", "origin", "url").Value;
                        Debug.WriteLine($"{remoteURL}");
                        return commit;
                    }
                }

            } while (dir != null);

            return null;
        }

        public static string OpenFile(Commit commit, string filePath)
        {
            return FindMatchingEntry(commit, filePath).FirstOrDefault()?.GetFileContent();
        }

        public static string GetFileContent(this TreeEntry treeEntry)
        {
            var blob = (Blob)treeEntry.Target;
            var contentStream = blob.GetContentStream();
            Debug.Assert(blob.Size == contentStream.Length);
            var tempFile = TempFile;
            using (var fileStream = File.Create(tempFile))
            {
                contentStream.Seek(0, SeekOrigin.Begin);
                contentStream.CopyTo(fileStream);
            }

            return tempFile;
        }

        private static IEnumerable<TreeEntry> FindMatchingEntry(Commit commit, string filePath)
        {
            foreach (string subPath in SubPaths(filePath))
            {
                var treeEntry = commit[subPath];
                if (treeEntry != null)
                {
                    yield return treeEntry;
                }
            }
        }

        private static IEnumerable<string> SubPaths(string filePath)
        {
            if (filePath == null)
            {
                yield break;
            }

            int index = filePath.IndexOf(Path.DirectorySeparatorChar);
            for (; index >= 0; index = filePath.IndexOf(Path.DirectorySeparatorChar, index+1))
            {
                yield return filePath.Substring(index + 1);
            }
        }
    }
}
