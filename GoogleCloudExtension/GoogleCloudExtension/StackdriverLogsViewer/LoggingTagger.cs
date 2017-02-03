using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.Diagnostics;

using GoogleCloudExtension.SolutionUtils;

using Microsoft.VisualStudio.Text.Adornments;

using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Documents;
using System.Linq;


namespace GoogleCloudExtension.StackdriverLogsViewer
{
    internal class LogItemWrapper
    {
        public object LogItem { get; set; }
        public IWpfTextView SourceLineTextView { get; set; }
        public int SourceLine { get; set; }
    }

    internal class LoggingTagger3 : ITagger<LoggingTag>
    {
        //private static readonly HashSet<string> LogMethodName = new HashSet<string>(
        //    new string[] { "WriteLog", "WriteLogV2" });
        private const string LogMethodName = "WriteLogV2";

        public readonly ITextView _view;
        private ITextSearchService _textSearchService;
        private ITextStructureNavigator _textStructureNavigator;
        public readonly ITextBuffer _sourceBuffer;
        private ITextViewLine _currentViewLine;

        private readonly IToolTipProvider _toolTipProvider;

        public static Dictionary<ITextView, LoggingTagger3> LoggingTaggerCollection;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public static LogItemWrapper CurrentLogItem { get; set; }

        public LoggingTagger3(ITextView view, ITextBuffer sourceBuffer, ITextSearchService textSearchService,
            ITextStructureNavigator textStructureNavigator, IToolTipProviderFactory toolTipProviderFactory)
        {
            _sourceBuffer = sourceBuffer;
            _view = view;
            _textSearchService = textSearchService;
            _textStructureNavigator = textStructureNavigator;
            // Subscribe to both change events in the view - any time the view is updated
            // or the caret is moved, we refresh our list of highlighted words.
            _view.Caret.PositionChanged += CaretPositionChanged;
            _view.LayoutChanged += ViewLayoutChanged;
            _toolTipProvider = toolTipProviderFactory.GetToolTipProvider(_view);
            if (_view == CurrentLogItem?.SourceLineTextView)
            {
                UpdateAtCaretPosition(_view.Caret.ContainingTextViewLine);
            }

            // TODO: syncronized access.
            if (LoggingTaggerCollection == null)
            {
                LoggingTaggerCollection = new Dictionary<ITextView, LoggingTagger3>();
            }
            if (!LoggingTaggerCollection.ContainsKey(view))
            {
                LoggingTaggerCollection.Add(view, this);
            }
            else
            {
                LoggingTaggerCollection[view] = this;
            }

        }

        /// <summary>
        /// Force an update if the view layout changes
        /// </summary>
        private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            //// If a new snapshot wasn't generated, then skip this layout
            //if (e.NewViewState.EditSnapshot != e.OldViewState.EditSnapshot)
            //{
            //    UpdateAtCaretPosition(_view.Caret.ContainingTextViewLine);
            //}
            if (CurrentLogItem != null)
            {
                UpdateAtCaretPosition(_view.Caret.ContainingTextViewLine);
            }
        }

        /// <summary>
        /// Force an update if the caret position changes
        /// </summary>
        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            UpdateAtCaretPosition(_view.Caret.ContainingTextViewLine);
        }

        /// <summary>
        /// Check the caret position. If the caret is on a new word, update the CurrentWord value
        /// </summary>
        public void UpdateAtCaretPosition(ITextViewLine currentViewLine)
        {
            if (CurrentLogItem == null && !showToolTip)
            {
                return;
            }

            _currentViewLine = currentViewLine;
            var tempEvent = TagsChanged;
            if (tempEvent != null)
                tempEvent(this, new SnapshotSpanEventArgs(
                    new SnapshotSpan(_sourceBuffer.CurrentSnapshot, 0, _sourceBuffer.CurrentSnapshot.Length)));
        }

        bool showToolTip = false;

        public IEnumerable<ITagSpan<LoggingTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (CurrentLogItem?.SourceLineTextView != _view)
            {
                _toolTipProvider.ClearToolTip();
                Debug.WriteLine("LogItem.CurrentSourceLineLogItem?.SourceLineTextView != _view");
                yield break;
            }

            if (spans.Count == 0)
                yield break;

            if (_currentViewLine == null)
            {
                yield break;
            }

            if (!_currentViewLine.IsValid)
            {
                yield break;
            }

            showToolTip = false;

            if (spans.Count > 0 &&  this.IsCaretAtLine())
            {

                ITextSnapshotLine textLine = _sourceBuffer.CurrentSnapshot.GetLineFromLineNumber(
                    CurrentLogItem.SourceLine - 1);
                int pos = textLine.GetText().IndexOf(LogMethodName);
                if (pos < 0)
                {
                    yield break;
                }

                var span = new SnapshotSpan(textLine.Start + pos, LogMethodName.Length);

                if (_currentViewLine.ContainsBufferPosition(span.Start))
                {

                    yield return new TagSpan<LoggingTag>(span, new LoggingTag());
                    showToolTip = true;
                    string text;

                    // TODO: handle null return of GetTextViewLineSpan
                    ShowToolTip(EditorSpanHelpers.GetTextViewLineSpan(_view, _currentViewLine, out text).Value);
                }

                //_textSearchService.FindAll(new FindData(LogMethodName,,
                //                    FindOptions.WholeWord | FindOptions.MatchCase | FindOptions.SingleLine,

                //// look for 'WriteLog' occurrences
                //foreach (SnapshotSpan span in _textSearchService.FindAll(new FindData(LogMethodName,
                //    spans[0].Snapshot, 
                //    FindOptions.WholeWord | FindOptions.MatchCase | FindOptions.SingleLine, 
                //    _textStructureNavigator)))
                //{
                //    if (_currentViewLine.ContainsBufferPosition(span.Start))
                //    {
                //        yield return new TagSpan<LoggingTag>(span, new LoggingTag());
                //        showToolTip = true;
                //        string text;

                //        // TODO: handle null return of GetTextViewLineSpan
                //        ShowToolTip(EditorSpanHelpers.GetTextViewLineSpan(_view, _currentViewLine, out text).Value);
                //    }
                //}
            }

            if (showToolTip)
            {
            }
            else
            {
                _toolTipProvider.ClearToolTip();
            }

            yield break;
        }

        private UIElement ToolTipControl(object logItem)
        {
            var control = new LoggerTooltip();
            control.Width = _view.ViewportWidth;
            control.DataContext = logItem;
            return control;
        }

        private void ShowToolTip(SnapshotSpan span)
        {
            _toolTipProvider.ClearToolTip();
            this._toolTipProvider.ShowToolTip(
                span.Snapshot.CreateTrackingSpan(
                    span, SpanTrackingMode.EdgeExclusive),
                    ToolTipControl(CurrentLogItem?.LogItem), 
                    PopupStyles.PositionClosest);
        }
    }


    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(TextMarkerTag))]
    [ContentType("text")] // only for code portion. Could be changed to csharp to colorize only C# code for example
    internal class LoggingTaggerProvider : IViewTaggerProvider
    {
        [Import]
        public IToolTipProviderFactory ToolTipProviderFactory { get; set; }

        [Import]
        internal ITextSearchService TextSearchService { get; set; }

        [Import]
        internal ITextStructureNavigatorSelectorService TextStructureNavigatorSelector { get; set; }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView.TextBuffer != buffer)
                return null;

            return new LoggingTagger3(textView, buffer, TextSearchService, 
                TextStructureNavigatorSelector.GetTextStructureNavigator(buffer),
                ToolTipProviderFactory) as ITagger<T>;
        }
    }

    internal class LoggingTag : TextMarkerTag
    {
        public LoggingTag() : base("LoggingFormat") { }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name("LoggingFormat")]
    [UserVisible(true)]
    internal class GotoFormatDefinition : MarkerFormatDefinition
    {
        public GotoFormatDefinition()
        {
            BackgroundColor = Colors.Yellow;
            ForegroundColor = Colors.Black;
            DisplayName = "Logging Word";
            ZOrder = 5;
        }
    }
}