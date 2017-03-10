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
using GoogleCloudExtension.StackdriverErrorReporting;
using ErrorReporting = GoogleCloudExtension.StackdriverErrorReporting;
using GoogleCloudExtension.StackdriverLogsViewer;
using static GoogleCloudExtension.GotoSourceLine.SourceVersionUtils;
using static GoogleCloudExtension.StackdriverLogsViewer.LogWritterNameConstants;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Linq;

namespace GoogleCloudExtension.GotoSourceLine
{
    /// <summary>
    /// Helper methods to show the tooltip to a logger method.
    /// </summary>
    internal static class ShowTooltipUtils
    {
        private static Lazy<ErrorFrameTooltipControl> s_errorReportingControl = new Lazy<ErrorFrameTooltipControl>();
        private static Lazy<LoggerTooltipControl> s_logsViewerControl = new Lazy<LoggerTooltipControl>();

        /// <summary>
        /// Get the logger method name based on the log entry severity.
        /// </summary>
        /// <param name="logLevel">The log entry severity, <seealso cref="LogSeverity"/></param>
        /// <returns>The method name if it is of known log level.  Or empty if not known log level value.</returns>
        public static string GetLoggerMethodName(this LogSeverity logLevel)
        {
            switch (logLevel)
            {
                case LogSeverity.Debug:
                    return DebugMethod;
                case LogSeverity.Emergency:
                    return FatalMethod;
                case LogSeverity.Warning:
                    return WarnMethod;
                case LogSeverity.Info:
                    return InfoMethod;
                case LogSeverity.Error:
                    return ErrorMethod;
                default:
                    return "";
            }
        }

        /// <summary>
        /// Navigate from a parsed stack frame to source code line.
        /// </summary>
        public static void ErrorFrameToSourceLine(
            ErrorGroupItem errorGroupItem, 
            ErrorReporting.StackFrame stackFrame)
        {
            if (errorGroupItem == null || stackFrame == null || !stackFrame.IsWellParsed)
            {
                throw new ArgumentException("Invalid argument");
            }

            SolutionHelper solution = null;
            ProjectItem projectItem = null;
            if ((solution = SolutionHelper.CurrentSolution) != null)
            {
                projectItem = solution.FindMatchingSourceFile(stackFrame.SourceFile).FirstOrDefault()?.ProjectItem;
            }

            if (null == projectItem)
            {
                SourceVersionUtils.FileItemNotFoundPrompt(stackFrame.SourceFile);
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
        private static void GotoLine(Window window, int line)
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
            if (SourceLineToolTipDataSource.Current.TextView == null)
            {
                return;
            }

            TryFindTagger(SourceLineToolTipDataSource.Current.TextView)?.ClearTooltip();
        }

        /// <summary>
        /// Show the Logger method tooltip.
        /// </summary>
        public static void ShowToolTip(ErrorGroupItem errorGroupItem, Window window, ErrorReporting.StackFrame stackFrame)
        {
            GotoLine(window, (int)stackFrame.LineNumber);
            IVsTextView textView = GetIVsTextView(window.Document.FullName);
            var wpfView = GetWpfTextView(textView);
            if (wpfView == null)
            {
                return;
            }
            s_errorReportingControl.Value.DataContext = new ErrorFrameTooltipViewModel(errorGroupItem);
            SourceLineToolTipDataSource.Current.Set(
                wpfView,
                stackFrame.LineNumber,
                s_errorReportingControl.Value,
                "");
            TryFindTagger(wpfView)?.ShowOrUpdateToolTip();
        }

        /// <summary>
        /// Show the Logger method tooltip.
        /// </summary>
        /// <param name="logItem">The <seealso cref="LogItem"/> that has the source line information.</param>
        /// <param name="window">The Visual Studio doucment window of the source file.</param>
        public static void ShowToolTip(this LogItem logItem, Window window)
        {
            GotoLine(window, (int)logItem.SourceLine);        
            IVsTextView textView = GetIVsTextView(window.Document.FullName);
            var wpfView = GetWpfTextView(textView);
            if (wpfView == null)
            {
                return;
            }
            s_logsViewerControl.Value.DataContext = new LoggerTooltipViewModel(logItem);
            SourceLineToolTipDataSource.Current.Set(
                wpfView,
                logItem.SourceLine.Value,
                s_logsViewerControl.Value,
                logItem.LogLevel.GetLoggerMethodName());
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

        private static StackdriverTagger TryFindTagger(IWpfTextView wpfView)
        {
            StackdriverTagger tagger = null;
            LoggerTaggerProvider.AllLoggerTaggers.TryGetValue(wpfView, out tagger);
            return tagger;
        }
    }
}
