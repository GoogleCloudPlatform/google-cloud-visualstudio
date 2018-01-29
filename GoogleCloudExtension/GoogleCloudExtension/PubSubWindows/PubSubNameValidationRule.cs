// Copyright 2016 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Utils.Validation;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// A validation rule that checks that a name fits the common google naming rules.
    /// </summary>
    public static class PubSubNameValidationRule
    {
        /// <summary>
        /// From the Pub/Sub api documentation,
        /// both subscription names and topic names must follow these rules:
        /// It must start with a letter.
        /// It must contain only letters (`[A-Za-z]`), numbers (`[0-9]`), dashes (`-`), underscores (`_`),
        /// periods (`.`), tildes (`~`), plus (`+`) or percent signs (`%`).
        /// It must be between 3 and 255 characters in length.
        /// It must not start with `"goog"`.
        /// </summary>
        public static IEnumerable<StringValidationResult> Validate(object value, string fieldName)
        {
            string name = value?.ToString();
            if (name == null)
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValdiationNotEmptyMessage), fieldName);
                yield break;
            }
            if (name.Length < 3)
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValidationMinCharactersMessage), fieldName, 3);
            }
            if (name.Length > 255)
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValidationMaxCharactersMessage), fieldName, 255);
            }
            if (!char.IsLetter(name.FirstOrDefault()))
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValidationStartWithLetterMessage), fieldName);
            }
            if (Regex.IsMatch(name, "[^A-Za-z0-9_\\.~+%\\-]"))
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValidationPubSubNameCharacterClassMessage), fieldName);
            }
            if (name.StartsWith("goog"))
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValidationDisallowStartGoogMessage), fieldName);
            }
        }
    }
}
