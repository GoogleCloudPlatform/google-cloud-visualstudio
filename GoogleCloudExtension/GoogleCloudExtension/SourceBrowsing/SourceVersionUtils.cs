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

using GoogleCloudExtension.GitUtils;
using GoogleCloudExtension.ProgressDialog;
using GoogleCloudExtension.SolutionUtils;
using GoogleCloudExtension.StackdriverLogsViewer;
using GoogleCloudExtension.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtension.SourceBrowsing
{
    /// <summary>
    /// Helper methods to find, open the source file of the matching revision.
    /// </summary>
    internal static class SourceVersionUtils
    {
        private const string SourceContextIdLabel = "git_revision_id";

        private static readonly ProgressDialogWindow.Options s_gitOperationOption =
            new ProgressDialogWindow.Options
            {
                Message = Resources.SourceVersionProgressDialogMessage,
                Title = Resources.UiDefaultPromptTitle,
                IsCancellable = false
            };

        /// <summary>
        /// A map of git sha to <seealso cref="GitCommit"/> object.
        /// </summary>
        private static readonly Dictionary<string, GitCommit> s_localCache =
            new Dictionary<string, GitCommit>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Open the source file, move to the source line and show tooltip.
        /// If git sha is present at the log entry, try to open the revision of the file.
        /// If the log item does not contain revision id,
        /// fallback to using the assembly version information.
        /// </summary>
        /// <param name="logItem">The log item to search for source file.</param>
        public static async Task NavigateToSourceLineCommandAsync(LogItem logItem)
        {
            EnvDTE.Window window = null;
            try
            {
                if (logItem.Entry.Labels?.ContainsKey(SourceContextIdLabel) == true)
                {
                    string sha = logItem.Entry.Labels[SourceContextIdLabel];
                    if (!ValidateGitDependencyHelper.ValidateGitForWindowsInstalled())
                    {
                        return;
                    }
                    window = await ProgressDialogWindow.PromptUser(
                        SearchGitRepoAndOpenFileAsync(sha, logItem.SourceFilePath),
                        s_gitOperationOption);
                }
                else
                {   // If the log item does not contain revision id, 
                    // fallback to using assembly version information.
                    var project = FindOrOpenProject(logItem);
                    if (project == null)
                    {
                        Debug.WriteLine($"Failed to find project of {logItem.AssemblyName}");
                        return;
                    }

                    var locatedFilePath = project.FindSourceFile(logItem.SourceFilePath)?.FullName;
                    if (locatedFilePath != null)
                    {
                        window = ShellUtils.Default.Open(locatedFilePath);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                FileItemNotFoundPrompt(ex.FilePath);
                return;
            }
            if (window == null)
            {
                FailedToOpenFilePrompt(logItem.SourceFilePath);
                return;
            }
            logItem.ShowToolTip(window);
        }

        /// <summary>
        /// Prompt when the file item not found.
        /// </summary>
        /// <param name="filePath">The file path to be shown in the error prompt.</param>
        public static void FileItemNotFoundPrompt(string filePath)
        {
            UserPromptUtils.ErrorPrompt(
                message: String.Format(Resources.SourceVersionUtilsFileNotFoundMessage, filePath),
                title: Resources.SourceVersionUtilsUnalbeFindFileTitle);
        }

        /// <summary>
        /// Prompt when Visual Studio fails to open the source file.
        /// </summary>
        public static void FailedToOpenFilePrompt(string filePath)
        {
            UserPromptUtils.ErrorPrompt(
                message: String.Format(Resources.SourceVersionUtilsFailedOpenFileMessage, filePath),
                title: Resources.UiDefaultPromptTitle);
        }

        /// <summary>
        /// Find or open a the project that matches the log item source information.
        /// </summary>
        /// <param name="logItem">The log item that contains source information.</param>
        /// <returns>
        /// null: No solution is opened, or does not find the project of referred by the log item.
        /// a <seealso cref="ProjectHelper"/> object otherwise.
        /// </returns>
        private static ProjectHelper FindOrOpenProject(LogItem logItem)
        {
            if (String.IsNullOrWhiteSpace(logItem.AssemblyName) || String.IsNullOrWhiteSpace(logItem.AssemblyVersion))
            {
                LogEntryVersionInfoMissingPrompt();
                return null;
            }

            if (!IsCurrentSolutionOpen())
            {
                OpenCurrentVersionProjectPrompt(logItem.AssemblyName, logItem.AssemblyVersion);
            }

            ProjectHelper project = null;
            Func<ProjectHelper> getProject =
                () => SolutionHelper.CurrentSolution.Projects?
                .Where(x => x.AssemblyName?.ToLowerInvariant() == logItem.AssemblyName.ToLowerInvariant())
                .FirstOrDefault();

            if ((project = getProject()) == null)
            {
                OpenCurrentVersionProjectPrompt(logItem.AssemblyName, logItem.AssemblyVersion);
                // Check again if the project is opened.
                if ((project = getProject()) == null)
                {
                    return null;
                }
            }

            if (project.Version != logItem.AssemblyVersion && !ContinueWhenVersionMismatch(project, logItem.AssemblyVersion))
            {
                return null;
            }

            return project;
        }

        private static void OpenCurrentVersionProjectPrompt(string assemblyName, string assemblyVersion)
        {
            if (UserPromptUtils.ActionPrompt(
                    prompt: String.Format(Resources.LogsViewerPleaseOpenProjectPrompt, assemblyName, assemblyVersion),
                    title: Resources.UiDefaultPromptTitle,
                    message: Resources.LogsViewerAskToOpenProjectMessage))
            {
                ShellUtils.Default.OpenProject();
            }
            else
            {
                throw new FileNotFoundException(null);
            }
        }

        private static void LogEntryVersionInfoMissingPrompt()
        {
            UserPromptUtils.ErrorPrompt(
                message: Resources.LogsViewerVersionInfoMissingMessage,
                title: Resources.UiDefaultPromptTitle);
        }

        /// <summary>
        /// Try to locate the local git repo and open the revision of the source file
        /// that writes the log entry.
        /// </summary>
        /// <param name="sha">The git commit SHA.</param>
        /// <param name="filePath">The full file path that generates the log item or error event.</param>
        /// <returns>
        /// The file path of the source file revision.
        /// null: Operation is cancelled or failed to open the file revision.
        /// </returns>
        /// <exception cref="FileNotFoundException">Does not find the revision of file.</exception>
        private static async Task<EnvDTE.Window> SearchGitRepoAndOpenFileAsync(string sha, string filePath)
        {
            if (s_localCache.ContainsKey(sha))
            {
                return await OpenGitFileAsync(s_localCache[sha], filePath);
            }
            else
            {
                // There is a chance the file is built from local git repo root.
                if (await SearchCommitAtPathAsync(Path.GetDirectoryName(filePath), sha))
                {
                    return await OpenGitFileAsync(s_localCache[sha], filePath);
                }
            }

            IEnumerable<string> gitPaths = VsGitData.GetLocalRepositories(GoogleCloudExtensionPackage.Instance.VsVersion);
            if (gitPaths != null)
            {
                foreach (var path in gitPaths)
                {
                    if (await SearchCommitAtPathAsync(path, sha))
                    {
                        return await OpenGitFileAsync(s_localCache[sha], filePath);
                    }
                }
            }
            return null;
        }

        private static bool ContinueWhenVersionMismatch(ProjectHelper project, string assemblyVersion)
        {
            if (!StackdriverLogsViewerStates.Current.ContinueWithVersionMismatchAssemblyFlag)
            {
                var prompt = String.Format(
                    Resources.LogsViewerVersionMismatchPrompt,
                    project.UniqueName,
                    project.Version,
                    assemblyVersion);

                if (UserPromptUtils.ActionPrompt(
                    prompt: prompt,
                    title: Resources.SourceVersionUtilsVersionMismatchTitle,
                    message: Resources.LogsViewerVersionMissmatchAskIgnoreMessage))
                {
                    StackdriverLogsViewerStates.Current.SetContinueWithVersionMismatchAssemblyFlag();
                }
            }
            return StackdriverLogsViewerStates.Current.ContinueWithVersionMismatchAssemblyFlag;
        }

        private static bool IsCurrentSolutionOpen() => SolutionHelper.CurrentSolution?.Projects?.Count > 0;

        private static async Task<bool> SearchCommitAtPathAsync(string filePath, string sha)
        {
            GitCommit gitCommit = await GitCommit.FindCommitAsync(filePath, sha);
            if (gitCommit != null)
            {
                s_localCache[sha] = gitCommit;
                return true;
            }
            return false;
        }

        private static async Task<EnvDTE.Window> OpenGitFileAsync(GitCommit commit, string filePath)
        {
            string relativePath;
            var matchingFiles = await commit.FindMatchingEntryAsync(filePath);
            if (matchingFiles.Count() == 0)
            {
                UserPromptUtils.ErrorPrompt(
                    message: String.Format(
                        Resources.SourceVersionUtilsFailedToLocateFileInRepoMessage,
                        filePath, commit.Sha, commit.Root),
                    title: Resources.UiDefaultPromptTitle);
                relativePath = null;
            }
            else if (matchingFiles.Count() > 1)
            {
                var index = PickFileDialog.PickFileWindow.PromptUser(matchingFiles);
                if (index < 0)
                {
                    return null;
                }

                relativePath = matchingFiles.ElementAt(index);
            }
            else
            {
                relativePath = matchingFiles.First();
            }

            if (relativePath == null)
            {
                return null;
            }

            return await OpenGitFile.Current.Open(
                commit.Sha,
                relativePath,
                async (tmpFile) => await commit.SaveTempFileAsync(tmpFile, relativePath));
        }
    }
}
