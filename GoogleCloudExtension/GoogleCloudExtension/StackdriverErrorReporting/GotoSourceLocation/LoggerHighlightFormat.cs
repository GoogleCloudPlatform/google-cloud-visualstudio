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

using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace GoogleCloudExtension.StackdriverErrorReporting
{
    /// <summary>
    /// Define a custom <seealso cref="TextMarkerTag"/>.
    /// </summary>
    internal class LoggerTag : TextMarkerTag
    {
        public LoggerTag() : base("LoggerMarkerFormat") { }
    }

    /// <summary>
    /// Define a custom <seealso cref="EditorFormatDefinition"/> for highlighting logging methods.
    /// </summary>
    [Export(typeof(EditorFormatDefinition))]
    [Name("LoggerMarkerFormat")]
    internal class LoggerHighlightFormat : MarkerFormatDefinition
    {
        /// <summary>
        /// Initializes an instance of <seealso cref="LoggerHighlightFormat"/> class.
        /// </summary>
        public LoggerHighlightFormat()
        {
            BackgroundColor = Colors.Yellow;
            ForegroundColor = Colors.Black;
            ZOrder = 5;
        }
    }
}
