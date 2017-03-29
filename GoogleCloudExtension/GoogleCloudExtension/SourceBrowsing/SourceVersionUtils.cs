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
using GoogleCloudExtension.StackdriverErrorReporting;
using GoogleCloudExtension.StackdriverLogsViewer;
using System;
using System.Collections.Generic;
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
        /// Global cache of git sha to <seealso cref="GitCommit"/> object.
        /// </summary>
        private static readonly Dictionary<string, GitCommit> s_localCache = new Dictionary<string, GitCommit>(StringComparer.OrdinalIgnoreCase);

        // TODO: change to Gax constant.
        private const string SourceContextIDLabel = "git_revision_id";

        /// <summary>
        /// Try to locate the local git repo and open the revision of the source file
        /// that writes the log entry.
        /// </summary>
        /// <param name="logItem">The log item to search for source file.</param>
        /// <returns>
        /// The file path of source file revision.
        /// Null if not found.
        /// </returns>
        public static string FindGitAndGetFileContent(this LogItem logItem)
        {
            if (logItem.Entry.Labels == null || !logItem.Entry.Labels.ContainsKey(SourceContextIDLabel))
            {
                return null;
            }

            string sha = logItem.Entry.Labels[SourceContextIDLabel];
            if (s_localCache.ContainsKey(sha))
            {
                return s_localCache[sha].GetFile(logItem.SourceFilePath);
            }

            // There is a chance the file path is already contains local git repo root.
            GitCommit commit = SearchCommitAtPath(Path.GetDirectoryName(logItem.SourceFilePath), sha);
            if (commit != null)
            {
                return commit.GetFile(logItem.SourceFilePath);
            }

            if (!IsCurrentSolutionOpen())
            {
                OpenCurrentVersionProjectPrompt(logItem.AssemblyName, logItem.AssemblyVersion);
            }
            var solution = SolutionHelper.CurrentSolution;
            commit = solution.Projects.Select(x => SearchCommitAtPath(x.ProjectRoot, sha)).Where(y => y != null).FirstOrDefault();
            if (commit != null)
            {
                return commit.GetFile(logItem.SourceFilePath);
            }
            return null;
        }

        /// <summary>
        /// Find or open a the project that matches the log item source information.
        /// </summary>
        /// <param name="logItem">The log item that contains source information.</param>
        /// <returns>
        /// null: No solution is opened, or does not find the project of referred by the log item.
        /// a <seealso cref="ProjectHelper"/> object otherwise.
        /// </returns>
        public static ProjectHelper FindOrOpenProject(this LogItem logItem)
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

        private static void LogEntryVersionInfoMissingPrompt()
        {
            UserPromptUtils.ErrorPrompt(
                message: Resources.LogsViewerVersionInfoMissingMessage,
                title: Resources.uiDefaultPromptTitle);
        }

        public static void OpenCurrentVersionProjectPrompt(string assemblyName, string assemblyVersion)
        {
            if (UserPromptUtils.ActionPrompt(
                    prompt: String.Format(Resources.LogsViewerPleaseOpenProjectPrompt, assemblyName, assemblyVersion),
                    title: Resources.uiDefaultPromptTitle,
                    message: Resources.LogsViewerAskToOpenProjectMessage))
            {
                ShellUtils.OpenProject();
            }
            if (SolutionHelper.CurrentSolution?.Projects?.Count == 0)
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
