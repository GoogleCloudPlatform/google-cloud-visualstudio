// Copyright 2018 Google Inc. All Rights Reserved.
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

using System.Windows.Documents;
using System.Windows.Markup;

namespace GoogleCloudExtension.Utils.Wpf
{
    /// <summary>
    /// A <see cref="Span"/> that removes <see cref="Run"/> elements that are only a single space,
    /// such as those created by whitespace only text nodes between xaml inline tags.
    /// </summary>
    [ContentProperty(nameof(Inlines))]
    public class WhitespaceDiscardingSpan : Span
    {
        public override void EndInit()
        {
            Inline nextInline;
            for (Inline inline = Inlines.FirstInline; inline != null; inline = nextInline)
            {
                nextInline = inline.NextInline;
                if (inline is Run run && run.Text == " ")
                {
                    Inlines.Remove(inline);
                }
            }
            base.EndInit();
        }
    }
}
