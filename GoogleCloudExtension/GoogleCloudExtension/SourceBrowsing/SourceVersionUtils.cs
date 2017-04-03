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

using GoogleCloudExtension.SolutionUtils;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.StackdriverLogsViewer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GoogleCloudExtension.SourceBrowsing
{
    /// <summary>
    /// Helper methods to find, open the source file of the matching revision.
    /// </summary>
    internal static class SourceVersionUtils
    {
        /// <summary>
        /// A map of git sha to <seealso cref="GitCommit"/> object.
        /// </summary>
        private static readonly Dictionary<string, GitCommit> s_localCache = new Dictionary<string, GitCommit>(StringComparer.OrdinalIgnoreCase);

        // TODO: change to Gax constant.
        private const string SourceContextIDLabel = "git_revision_id";

        /// <summary>
        /// Open the source file, move to the source line and show tooltip.
        /// If git sha is present at the log entry, try to open the revision of the file.
        /// If the log item does not contain revision id,
        /// fallback to using the assembly version information.
        /// </summary>
        /// <param name="logItem">The log item to search for source file.</param>
        public static void NavigateToSourceLineCommand(LogItem logItem)
        {
            EnvDTE.Window window;
            try
            {
                // The expression evaluates to nullable bool. Need to explicitly specify == true.
                if (logItem.Entry.Labels?.ContainsKey(SourceContextIDLabel) == true)
                {
                    string sha = logItem.Entry.Labels[SourceContextIDLabel];
                    window = SearchGitRepoAndGetFileContent(sha, logItem.SourceFilePath);

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
                    window = ShellUtils.Open(locatedFilePath);
                    if (window == null)
                    {
                        FailedToOpenFilePrompt(logItem.SourceFilePath);
                        return;
                    }
                }
            }
            catch (ActionCancelledException)
            {
                return;
            }

            //if (locatedFilePath == null)
            //{
            //    FileItemNotFoundPrompt(logItem.SourceFilePath);
            //}

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
                title: Resources.uiDefaultPromptTitle);
        }

        /// <summary>
        /// Prompt when Visual Studio fails to open the source file.
        /// </summary>
        public static void FailedToOpenFilePrompt(string filePath)
        {
            UserPromptUtils.ErrorPrompt(
                message: String.Format(Resources.SourceVersionUtilsFailedOpenFileMessage, filePath),
                title: Resources.uiDefaultPromptTitle);
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
                    throw new ActionCancelledException();
                }
            }

            if (project.Version != logItem.AssemblyVersion && !ContinueWhenVersionMismatch(project, logItem.AssemblyVersion))
            {
                throw new ActionCancelledException();
            }

            return project;
        }

        private static void OpenCurrentVersionProjectPrompt(string assemblyName, string assemblyVersion)
        {
            if (UserPromptUtils.ActionPrompt(
                    prompt: String.Format(Resources.LogsViewerPleaseOpenProjectPrompt, assemblyName, assemblyVersion),
                    title: Resources.uiDefaultPromptTitle,
                    message: Resources.LogsViewerAskToOpenProjectMessage))
            {
                ShellUtils.OpenProject();
            }
            if (!IsCurrentSolutionOpen())
            {
                throw new ActionCancelledException();
            }
        }

        private static void LogEntryVersionInfoMissingPrompt()
        {
            UserPromptUtils.ErrorPrompt(
                message: Resources.LogsViewerVersionInfoMissingMessage,
                title: Resources.uiDefaultPromptTitle);
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
        /// <exception cref="ActionCancelledException">Does not find the revision of file.</exception>
        private static EnvDTE.Window SearchGitRepoAndGetFileContent(string sha, string filePath)
        {
            if (s_localCache.ContainsKey(sha))
            {
                return s_localCache[sha].OpenFileRevision(filePath);
            }

            // There is a chance the file is built from local git repo root.
            GitCommit commit = SearchCommitAtPath(Path.GetDirectoryName(filePath), sha);
            if (commit != null)
            {
                return commit.OpenFileRevision(filePath);
            }

            if (!IsCurrentSolutionOpen())
            {
                OpenProjectFromLocalRepositoryPrompt();
            }
            var solution = SolutionHelper.CurrentSolution;
            commit = solution.Projects.Select(x => SearchCommitAtPath(x.ProjectRoot, sha)).Where(y => y != null).FirstOrDefault();
            if (commit != null)
            {
                return commit.OpenFileRevision(filePath);
            }
            return null;
        }

        private static void OpenProjectFromLocalRepositoryPrompt()
        {
            if (UserPromptUtils.ActionPrompt(
                    prompt: String.Format(Resources.SourceVersionUtilsOpenProjectFromLocalRepoPrompt),
                    title: Resources.uiDefaultPromptTitle,
                    message: Resources.LogsViewerAskToOpenProjectMessage))
            {
                ShellUtils.OpenProject();
            }
            if (!IsCurrentSolutionOpen())
            {
                throw new ActionCancelledException();
            }
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
                    title: Resources.uiDefaultPromptTitle,
                    message: Resources.LogsViewerVersionMissmatchAskIgnoreMessage))
                {
                    StackdriverLogsViewerStates.Current.SetContinueWithVersionMismatchAssemblyFlag();
                }
            }
            return StackdriverLogsViewerStates.Current.ContinueWithVersionMismatchAssemblyFlag;
        }

        private static bool IsCurrentSolutionOpen() => SolutionHelper.CurrentSolution?.Projects?.Count > 0;

        private static GitCommit SearchCommitAtPath(string path, string sha)
        {
            var commit = GitCommit.FindCommit(path, sha);
            if (commit != null)
            {
                s_localCache[sha] = commit;
                return commit;
            }
            return null;
        }
    }
}
