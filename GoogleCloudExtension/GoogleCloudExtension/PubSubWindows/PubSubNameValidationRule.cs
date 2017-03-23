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
        /// From the pub sub api documentation,
        /// both subscription names and topic names must follow these rules:
        /// It must start with a letter.
        /// It must contain only letters (`[A-Za-z]`), numbers (`[0-9]`), dashes (`-`), underscores (`_`),
        /// periods (`.`), tildes (`~`), plus (`+`) or percent signs (`%`).
        /// It must be between 3 and 255 characters in length.
        /// It must not start with `"goog"`.
        /// </summary>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string name = value?.ToString();
            if (name == null || name.Length < 3)
            {
                return new ValidationResult(false, Resources.ValidationThreeCharactersMessage);
            }
            if (name.Length > 255)
            {
                return new ValidationResult(false, Resources.Validation255CharactersMessage);
            }
            if (!char.IsLetter(name.First()))
            {
                return new ValidationResult(false, Resources.ValidationStartWithLetterMessage);
            }
            if (Regex.IsMatch(name, "[^A-Za-z0-9_\\.~+%\\-]"))
            {
                return new ValidationResult(false, Resources.ValidationPubSubNameCharacterClassMessage);
            }
            if (name.StartsWith("goog"))
            {
                return new ValidationResult(false, Resources.ValidationDisallowStartGoogMessage);
            }
            return ValidationResult.ValidResult;
        }
    }
}
