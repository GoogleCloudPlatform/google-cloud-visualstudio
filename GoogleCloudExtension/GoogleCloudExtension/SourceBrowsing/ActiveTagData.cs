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
using System.Windows.Controls;

namespace GoogleCloudExtension.SourceBrowsing
{
    /// <summary>
    /// Define the source line tooltip data sources.
    /// This is singleton so that there is at most one tooltip shown globally at any time. 
    /// This approach simplifies the overall design for <seealso cref="StackdriverTagger"/>. 
    /// </summary>
    internal class ActiveTagData
    {
        /// <summary>
        /// The associated <seealso cref="IWpfTextView"/> interface 
        /// to the source file that generates the <seealso cref="LogData"/>.
        /// </summary>
        public IWpfTextView TextView { get; private set; }

        /// <summary>
        /// The source line number to browse to
        /// </summary>
        public long SourceLine { get; private set; } = -1;

        /// <summary>
        /// The method name to be highlighted. 
        /// The name can be the method that produces the log or the method name in stack frame.
        /// Optional, if the value is null or empty, the entire source line is highlighted.
        /// </summary>
        public string MethodName { get; private set; }

        /// <summary>
        /// The tooltip control that is displayed around the highlighted source line.
        /// </summary>
        public UserControl TooltipControl { get; private set; }

        /// <summary>
        /// Check if the source is in valid state.
        /// </summary>
        public bool IsValidSource => TooltipControl != null && TextView != null && SourceLine > 0;

        /// <summary>
        /// Add an empty private constructor to disable creation of new instances outside.
        /// </summary>
        private ActiveTagData() { }

        /// <summary>
        /// The active tag data. 
        /// If it is null, there is no active tag.  
        /// StackdriverTagger should delete all tags or tool tips.
        /// </summary>
        public static ActiveTagData Current { get; private set; }

        /// <summary>
        /// Set all data members to null.
        /// </summary>
        public static void ResetCurrent()
        {
            Current = null;
        }

        /// <summary>
        /// Set the data members in a batch.
        /// for parameter definition, <seealso cref="ActiveTagData"/> data members.
        /// </summary>
        public static void SetCurrent(IWpfTextView view, long line, UserControl control, string method = null)
        {
            Current = new ActiveTagData()
            {
                TextView = view,
                SourceLine = line,
                TooltipControl = control,
                MethodName = method
            };
        }
    }
}
