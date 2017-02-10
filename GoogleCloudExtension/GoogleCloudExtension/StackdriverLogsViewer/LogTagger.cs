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
        public long? SourceLine { get; set; }
    }

    internal class LogTagger : ITagger<LoggingTag>
    {
        //private static readonly HashSet<string> LogMethodName = new HashSet<string>(
        //    new string[] { "WriteLog", "WriteLogV2" });
        private readonly string[] LogMethodName = 
            new string[] {
                "WriteLogV2",
                ".Info",
                ".Debug",
                ".Error",
                ".Warn",
                ".Fatal"
            };

        public readonly ITextView _view;
        private ITextSearchService _textSearchService;
        private ITextStructureNavigator _textStructureNavigator;
        public readonly ITextBuffer _sourceBuffer;
        // private ITextViewLine _currentViewLine;

        private readonly IToolTipProvider _toolTipProvider;

        public static Dictionary<ITextView, LogTagger> LoggingTaggerCollection;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public static LogItemWrapper CurrentLogItem { get; set; }

        public LogTagger(ITextView view, ITextBuffer sourceBuffer, ITextSearchService textSearchService,
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
                ShowToolTip();
            }

            // TODO: syncronized access.
            if (LoggingTaggerCollection == null)
            {
                LoggingTaggerCollection = new Dictionary<ITextView, LogTagger>();
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
            // If a new snapshot wasn't generated, then skip this layout
            if (e.NewViewState.EditSnapshot != e.OldViewState.EditSnapshot)
            {
                ClearTooltip();
            }
            else
            if (CurrentLogItem != null)
            {
                ShowToolTip();
            }
        }

        /// <summary>
        /// Force an update if the caret position changes
        /// </summary>
        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
        {
            // UpdateAtCaretPosition(_view.Caret.ContainingTextViewLine);
        }

        private void SendTagsChangedEvent()
        {
            // _currentViewLine = currentViewLine;
            var tempEvent = TagsChanged;
            if (tempEvent != null)
                tempEvent(this, new SnapshotSpanEventArgs(
                    new SnapshotSpan(_sourceBuffer.CurrentSnapshot, 0, _sourceBuffer.CurrentSnapshot.Length)));
        }

        bool showToolTip = false;

        public void ClearTooltip()
        {
            CurrentLogItem = null;
            SendTagsChangedEvent();
        }

        /// <summary>
        /// Check the caret position. If the caret is on a new word, update the CurrentWord value
        /// </summary>
        public void ShowToolTip()
        {
            if (CurrentLogItem == null)
            {
                return;
            }

            SendTagsChangedEvent();
        }

        public IEnumerable<ITagSpan<LoggingTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (CurrentLogItem?.SourceLineTextView != _view)
            {
                _toolTipProvider.ClearToolTip();
                showToolTip = false;
                Debug.WriteLine("LogItem.CurrentSourceLineLogItem?.SourceLineTextView != _view");
                yield break;
            }

            if (spans.Count == 0)
            {
                _toolTipProvider.ClearToolTip();
                showToolTip = false;
                Debug.WriteLine("spans.Count == 0");
                yield break;
            }

            //if (_currentViewLine == null)
            //{
            //    yield break;
            //}

            //if (!_currentViewLine.IsValid)
            //{
            //    yield break;
            //}

            showToolTip = false;

            if (spans.Count > 0) //  &&  this.IsCaretAtLine())
            {

                ITextSnapshotLine textLine = _sourceBuffer.CurrentSnapshot.GetLineFromLineNumber(
                    (int)CurrentLogItem.SourceLine - 1);
                SnapshotSpan span;
                string methodName = LogItem.MethodName(CurrentLogItem.LogItem as LogItem);
                if (String.IsNullOrWhiteSpace(methodName))
                {
                    span = new SnapshotSpan(textLine.Start, textLine.Length);
                }
                else
                {
                    int pos = textLine.GetText().IndexOf(methodName);
                    if (pos < 0)
                    {
                        yield break;
                    }

                    span = new SnapshotSpan(textLine.Start + pos, methodName.Length);
                }

                //if (_currentViewLine.ContainsBufferPosition(span.Start))
                //{

                    yield return new TagSpan<LoggingTag>(span, new LoggingTag());
                    showToolTip = true;
                    string text;

                // TODO: handle null return of GetTextViewLineSpan
                // ShowToolTip(EditorSpanHelpers.GetTextViewLineSpan(_view, _currentViewLine, out text).Value);
                ShowToolTip(new SnapshotSpan(textLine.Start, textLine.Length));
                //}

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

            return new LogTagger(textView, buffer, TextSearchService, 
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