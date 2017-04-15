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

using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.GitUtils
{
    /// <summary>
    /// A wrapper for executing git commands on a local git repository root.
    /// </summary>
    public class GitRepository
    {
        private const string GitExecutable = "git.exe";

        private static Lazy<string> s_gitPathLazy = new Lazy<string>(GetGitPath);

        /// <summary>
        /// Git repository local root path.
        /// </summary>
        public string Root { get; }

        /// <summary>
        /// Gets the git.exe full path if it is installed properly.
        /// Return null if git.exe is not found.
        /// </summary>
        public static string GitPath => s_gitPathLazy.Value;

        /// <summary>
        /// Returns a <seealso cref="GitRepository"/> object 
        /// if the <paramref name="dir"/> is a valid locat Git repository path.
        /// </summary>
        /// <param name="dir">The file path to be checked.</param>
        /// <returns>
        /// <seealso cref="GitRepository"/> object 
        /// Or null if the path is not a valid git repository path.
        /// </returns>
        public static async Task<GitRepository> GetGitCommandWrapperForPathAsync(string dir)
        {
            if (await IsGitRepositoryAsync(dir))
            {
                var root = (await RunGitCommandAsync("rev-parse --show-toplevel", dir)).FirstOrDefault()?.Replace('/', '\\');
                return new GitRepository(root);
            }
            return null;
        }

        /// <summary>
        /// Returns true if the directory is under a local git repository folder.
        /// </summary>
        /// <param name="dir">The file directory.</param>
        public static async Task<bool> IsGitRepositoryAsync(string dir) => (await RunGitCommandAsync("log -1", dir))?.Count > 0;

        /// <summary>
        /// Returns true if the git repository contains the git SHA revision.
        /// </summary>
        /// <param name="sha">The Git SHA.</param>
        public async Task<bool> ContainsCommitAsync(string sha) => (await ExecCommandAsync($"cat-file -t {sha}")).FirstOrDefault() == "commit";

        /// <summary>
        /// Returns the items tree of a given git SHA revision.
        /// </summary>
        /// <param name="sha">The Git SHA.</param>
        public Task<List<string>> ListTreeAsync(string sha) => ExecCommandAsync($"ls-tree -r {sha} --name-only");

        /// <summary>
        /// Returns the file content of a givin git SHA revision.
        /// </summary>
        /// <param name="sha">The Git SHA.</param>
        /// <param name="relativePath">Relative path to git repository root.</param>
        public Task<List<string>> GetRevisionFileAsync(string sha, string relativePath)
            => ExecCommandAsync($"show {sha}:{relativePath.Replace('\\', '/')}");

        private Task<List<string>> ExecCommandAsync(string command) => RunGitCommandAsync(command, Root);

        private GitRepository(string gitLocalRoot)
        {
            Root = gitLocalRoot;
        }

        private static string GetGitPath()
        {
            return Environment.GetEnvironmentVariable("PATH")
                .Split(';')
                .Select(x => Path.Combine(x, GitExecutable))
                .FirstOrDefault(x => File.Exists(x));
        }

        /// <summary>
        /// Run a git command and return the output or error output.
        /// </summary>
        private static async Task<List<string>> RunGitCommandAsync(
            string command,
            string gitLocalRoot)
        {
            if (!File.Exists(gitLocalRoot) && !Directory.Exists(gitLocalRoot))
            {
                return null;
            }
            List<string> output = new List<string>();
            bool commandResult = await ProcessUtils.RunCommandAsync(
                file: GitPath,
                args: command,
                handler: (o, e) => output.Add(e.Line),
                workingDir: gitLocalRoot);
            return commandResult ? output : null;
        }
    }
}
