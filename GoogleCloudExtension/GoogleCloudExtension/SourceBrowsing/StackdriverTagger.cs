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

using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GoogleCloudExtension.SourceBrowsing
{
    /// <summary>
    /// Define the custom <seealso cref="ITagger{T}"/> for showing a tooltip around a source code line.
    /// </summary>
    internal class StackdriverTagger : ITagger<StackdriverTag>
    {
        private static StackdriverTag s_emptyLoggerTag = new StackdriverTag();
        private readonly IToolTipProvider _toolTipProvider;
        private readonly ITextView _view;
        private readonly ITextBuffer _sourceBuffer;
        private bool _isTooltipShown = false;

        /// <summary>
        /// Tags changed event. Implementation of <seealso cref="ITagger{T}"/> interface.
        /// </summary>
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        /// <summary>
        /// Create a new instance of <seealso cref="StackdriverTagger"/> class.
        /// </summary>
        /// <param name="view">The text view on which the tag shows.</param>
        /// <param name="sourceBuffer">The source buffer with the text view.</param>
        /// <param name="toolTipProviderFactory">The tool tip provider. <seealso cref="IToolTipProviderFactory"/>. </param>
        public StackdriverTagger(ITextView view, ITextBuffer sourceBuffer, IToolTipProviderFactory toolTipProviderFactory)
        {
            _sourceBuffer = sourceBuffer;
            _view = view;
            _view.LayoutChanged += OnViewLayoutChanged;
            _view.LostAggregateFocus += OnLostAggregateFocus;
            _toolTipProvider = toolTipProviderFactory.GetToolTipProvider(_view);
            if (_view == ActiveTagData.Current?.TextView)
            {
                ShowOrUpdateToolTip();
            }
        }



        /// <summary>
        /// Display tooltip by refreshing the taggers.
        /// </summary>
        public void ShowOrUpdateToolTip()
        {
            if (ActiveTagData.Current == null)
            {
                return;
            }

            SendTagsChangedEvent();
        }

        /// <summary>
        /// Clear tooptip by clearing <seealso cref="ActiveTagData"/> and  refreshing the taggers.
        /// </summary>
        public void ClearTooltip()
        {
            ActiveTagData.ResetCurrent();
            if (_isTooltipShown)
            {
                SendTagsChangedEvent();
            }
        }

        /// <summary>
        /// Implement interface <seealso cref="ITagger{T}"/>.
        /// </summary>
        public IEnumerable<ITagSpan<StackdriverTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            // Note, keep a local copy of ActiveTagData.Current. 
            // In case ActiveTagData.Current changes by other thread or by async re-entrancy.
            var activeData = ActiveTagData.Current;
            if (activeData?.TextView != _view || spans.Count == 0)
            {
                Debug.WriteLine($"TooltipSource.TextView != _view is {ActiveTagData.Current?.TextView != _view}, spans.Count is {spans.Count}");
                HideTooltip();
                yield break;
            }

            ITextSnapshotLine textLine = _sourceBuffer.CurrentSnapshot.GetLineFromLineNumber((int)activeData.SourceLine - 1);
            SnapshotSpan span;
            if (String.IsNullOrWhiteSpace(activeData.MethodName))
            {
                var text = textLine.GetText();
                int begin = StringUtils.FirstNonSpaceIndex(text);
                int end = StringUtils.LastNonSpaceIndex(text);
                if (begin == -1 || end == -1)
                {
                    yield break;
                }
                span = new SnapshotSpan(textLine.Start + begin, end - begin + 1);
            }
            else
            {
                int pos = textLine.GetText().IndexOf(activeData.MethodName);
                if (pos < 0)
                {
                    HideTooltip();
                    yield break;
                }
                span = new SnapshotSpan(textLine.Start + pos, activeData.MethodName.Length);
            }
            yield return new TagSpan<StackdriverTag>(span, s_emptyLoggerTag);
            DisplayTooltip(new SnapshotSpan(textLine.Start, textLine.Length));
        }

        private void OnLostAggregateFocus(object sender, EventArgs e)
        {
            if (_isTooltipShown)
            {
                if (ActiveTagData.Current?.TextView == _view)
                {
                    ActiveTagData.ResetCurrent();
                }

                SendTagsChangedEvent();
            }
        }

        /// <summary>
        /// Force an update if the view layout changes
        /// </summary>
        private void OnViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            // If a new snapshot is generated, clear the tooltip.
            if (e.NewViewState.EditSnapshot != e.OldViewState.EditSnapshot
                || (_isTooltipShown && ActiveTagData.Current == null))
            {
                ClearTooltip();
            }
            else if (ActiveTagData.Current != null
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

        private void DisplayTooltip(SnapshotSpan span)
        {
            _toolTipProvider.ClearToolTip();
            _isTooltipShown = true;
            // Note, keep a local copy of ActiveTagData.Current. 
            // In case ActiveTagData.Current changes by other thread or by async re-entrancy.
            ActiveTagData activeData = ActiveTagData.Current;
            if (activeData == null)
            {
                Debug.WriteLine($"{nameof(ActiveTagData.Current)} is empty, this is probably a code bug.");
                return;
            }
            activeData.TooltipControl.Width = _view.ViewportWidth;
            this._toolTipProvider.ShowToolTip(
                span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive),
                activeData.TooltipControl, 
                PopupStyles.PositionClosest);
        }
    }
}
