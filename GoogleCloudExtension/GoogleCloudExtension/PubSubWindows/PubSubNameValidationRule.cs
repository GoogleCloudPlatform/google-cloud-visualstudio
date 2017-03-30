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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace GoogleCloudExtension.PubSubWindows
{
    /// <summary>
    /// A validation rule that checks that a name fits the common google naming rules.
    /// </summary>
    public class PubSubNameValidationRule : ValidationRule
    {
        /// <summary>
        /// Override for validation rule. Exists if WPF binding validation becomes usable.
        /// </summary>
        /// <param name="value">The name to validate.</param>
        /// <param name="cultureInfo">Unused.</param>
        /// <returns></returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return Validate(value).FirstOrDefault() ?? ValidationResult.ValidResult;
        }

        /// <summary>
        /// From the pub sub api documentation,
        /// both subscription names and topic names must follow these rules:
        /// It must start with a letter.
        /// It must contain only letters (`[A-Za-z]`), numbers (`[0-9]`), dashes (`-`), underscores (`_`),
        /// periods (`.`), tildes (`~`), plus (`+`) or percent signs (`%`).
        /// It must be between 3 and 255 characters in length.
        /// It must not start with `"goog"`.
        /// </summary>
        public static IEnumerable<StringValidationResult> Validate(object value)
        {
            string name = value?.ToString();
            if (name == null)
            {
                yield return new StringValidationResult(Resources.ValidationThreeCharactersMessage);
                yield break;
            }
            if (name.Length < 3)
            {
                yield return new StringValidationResult(Resources.ValidationThreeCharactersMessage);
            }
            if (name.Length > 255)
            {
                yield return new StringValidationResult(Resources.Validation255CharactersMessage);
            }
            if (!char.IsLetter(name.First()))
            {
                yield return new StringValidationResult(Resources.ValidationStartWithLetterMessage);
            }
            if (Regex.IsMatch(name, "[^A-Za-z0-9_\\.~+%\\-]"))
            {
                yield return new StringValidationResult(Resources.ValidationPubSubNameCharacterClassMessage);
            }
            if (name.StartsWith("goog"))
            {
                yield return new StringValidationResult(Resources.ValidationDisallowStartGoogMessage);
            }
        }

        public class StringValidationResult : ValidationResult
        {
            public string Message { get; }

            public StringValidationResult(string errorContent) : base(false, errorContent)
            {
                Message = errorContent;
            }
        }
    }
}
