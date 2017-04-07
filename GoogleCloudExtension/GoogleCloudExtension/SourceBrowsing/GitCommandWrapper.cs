using GoogleCloudExtension.Utils;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.SourceBrowsing
{
    internal class GitCommandWrapper
    {
        private const string GitExecutable = "git.exe";
        private const string GitDefaultLocation = @"Git\cmd\git.exe";
        private const int DefaultGitCommandTimeoutMilliseconds = 2000;

        private static Lazy<string> s_gitPathLazy = new Lazy<string>(GetGitPath);

        private readonly string _localRoot;

        /// <summary>
        /// Gets the git.exe full path if it is installed properly.
        /// Return null if git.exe is not found.
        /// </summary>
        public static string GitPath => s_gitPathLazy.Value;

        private GitCommandWrapper(string gitLocalRoot)
        {
            _localRoot = gitLocalRoot;
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

        public static bool IsGitRepository(string gitLocalRoot) => RunCommand("log - 1", gitLocalRoot)?.Count > 0;

        public bool ContainsCommit(string sha) => RunCommand($"cat-file -t {sha}").FirstOrDefault() == "commit";

        public List<string> ListTree(string sha) => RunCommand($"ls-tree -r {sha} --name-only");

        public List<string> GetRevisionFile(string sha, string relativePath)
            => RunCommand($"show {sha}:{relativePath.Replace('\\', '/')}");

        private List<string> RunCommand(string command) => RunCommand(command, _localRoot);

        private static string GetGitPath()
        {
            // Firstly check default installation location.
            var programPath = Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles);
            if (File.Exists(Path.Combine(programPath, GitDefaultLocation)))
            {
                return Path.Combine(programPath, GitDefaultLocation);
            }

            return Environment.GetEnvironmentVariable("PATH")
                .Split(';')
                .FirstOrDefault(x => File.Exists(Path.Combine(x, GitExecutable)));
        }

        /// <summary>
        /// Run a git command and return the output or error output.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="gitLocalRoot"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        private static List<string> RunCommand(
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
