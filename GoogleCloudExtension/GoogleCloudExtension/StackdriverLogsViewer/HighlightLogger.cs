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
        public static IWpfTextView ShowTip(EnvDTE.Window window)
        {
            IVsTextView textView = GetIVsTextView(window.Document.FullName);
            var wpfView = GetWpfTextView(textView);
            if (LoggingTagger.LoggingTaggerCollection != null)
            {
                LoggingTagger loggingTagger;
                if (LoggingTagger.LoggingTaggerCollection.TryGetValue(wpfView, out loggingTagger))
                {
                    loggingTagger.UpdateAtCaretPosition(wpfView.Caret.ContainingTextViewLine);
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
