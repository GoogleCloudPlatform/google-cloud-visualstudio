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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Some helper functions for string processing
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// Returns a list of sub strings for text searching.
        /// (1) Split by double quotes or space.
        ///     "This is a search string"  And This  
        ///      The above is split into 3 substrings:  "This is a search string", "And", "This"
        /// (2) Single \  are escaped into double \\. 
        /// (3) Only accepting \" escaping.
        /// (3) If single " is found without escaping, it is skipped.
        /// 
        /// Brief explanation of the spliting algorithm.
        ///     scannedTokens contains the list of divided sub-strings.
        ///     currentToken contains the current working(appending) sub-string.
        ///     currentToken can be either the sub-sting inside a double quotes pair,  
        ///         or a single word separated by sapce.
        ///          
        ///     If it is a double quote character,  it could be 
        ///         (a) left side of a double quotes pair. 
        ///         (b) right side of a double quotes pair.
        ///         (c) the very last alone double quote.  
        ///         (e) the escaped double quote.
        ///     If it is a single space, it could be
        ///         (a) A space inside a double quotes pair.  Add it to currentToken.
        ///         (b) else, it is a separator, add the currentToken to 
        /// </summary>
        public static IEnumerable<string> SplitStringBySpaceOrQuote(string source)
        {
            if (source == null)
            {
                return null;
            }

            StringBuilder currentToken = new StringBuilder();
            bool inDelimitedString = false;
            bool hasLeadingBackslash = false;
            List<string> scannedTokens = new List<string>();
            foreach (char chr in source)
            {
                if (hasLeadingBackslash)
                {
                    hasLeadingBackslash = false;
                    if (chr == '"')
                    {
                        currentToken.Append("\\\"");
                        continue;
                    }
                    else
                    {
                        currentToken.Append("\\\\");
                    }
                }

                switch (chr)
                {
                    case '"':
                        if (inDelimitedString)
                        {
                            if (currentToken.Length > 0)
                            {
                                scannedTokens.Add(currentToken.ToString());
                                currentToken.Clear();
                            }
                        }
                        inDelimitedString = !inDelimitedString;
                        break;

                    case ' ':
                        if (!inDelimitedString)
                        {
                            if (currentToken.Length > 0)
                            {
                                scannedTokens.Add(currentToken.ToString());
                                currentToken.Clear();
                            }
                        }
                        else
                        {
                            currentToken.Append(chr);
                        }
                        break;

                    case '\\':
                        hasLeadingBackslash = true;
                        break;

                    default:
                        currentToken.Append(chr);
                        break;
                }
            }

            if (currentToken.Length > 0)
            {
                var splits = currentToken.ToString().Split(
                    new char[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);
                scannedTokens.AddRange(splits);
                currentToken.Clear();
            }

            return scannedTokens;
        }

        /// <summary>
        /// Check if input is digits only.
        /// return false if it is null.
        /// Empty string is valid so it returns true.
        /// </summary>
        public static bool IsDigitsOnly(string text) => text == null ? false : text.All(char.IsDigit);
    }
}
