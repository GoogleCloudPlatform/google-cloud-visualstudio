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
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Exact header and stack frames from the Stackdriver Error Reporting error event message.
    /// </summary>
    public class ParsedException : Model
    {
        /// <summary>
        /// "\r\n" (\u000D\u000A) for Windows
        /// "\n" (\u000A) for Unix
        /// "\r" (\u000D) for Mac(if such implementation existed)
        /// </summary>
        private static readonly string[] s_lineBreaks = new string[] { "\r\n", "\n", "\r" };
        private readonly string[] _lines;

        /// <summary>
        /// Gets the owning <seealso cref="ErrorGroupItem"/> object.
        /// </summary>
        public ErrorGroupItem OwningParentObj { get; }

        /// <summary>
        /// The unparsed exception message.
        /// </summary>
        public string RawMessage { get; }

        /// <summary>
        /// The header part of the stack message.
        /// The value can be null.
        /// </summary>
        public string Header => _lines.Length > 0 ? _lines[0] : null;

        /// <summary>
        /// Gets the stack frames.
        /// Please note, this uses lazy evaluation.
        /// It can be empty.
        /// </summary>
        public List<StackFrame> StackFrames => ParseFrames();

        /// <summary>
        /// Flag to indicate if parsed frames 
        /// </summary>
        public bool ShowParsedFrames => StackFrames.Any(x => x.IsWellParsed);

        /// <summary>
        /// Return first frame summary.
        /// </summary>
        public string FirstFrameSummary => StackFrames.FirstOrDefault()?.SummaryText;

        public ParsedException(string exceptionMessage, ErrorGroupItem parent)
        {
            if (exceptionMessage == null)
            {
                throw new ErrorReportingException(new ArgumentNullException(nameof(exceptionMessage)));
            }
            RawMessage = exceptionMessage;
            _lines = RawMessage.Split(s_lineBreaks, StringSplitOptions.RemoveEmptyEntries);
            OwningParentObj = parent;
        }

        private List<StackFrame> ParseFrames()
        {
            if (_lines.Length <= 1)
            {
                return null;
            }

            List<StackFrame> frameList = new List<StackFrame>();
            for (int i = 1; i < _lines.Length; ++i)
            {
                var frame = new StackFrame(_lines[i], this);
                frameList.Add(frame);
            }

            return frameList;
        }
    }
}
