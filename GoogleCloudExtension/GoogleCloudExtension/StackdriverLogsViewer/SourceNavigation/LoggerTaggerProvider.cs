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
using Microsoft.VisualStudio.Utilities;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;

namespace GoogleCloudExtension.StackdriverLogsViewer
{
    /// <summary>
    /// Define a custom <seealso cref="IViewTaggerProvider"/>.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(TextMarkerTag))]
    [ContentType("text")]  // TODO: try CSharp ? 
    internal class LoggerTaggerProvider : IViewTaggerProvider
    {
        /// <summary>
        /// Static constructor that initializes static memebers.
        /// </summary>
        static LoggerTaggerProvider()
        {
            AllLoggerTaggers = new ConcurrentDictionary<ITextView, LoggerTagger>();
        }

        /// <summary>
        /// Keeps the text view to logger taggers map.
        /// </summary>
        public static ConcurrentDictionary<ITextView, LoggerTagger> AllLoggerTaggers;

        /// <summary>
        /// Import <seealso cref="IToolTipProviderFactory"/>.
        /// </summary>
        [Import]
        public IToolTipProviderFactory ToolTipProviderFactory { get; set; }

        /// <summary>
        /// Implement <see cref="IViewTaggerProvider"/> interface.
        /// </summary>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (textView.TextBuffer != buffer)
            {
                return null;
            }

            var tagger = AllLoggerTaggers.GetOrAdd(textView, (x) => new LoggerTagger(x, buffer, ToolTipProviderFactory));
            return tagger as ITagger<T>;
        }
    }
}
