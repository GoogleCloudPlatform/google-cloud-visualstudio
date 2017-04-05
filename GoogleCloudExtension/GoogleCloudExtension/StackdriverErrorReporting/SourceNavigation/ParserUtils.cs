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

using System;
using System.Linq;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Utility methods for helping parsing exception message.
    /// </summary>
    internal class ParserUtils
    {
        private const string InnderExceptionSeparator = "--->";
        /// <summary>
        /// "\r\n" (\u000D\u000A) for Windows
        /// "\n" (\u000A) for Unix
        /// "\r" (\u000D) for some caess.
        /// </summary>
        private static readonly string[] s_lineBreaks = new string[] { "\r\n", "\n", "\r" };
        private static readonly string[] s_lineBreaksAndInnerErrorSeparator = new string[] { InnderExceptionSeparator, "\r\n", "\n", "\r" };

        /// <summary>
        /// Extract the head summary from exception string.
        /// Example:
        ///     System.ArgumentException: this is a test error \r\n at void testmethod() ....  
        ///     This returns   "System.ArgumentException: this is a test error "  
        /// 
        ///     System.ArgumentException: this is another exception ---> System.ArgumentException with inner exception \r\n ....
        ///     This returns    "System.ArgumentException: this is another exception"
        /// </summary>
        public static string ExtractHeader(string exceptionMessage) =>
            exceptionMessage?.Split(s_lineBreaks, 2, StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault();

        /// <summary>
        /// Split exception lines into string array using one of the \r\n,  \r, \n line break symbols.
        /// </summary>
        public static string[] SplitLines(string exceptionMessage) =>
            exceptionMessage.Split(s_lineBreaks, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// If header contains "--->" token, return the part before this token.
        /// </summary>
        public static string SeparateHeaderFromInnerError(string header)
        {
            int? index = header?.IndexOf(InnderExceptionSeparator);
            return index > 0 ? header.Substring(0, index.Value) : header;
        }
    }
}
