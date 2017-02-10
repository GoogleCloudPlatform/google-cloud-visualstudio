using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using GoogleCloudExtension.StackdriverLogsViewer;

namespace GoogleCloudExtension.SolutionUtils
{
    internal static class EditorSpanHelpers
    {
        // Oddly, adding this as member function causes loading the LoggingTagger fails.
        public static bool IsCaretAtLine(this LogTagger tagger)
        {
            if (LogTagger.CurrentLogItem == null || LogTagger.CurrentLogItem.SourceLine <= 0 ||
                tagger._sourceBuffer == null)
            {
                return false;
            }

            ITextSnapshotLine textLine = tagger._sourceBuffer.CurrentSnapshot.GetLineFromLineNumber(
                (int)LogTagger.CurrentLogItem.SourceLine.Value - 1);
            //ITextViewLine viewLine = _view.Caret.ContainingTextViewLine;
            //SnapshotSpan? span = EditorSpanHelpers.GetSpanAtMousePosition(_view as IWpfTextView, null);
            var caretSnapshotPoint = tagger._view.Caret.Position.Point.GetPoint(tagger._sourceBuffer,
                tagger._view.Caret.Position.Affinity);
            var textLineSpan = new SnapshotSpan(textLine.Start, textLine.Length + 1);
            return caretSnapshotPoint.HasValue && textLineSpan.Contains(caretSnapshotPoint.Value);
        }

        public static SnapshotSpan? GetSpanAtMousePosition(IWpfTextView view, ITextStructureNavigator navigator)
        {
            CaretPosition caretPoisition = view.Caret.Position;
            var point = caretPoisition.Point.GetPoint(view.TextBuffer, caretPoisition.Affinity);
            if (!point.HasValue)
            {
                return null;
            }

            return view.GetTextElementSpan(point.Value);
        }

        /// <summary>
        /// This will get the text of the ITextView line as it appears in the actual user editable 
        /// document. 
        /// <returns>The SnapshotSpan of the textViewLine</returns>
        /// </summary>
        public static SnapshotSpan? GetTextViewLineSpan(ITextView textView, ITextViewLine textViewLine, out string text)
        {
            var extent = textViewLine.Extent;
            var bufferGraph = textView.BufferGraph;
            try
            {
                var collection = bufferGraph.MapDownToSnapshot(extent, SpanTrackingMode.EdgeInclusive, textView.TextSnapshot);
                var span = new SnapshotSpan(collection[0].Start, collection[collection.Count - 1].End);
                text = span.ToString();
                return span;
            }
            catch
            {
                text = null;
                return null;
            }
        }
    }
}
