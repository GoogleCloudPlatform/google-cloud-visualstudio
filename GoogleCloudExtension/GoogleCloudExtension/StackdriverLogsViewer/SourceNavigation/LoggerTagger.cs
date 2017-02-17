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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Define the custom <seealso cref="ITagger{T}"/> for logger methods.
    /// </summary>
    internal class LoggerTagger : ITagger<LoggerTag>
    {
        private static LoggerTag s_emptyLoggerTag = new LoggerTag();
        private static readonly Lazy<LoggerTooltipControl> _tooltipControl = 
            new Lazy<LoggerTooltipControl>(() => new LoggerTooltipControl());
        private readonly IToolTipProvider _toolTipProvider;
        private readonly ITextView _view;
        private readonly ITextBuffer _sourceBuffer;
        private bool _isTooltipShown = false;

        /// <summary>
        /// Tags changed event. Implementation of <seealso cref="ITagger{T}"/> interface.
        /// </summary>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <summary>
        /// Create a new instance of <seealso cref="LoggerTagger"/> class.
        /// </summary>
        /// <param name="view">The text view on which the tag shows.</param>
        /// <param name="sourceBuffer">The source buffer with the text view.</param>
        /// <param name="toolTipProviderFactory">The tool tip provider. <seealso cref="IToolTipProviderFactory"/>. </param>
        public LoggerTagger(ITextView view, ITextBuffer sourceBuffer, IToolTipProviderFactory toolTipProviderFactory)
        {
            _sourceBuffer = sourceBuffer;
            _view = view;
            _view.LayoutChanged += ViewLayoutChanged;
            _toolTipProvider = toolTipProviderFactory.GetToolTipProvider(_view);
            if (_view == LoggerTooltipSource.Current.TextView)
            {
                ShowOrUpdateToolTip();
            }
        }

        /// <summary>
        /// Display tooltip by refreshing the taggers.
        /// </summary>
        public void ShowOrUpdateToolTip()
        {
            if (!LoggerTooltipSource.Current.IsValidSource)
            {
                return;
            }

            SendTagsChangedEvent();
        }

        /// <summary>
        /// Clear tooptip by clearing <seealso cref="LoggerTooltipSource"/> and  refreshing the taggers.
        /// </summary>
        public void ClearTooltip()
        {
            LoggerTooltipSource.Current.Reset();
            if (_isTooltipShown)
            {
                SendTagsChangedEvent();
            }
        }

        /// <summary>
        /// Implement interface <seealso cref="ITagger{T}"/>.
        /// </summary>
        public IEnumerable<ITagSpan<LoggerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (LoggerTooltipSource.Current.TextView != _view || spans.Count == 0)
            {
                Debug.WriteLine($"TooltipSource.TextView != _view is {LoggerTooltipSource.Current.TextView != _view}, spans.Count is {spans.Count}");
                HideTooltip();
                yield break;
            }

            ITextSnapshotLine textLine = _sourceBuffer.CurrentSnapshot.GetLineFromLineNumber((int)LoggerTooltipSource.Current.SourceLine - 1);
            SnapshotSpan span;
            if (String.IsNullOrWhiteSpace(LoggerTooltipSource.Current.MethodName))
            {
                span = new SnapshotSpan(textLine.Start, textLine.Length);
            }
            else
            {
                int pos = textLine.GetText().IndexOf(LoggerTooltipSource.Current.MethodName);
                if (pos < 0)
                {
                    HideTooltip();
                    yield break;
                }
                span = new SnapshotSpan(textLine.Start + pos, LoggerTooltipSource.Current.MethodName.Length);
            }
            yield return new TagSpan<LoggerTag>(span, s_emptyLoggerTag);
            DisplayTooltip(new SnapshotSpan(textLine.Start, textLine.Length));
        }

        /// <summary>
        /// Force an update if the view layout changes
        /// </summary>
        private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            // If a new snapshot is generated, clear the tooltip.
            if (e.NewViewState.EditSnapshot != e.OldViewState.EditSnapshot
                || (_isTooltipShown && !LoggerTooltipSource.Current.IsValidSource))
            {
                ClearTooltip();
            }
            else if (LoggerTooltipSource.Current.IsValidSource 
                // if tooltip is not shown, or if the view port width changes.
                && (!_isTooltipShown || e.NewViewState.ViewportWidth != e.OldViewState.ViewportWidth))
            {
                ShowOrUpdateToolTip();
            }
        }

        private void SendTagsChangedEvent()
        {
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(
                new SnapshotSpan(_sourceBuffer.CurrentSnapshot, 0, _sourceBuffer.CurrentSnapshot.Length)));
        }

        private void HideTooltip()
        {
            _toolTipProvider.ClearToolTip();
            _isTooltipShown = false;
        }

        /// <summary>
        /// Note, the input type is defined as object. It takes <seealso cref="LogItem"/> type in runtime.
        /// The reason of using object is to cheat the MEF loader not to reference LogItem.  
        /// </summary>
        /// <param name="logItem">A <seealso cref="LogItem"/> object.</param>
        private UIElement CreateTooltipControl(object logItem)
        {
            var control = _tooltipControl.Value;
            control.Width = _view.ViewportWidth;
            control.DataContext = logItem;
            return control;
        }

        private void DisplayTooltip(SnapshotSpan span)
        {
            _toolTipProvider.ClearToolTip();
            _isTooltipShown = true;
            this._toolTipProvider.ShowToolTip(
                span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive),
                CreateTooltipControl(LoggerTooltipSource.Current.LogData), 
                PopupStyles.PositionClosest);
        }
    }
}
