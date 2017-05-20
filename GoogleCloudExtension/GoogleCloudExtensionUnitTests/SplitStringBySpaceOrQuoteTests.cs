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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace UnitTestProject
{
    [TestClass]
    public class SplitStringBySpaceOrQuoteTests
    {
        [TestMethod]
        public void General()
        {
            Verify("\"This is a search string\"  And This  ",
                new string[]
                {
                    "This is a search string",
                    "And",
                    "This"
                }
                );

            Verify("\"This is a search string  And This  ",
                new string[]
                {
                    "This",
                    "is",
                    "a",
                    "search",
                    "string",
                    "And",
                    "This"
                }
                );

            Verify("This is search    ",
                new string[]
                {
                    "This",
                    "is",
                    "search",
                }
                );

            Verify(@"This is \search    ",
                new string[]
                {
                    "This",
                    "is",
                    @"\\search",
                }
                );
        }

        [TestMethod]
        public void NullEmpty()
        {
            Verify("", null);
            Verify("   ", null);
            Verify(null, null);
            Verify("\"", null);
            Verify("   \"  ", null);
            Verify("\"  \"", new string[] { "  " });
            Verify("\"  \"   ", new string[] { "  " });
            Verify("  \"  \"   ", new string[] { "  " });
            Verify("  \"\"   ", null);
        }

        [TestMethod]
        public void SingleQuote()
        {
            Verify("'", new string[] { "'" });
            Verify("'    '", new string[] { "'", "'" });
        }

        [TestMethod]
        public void MoreQuotes()
        {
            Verify("\"string 1\"   And  \"String 2\"   more   ",
                new string[] {
                    "string 1",
                    "And",
                    "String 2",
                    "more"
                });

            Verify("\"string 1\"   \"  And  \"String 2\"   more   ",
                new string[] {
                    "string 1",
                    "  And  ",
                    "String",
                    "2",
                    "more"
                });

            Verify(" string after \"    ",
                new string[] {
                    "string",
                    "after"
                });
        }

        [TestMethod]
        public void StringEscaping()
        {
            Verify("\"a\\\"\"",
                new string[] {
                    "a\\\"",
                });
        }

        [TestMethod]
        public void StringEscapingMore()
        {
            Verify("\"string 1\\\"   And  \"String 2\"   more   ",
                new string[] {
                    "string 1\\\"   And  ",
                    "String",
                    "2",
                    "more"
                });
        }

        private static void Verify(string input, string[] expected)
        {
            var output = StringUtils.SplitStringBySpaceOrQuote(input);
            if (expected == null || expected.Count() == 0)
            {
                if (output != null && output.Count() != 0)
                {
                    Assert.Fail($"Fail on {input}");
                }
                else
                {
                    return;
                }
            }

            if (output.Any(x => !expected.Contains(x)) ||
                expected.Any(x => !output.Contains(x)))
            {
                Assert.Fail($"Fail on {input}");
            }
        }
    }
}
