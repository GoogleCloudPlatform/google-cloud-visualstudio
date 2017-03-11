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
using System;
using System.Collections.Concurrent;
using System.ComponentModel.Composition;

namespace GoogleCloudExtension.SourceBrowsing
{
    /// <summary>
    /// Define a custom <seealso cref="IViewTaggerProvider"/>.
    /// </summary>
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(TextMarkerTag))]
    [ContentType("CSharp")]
    internal class LoggerTaggerProvider : IViewTaggerProvider
    {
        private static Lazy<ConcurrentDictionary<ITextView, StackdriverTagger>> _taggers = new Lazy<ConcurrentDictionary<ITextView, StackdriverTagger>>();

        /// <summary>
        /// Gets text view to taggers map.
        /// The data structure is used by CreateTagger that is exposed to Visual Studio. 
        /// VS may call it at any thread.
        /// Using concurrent dictionary so as to syncronize access to the map from different threads.
        /// </summary>
        public static ConcurrentDictionary<ITextView, StackdriverTagger> AllLoggerTaggers => _taggers.Value;

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

            var tagger = AllLoggerTaggers.GetOrAdd(textView, (x) => new StackdriverTagger(x, buffer, ToolTipProviderFactory));
            return tagger as ITagger<T>;
        }
    }
}
