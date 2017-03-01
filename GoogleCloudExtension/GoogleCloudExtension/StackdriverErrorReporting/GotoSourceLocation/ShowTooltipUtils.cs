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
using GoogleCloudExtension.SolutionUtils;
using LogsViewer = GoogleCloudExtension.StackdriverLogsViewer;
using static GoogleCloudExtension.StackdriverLogsViewer.SourceVersionUtils;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Linq;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Helper methods to show the tooltip to a logger method.
    /// </summary>
    internal static class ShowTooltipUtils
    {
        public static void NavigateToSourceLineCommand(this ErrorGroupItem errorGroupItem, StackFrame stackFrame)
        {
            SolutionHelper solution = null;
            if ((solution = SolutionHelper.CurrentSolution) == null)
            {
                OpenCurrentVersionProjectPrompt(errorGroupItem.SeenIn, "");
                // Check if a solution is open. User can choose not to open solution or project. 
                if ((solution = SolutionHelper.CurrentSolution) == null)
                {
                    return;    // Quit if there is no solution open. 
                }
            }

            var projectItem = solution.FindMatchingSourceFile(stackFrame.SourceFile).FirstOrDefault()?.ProjectItem;
            if (null == projectItem)
            {
                FileItemNotFoundPrompt();
                return;
            }

            var window = ShellUtils.Open(projectItem);
            if (null == window)
            {
                FailedToOpenFilePrompt(stackFrame.SourceFile);
                return;
            }

            ShowToolTip(errorGroupItem, window, stackFrame);
        }

        /// <summary>
        /// Move cursor to the <paramref name="line"/> number of the document window.
        /// </summary>
        /// <param name="window"><seealso cref="Window"/></param>
        /// <param name="line">The line number of the source file.</param>
        public static void GotoLine(Window window, int line)
        {
            TextSelection selection = window.Document.Selection as TextSelection;
            TextPoint tp = selection.TopPoint;
            selection.GotoLine(line, Select: false);
        }

        /// <summary>
        /// Dismiss the logger method tooltip.
        /// </summary>
        public static void HideTooltip()
        {
            if (TooltipSource.Current.TextView == null)
            {
                return;
            }

            TryFindTagger(TooltipSource.Current.TextView)?.ClearTooltip();
        }

        /// <summary>
        /// Show the Logger method tooltip.
        /// </summary>
        /// <param name="logItem">The <seealso cref="LogItem"/> that has the source line information.</param>
        /// <param name="window">The Visual Studio doucment window of the source file.</param>
        public static void ShowToolTip(ErrorGroupItem errorGroupItem, Window window, StackFrame stackFrame)
        {
            GotoLine(window, (int)stackFrame.LineNumber);
            IVsTextView textView = GetIVsTextView(window.Document.FullName);
            var wpfView = GetWpfTextView(textView);
            if (wpfView == null)
            {
                return;
            }
            TooltipSource.Current.Set(
                new TooltipViewModel(errorGroupItem),
                wpfView,
                stackFrame.LineNumber,
                ""); // stackFrame.Function);  TODO: set right function name
            TryFindTagger(wpfView)?.ShowOrUpdateToolTip();
        }

        /// <summary>
        /// Returns an IVsTextView for the given file path if the file is opened in Visual Studio.
        /// </summary>
        /// <param name="filePath">Full Path of the file you are looking for.</param>
        /// <returns>The IVsTextView for this file if it is open. Returns null otherwise.</returns>
        private static IVsTextView GetIVsTextView(string filePath)
        {
            var sp = ShellUtils.GetGloblalServiceProvider();
            IVsUIHierarchy uiHierarchy;
            uint itemID;
            IVsWindowFrame windowFrame;
            if (VsShellUtilities.IsDocumentOpen(
                sp, filePath, Guid.Empty,
                out uiHierarchy, out itemID, out windowFrame))
            {
                // Get the IVsTextView from the windowFrame.
                return VsShellUtilities.GetTextView(windowFrame);
            }

            return null;
        }

        /// <summary>
        /// Get <seealso cref="IWpfTextView"/> interface from <seealso cref="IVsTextView"/> interface.
        /// </summary>
        private static IWpfTextView GetWpfTextView(IVsTextView textView)
        {
            IWpfTextView view = null;
            IVsUserData userData = textView as IVsUserData;
            if (userData != null)
            {
                IWpfTextViewHost viewHost;
                object holder;
                Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
                if (VSConstants.S_OK == userData.GetData(ref guidViewHost, out holder))
                {
                    viewHost = (IWpfTextViewHost)holder;
                    view = viewHost.TextView;
                }
            }
            return view;
        }

        private static LoggerTagger TryFindTagger(IWpfTextView wpfView)
        {
            LoggerTagger tagger = null;
            LoggerTaggerProvider.AllLoggerTaggers.TryGetValue(wpfView, out tagger);
            return tagger;
        }
    }
}
