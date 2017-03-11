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
        private readonly string[] _lines;

        /// <summary>
        /// The unparsed exception message.
        /// </summary>
        public string RawMessage { get; }

        /// <summary>
        /// The header part of the stack message.
        /// The value can be null.
        /// </summary>
        public string Header { get; }

        /// <summary>
        /// Gets the stack frames.
        /// Please note, this uses lazy evaluation.
        /// It can be empty.
        /// </summary>
        public List<StackFrame> StackFrames => ParseFrames();

        /// <summary>
        /// Flag to indicate if parsed frames tab should be hidden or shown.
        /// </summary>
        public bool ShowParsedFrames => StackFrames.Any(x => x.IsWellParsed);

        /// <summary>
        /// Return first frame summary.
        /// </summary>
        public string FirstFrameSummary => StackFrames.FirstOrDefault()?.SummaryText;

        public ParsedException(string exceptionMessage)
        {
            if (exceptionMessage == null)
            {
                throw new ErrorReportingException(new ArgumentNullException(nameof(exceptionMessage)));
            }
            RawMessage = exceptionMessage;
            _lines = ParserUtils.SplitLines(exceptionMessage);
            Header = ParserUtils.SeparateHeaderFromInnerError(_lines.FirstOrDefault());
        }

        private List<StackFrame> ParseFrames()
        {
            if (_lines.Length <= 1)
            {
                return null;
            }

            return _lines.Skip(1).Select(x => new StackFrame(x)).ToList();
        }
    }
}
