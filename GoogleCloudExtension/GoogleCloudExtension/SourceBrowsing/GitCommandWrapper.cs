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
using System.Linq;

namespace GoogleCloudExtension.SourceBrowsing
{
    /// <summary>
    /// A wrapper for git commands.
    /// </summary>
    internal class GitCommandWrapper
    {
        private const string GitExecutable = "git.exe";
        private const string GitDefaultLocation = @"Git\cmd\git.exe";
        private const int DefaultGitCommandTimeoutMilliseconds = 2000;

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

        private GitCommandWrapper(string gitLocalRoot)
        {
            Root = RunGitCommand("rev-parse --show-toplevel", gitLocalRoot).FirstOrDefault()?.Replace('/', '\\');
        }

        /// <summary>
        /// Returns a <seealso cref="GitCommandWrapper"/> object 
        /// if the <paramref name="gitLocalRoot"/> is a valid locat Git repository path.
        /// </summary>
        /// <param name="gitLocalRoot">The file path to be checked.</param>
        /// <returns>
        /// <seealso cref="GitCommandWrapper"/> object or null if the path is not a valid git repository path.
        /// </returns>
        public static GitCommandWrapper GetGitCommandWrapperForPath(string gitLocalRoot)
        {
            return IsGitRepository(gitLocalRoot) ? new GitCommandWrapper(gitLocalRoot) : null;
        }

        public static bool IsGitRepository(string gitLocalRoot) => RunGitCommand("log -1", gitLocalRoot)?.Count > 0;

        public bool ContainsCommit(string sha) => ExecCommand($"cat-file -t {sha}").FirstOrDefault() == "commit";

        public List<string> ListTree(string sha) => ExecCommand($"ls-tree -r {sha} --name-only");

        public List<string> GetRevisionFile(string sha, string relativePath)
            => ExecCommand($"show {sha}:{relativePath.Replace('\\', '/')}");

        private List<string> ExecCommand(string command) => RunGitCommand(command, Root);

        private static string GetGitPath()
        {
            // Firstly check default installation location.
            string programPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (programPath.EndsWith("(x86)"))
            {
                programPath = programPath.Replace(" (x86)", "");
            }
            if (File.Exists(Path.Combine(programPath, GitDefaultLocation)))
            {
                return Path.Combine(programPath, GitDefaultLocation);
            }

            return Environment.GetEnvironmentVariable("PATH")
                .Split(';')
                .Select(x => Path.Combine(x, GitExecutable))
                .FirstOrDefault(x => File.Exists(x));
        }

        /// <summary>
        /// Run a git command and return the output or error output.
        /// </summary>
        private static List<string> RunGitCommand(
            string command, 
            string gitLocalRoot, 
            int timeoutMilliseconds = DefaultGitCommandTimeoutMilliseconds)
        {
            List<string> output = new List<string>();
            var t = ProcessUtils.RunCommandAsync(
                    file: GitPath,
                    args: command,
                    handler: (o, e) => output.Add(e.Line),
                    workingDir: gitLocalRoot);
            return t.Wait(timeoutMilliseconds) && t.Result ? output : null;
        }
    }
}
