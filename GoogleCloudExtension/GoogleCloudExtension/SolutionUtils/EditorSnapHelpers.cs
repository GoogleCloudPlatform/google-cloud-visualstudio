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


namespace GoogleCloudExtension.SolutionUtils
{
    internal static class EditorSpanHelpers
    {
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
