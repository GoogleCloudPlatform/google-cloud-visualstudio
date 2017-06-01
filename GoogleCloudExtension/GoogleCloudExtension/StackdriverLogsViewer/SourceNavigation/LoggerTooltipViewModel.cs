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

using GoogleCloudExtension.SourceBrowsing;
using GoogleCloudExtension.Utils;
using System.Diagnostics;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// View model to <seealso cref="LoggerTooltipControl"/>.
    /// </summary>
    internal class LoggerTooltipViewModel : Model
    {
        private bool _filterLogsOfSourceLine = true;

        /// <summary>
        /// The close button command
        /// </summary>
        public ProtectedCommand OnCloseButtonCommand { get; }

        /// <summary>
        /// Command responses to the back to logs viewer button.
        /// </summary>
        public ProtectedCommand BackToLogsViewerCommand { get; }

        /// <summary>
        /// Indicate if it opens an advanced    
        /// </summary>
        public bool FilterLogsOfSourceLine
        {
            get { return _filterLogsOfSourceLine; }
            set { SetValueAndRaise(ref _filterLogsOfSourceLine, value); }
        }

        /// <summary>
        /// The log item do display in tooltip.
        /// </summary>
        public LogItem Log { get; }

        /// <summary>
        /// Initializes a new instance of <seealso cref="LoggerTooltipViewModel"/> class.
        /// </summary>
        /// <param name="log">The log item the tooltip shows.</param>
        public LoggerTooltipViewModel(LogItem logItem)
        {
            OnCloseButtonCommand = new ProtectedCommand(ShowTooltipUtils.HideTooltip);
            BackToLogsViewerCommand = new ProtectedCommand(BackToLogsViewer);
            Log = logItem;
        }

        private void BackToLogsViewer()
        {
            var window = ToolWindowCommandUtils.ShowToolWindow<LogsViewerToolWindow>();
            if (Log.Entry == null || window == null)
            {
                Debug.WriteLine("Entry or window is null, this is likely a code bug");
                return;
            }

            if (FilterLogsOfSourceLine)
            {
                window.FilterOnSourceLocation(Log);
            }
        }
    }
}
