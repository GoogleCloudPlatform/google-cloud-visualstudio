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


namespace ToolWindow
{
    internal static class SpanHelpers
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
    }
}
