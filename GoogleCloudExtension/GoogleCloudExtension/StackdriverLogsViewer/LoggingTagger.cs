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
    internal class LoggingTagger : ITagger<LoggingTag>
    {
        private const string LogMethodName = "WriteLog";

        private readonly ITextView _view;
        private ITextSearchService _textSearchService;
        private ITextStructureNavigator _textStructureNavigator;
        private readonly ITextBuffer _sourceBuffer;
        private ITextViewLine _currentViewLine;

        private readonly IToolTipProvider _toolTipProvider;

        public static Dictionary<ITextView, LoggingTagger> LoggingTaggerCollection;

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public LoggingTagger(ITextView view, ITextBuffer sourceBuffer, ITextSearchService textSearchService,
                                   ITextStructureNavigator textStructureNavigator, 
                                   IToolTipProviderFactory toolTipProviderFactory)
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
            if (_view == LogItem.CurrentSourceLineLogItem?.SourceLineTextView)
            {
                UpdateAtCaretPosition(_view.Caret.ContainingTextViewLine);
            }

            // TODO: syncronized access.
            if (LoggingTaggerCollection == null)
            {
                LoggingTaggerCollection = new Dictionary<ITextView, LoggingTagger>();
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
            _currentViewLine = currentViewLine;
            var tempEvent = TagsChanged;
            if (tempEvent != null)
                tempEvent(this, new SnapshotSpanEventArgs(
                    new SnapshotSpan(_sourceBuffer.CurrentSnapshot, 0, _sourceBuffer.CurrentSnapshot.Length)));
        }


        public IEnumerable<ITagSpan<LoggingTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (LogItem.CurrentSourceLineLogItem?.SourceLineTextView != _view)
            {
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

            bool showToolTip = false;

            if (spans.Count > 0)
            {
                // look for 'WriteLog' occurrences
                foreach (SnapshotSpan span in _textSearchService.FindAll(new FindData(LogMethodName,
                    spans[0].Snapshot, 
                    FindOptions.WholeWord | FindOptions.MatchCase | FindOptions.SingleLine, 
                    _textStructureNavigator)))
                {
                    if (_currentViewLine.ContainsBufferPosition(span.Start))
                    {
                        yield return new TagSpan<LoggingTag>(span, new LoggingTag());
                        showToolTip = true;
                        string text;

                        // TODO: handle null return of GetTextViewLineSpan
                        ShowToolTip(EditorSpanHelpers.GetTextViewLineSpan(_view, _currentViewLine, out text).Value);
                    }
                }
            }

            if (showToolTip)
            {
            }
            else
            {
                _toolTipProvider.ClearToolTip();
            }
        }

        private UIElement ToolTipControlDefault(LogItem logItem)
        {
            return new Border
            {
                Background = new SolidColorBrush(Colors.LightGray),
                Padding = new Thickness(10),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children =
                        {
                            new Rectangle
                            {
                                Height = 30,
                                Width = 30,
                                Fill = new SolidColorBrush(Colors.Red)
                            },
                            new TextBlock
                            {
                                Margin = new Thickness(10, 0, 0, 0),
                                Inlines =
                                {
                                    new Run(logItem.Message)
                                }
                            }
                        }
                }
            };
        }

        private UIElement ToolTipControl(LogItem logItem)
        {
            var control = new LoggerTooltip();
            control.DataContext = logItem;
            return control;
        }

        private void ShowToolTip(SnapshotSpan span)
        {
            this._toolTipProvider.ClearToolTip();
            this._toolTipProvider.ShowToolTip(
                span.Snapshot.CreateTrackingSpan(
                    span, SpanTrackingMode.EdgeExclusive),
                    ToolTipControl(LogItem.CurrentSourceLineLogItem), 
                    PopupStyles.PositionClosest);
        }
    }


    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(TextMarkerTag))]
    [ContentType("text")] // only for code portion. Could be changed to csharp to colorize only C# code for example
    internal class GotoTaggerProvider : IViewTaggerProvider
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

            return new LoggingTagger(textView, buffer, TextSearchService, 
                TextStructureNavigatorSelector.GetTextStructureNavigator(buffer),
                ToolTipProviderFactory) as ITagger<T>;
        }
    }

    internal class LoggingTag : TextMarkerTag
    {
        public LoggingTag() : base("Logging") { }
    }

    [Export(typeof(EditorFormatDefinition))]
    [Name("Logging")]
    [UserVisible(true)]
    internal class GotoFormatDefinition : MarkerFormatDefinition
    {
        public GotoFormatDefinition()
        {
            BackgroundColor = Colors.Red;
            ForegroundColor = Colors.White;
            DisplayName = "Logging Word";
            ZOrder = 5;
        }
    }
}