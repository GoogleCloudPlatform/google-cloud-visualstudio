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
using System.Text.RegularExpressions;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Stack frame may or may not contain source file name and line number.
    /// For those that does, they are parsed and set Parsed flag to true.
    /// 
    /// This class only attempt to parse C# stacks. 
    /// For non-C# language stack, it remain not parsed.
    /// 
    /// The Regex is defined as:
    /// stack_frame = at <method>(<arguments>) [in <source_location>]]
    /// source_location = <file_name>:line <number>
    /// 
    /// Below is an example:
    ///  at GoogleCloudExtensionUnitTests.StackframeParserTests.SelfLoop(Int32 count) in C:\\git\\wind\\GoogleCloudExtension\\GoogleCloudExtensionUnitTests\\StackframeParserTests.cs:line 46
    /// </summary>
    public class StackFrame : Model
    {
        private const string MethodGroup = "method";
        private const string FileNameGroup = "file";
        private const string LineNumberGroup = "line";
        private const string AtToken = "   at ";
        private const string InToken = " in ";
        private const string ArgumentPattern = @"\(.*\)";
        private static readonly string QualifiedNamePattern = $@"(?<{MethodGroup}>[a-zA-Z_<>][a-zA-Z0-9_<>,`\[\]\+\.]*)";
        private static readonly string PathLineNumberPattern = $@"(?<{FileNameGroup}>.*):line (?<{LineNumberGroup}>[0-9]*)";
        private static readonly string FrameParserPattern = $@"^{AtToken}{QualifiedNamePattern}{ArgumentPattern}{InToken}{PathLineNumberPattern}$";
        private static readonly Regex s_stackFrameRegex = new Regex(FrameParserPattern);

        /// <summary>
        /// Gets the function name of the stack frame.
        /// </summary>
        public string Function { get; private set; }

        /// <summary>
        /// Gets the source file path of the stack frame.
        /// </summary>
        public string SourceFile { get; private set; }

        /// <summary>
        /// Gets the line number of the stack frame.
        /// </summary>
        public long LineNumber { get; private set; }

        /// <summary>
        /// Gets the raw data of the frame.
        /// </summary>
        public string RawData { get; }

        /// <summary>
        /// Gets the flag that indicates if the source information is well exacted.
        /// </summary>
        public bool IsWellParsed { get; private set; }

        /// <summary>
        /// The text to display for the frame.
        /// If it is well parsed, display the function name.
        /// Otherwise the raw data.
        /// </summary>
        public string SummaryText => IsWellParsed ? Function : RawData;

        /// <summary>
        /// The source file path and line number that is displayed to user.
        /// </summary>
        public string SourceLinkCaption => IsWellParsed ? $"{SourceFile}:{LineNumber}" : null;

        public StackFrame(string raw)
        {
            if (raw == null)
            {
                throw new ErrorReportingException(new ArgumentNullException(nameof(raw)));
            }
            RawData = raw;
            ParseStackFrame();
        }

        /// <summary>
        /// Parse a line;
        /// </summary>
        private void ParseStackFrame()
        {
            var match = s_stackFrameRegex.Match(RawData);
            if (match.Success)
            {
                Function = match.Groups[MethodGroup].Value;
                SourceFile = match.Groups[FileNameGroup]?.Value;
                long tmp;
                if (long.TryParse(match.Groups[LineNumberGroup]?.Value, out tmp))
                {
                    LineNumber = tmp;
                    IsWellParsed = true;
                }                    
            }
        }
    }
}
