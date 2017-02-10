using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GoogleCloudExtension.SolutionUtils;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    internal class HighlightLogger
    {
        public static void HideTooltip(IWpfTextView wpfView)
        {
            LogTagger.CurrentLogItem = null;
            if (LogTagger.LoggingTaggerCollection != null)
            {
                LogTagger loggingTagger;
                if (LogTagger.LoggingTaggerCollection.TryGetValue(wpfView, out loggingTagger))
                {
                    loggingTagger.ClearTooltip();
                }
            }
        }

        public static IWpfTextView ShowTip(EnvDTE.Window window, LogItem logItem)
        {
            IVsTextView textView = GetIVsTextView(window.Document.FullName);
            var wpfView = GetWpfTextView(textView);
            LogTagger.CurrentLogItem = new LogItemWrapper()
            {
                LogItem = logItem,
                SourceLine = logItem.SourceLine,
                SourceLineTextView = wpfView
            };
            if (LogTagger.LoggingTaggerCollection != null)
            {
                LogTagger loggingTagger;
                if (LogTagger.LoggingTaggerCollection.TryGetValue(wpfView, out loggingTagger))
                {
                    loggingTagger.ShowToolTip();
                }
            }

            return wpfView;
        }

        /// <summary>
        /// Returns an IVsTextView for the given file path, if the given file is open in Visual Studio.
        /// </summary>
        /// <param name="filePath">Full Path of the file you are looking for.</param>
        /// <returns>The IVsTextView for this file, if it is open, null otherwise.</returns>
        private static IVsTextView GetIVsTextView(string filePath)
        {
            var sp = SolutionHelper.GetGloblalServiceProvider();
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

        private static IWpfTextView GetWpfTextView(IVsTextView vTextView)
        {
            IWpfTextView view = null;
            IVsUserData userData = vTextView as IVsUserData;

            if (null != userData)
            {
                IWpfTextViewHost viewHost;
                object holder;
                Guid guidViewHost = DefGuidList.guidIWpfTextViewHost;
                userData.GetData(ref guidViewHost, out holder);
                viewHost = (IWpfTextViewHost)holder;
                view = viewHost.TextView;
            }

            return view;
        }
    }
}
