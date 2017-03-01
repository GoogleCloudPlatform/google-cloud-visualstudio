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

using Microsoft.VisualStudio.Text.Editor;
using System;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Define the logger tooltip data sources.
    /// This is singleton so that there is at most one tooltip shown globally at any time. 
    /// This approach simplifies the overall design for <seealso cref="LoggerTagger"/>. 
    /// </summary>
    internal class TooltipSource
    {
        // Note, adding lambda () => new LoggerTooltipSource() is necessary for the constructor is private.
        private static Lazy<TooltipSource> s_instance = new Lazy<TooltipSource>(() => new TooltipSource());
        /// <summary>
        /// The ErrorGroupItem object as the data context for <seealso cref="TooltipControl"/>.
        /// </summary>
        public object Error { get; private set; }

        /// <summary>
        /// The associated <seealso cref="IWpfTextView"/> interface 
        /// to the source file that generates the <seealso cref="LogData"/>.
        /// </summary>
        public IWpfTextView TextView { get; private set; }

        /// <summary>
        /// The source line number associated with the <seealso cref="LogData"/>.
        /// </summary>
        public long SourceLine { get; private set; } = -1;

        /// <summary>
        /// The method name that produces the error.
        /// </summary>
        public string MethodName { get; private set; }

        /// <summary>
        /// Check if the source is in valid state.
        /// </summary>
        public bool IsValidSource => Error != null && TextView != null && SourceLine > 0 && MethodName != null;

        /// <summary>
        /// Add an empty private constructor to disable creation of new instances outside.
        /// </summary>
        private TooltipSource() { }

        /// <summary>
        /// The singleton instance of <seealso cref="TooltipSource"/>.
        /// </summary>
        public static TooltipSource Current => s_instance.Value;

        /// <summary>
        /// Set all data members to null.
        /// </summary>
        public void Reset()
        {
            Error = null;
            TextView = null;
            SourceLine = -1;
            MethodName = null;
        }

        /// <summary>
        /// Set the data members in a batch.
        /// for parameter definition, <seealso cref="TooltipSource"/> data members.
        /// </summary>
        public void Set(object data, IWpfTextView view, long line, string method)
        {
            Error = data;
            TextView = view;
            SourceLine = line;
            MethodName = method;
        }
    }
}
