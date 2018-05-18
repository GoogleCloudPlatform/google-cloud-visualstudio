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

using GoogleCloudExtension.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace GoogleCloudExtension.Utils
{
    /// <summary>
    /// Common utils for publishing steps to GCP.
    /// </summary>
    public static class GcpPublishStepsUtils
    {
        // Static properties for unit testing.
        internal static DateTime? NowOverride { private get; set; }
        private static DateTime Now => NowOverride ?? DateTime.Now;
        /// <summary>
        /// Returns a default version name suitable for publishing to GKE and Flex.
        /// </summary>
        /// <returns>The default name string.</returns>
        public static string GetDefaultVersion()
        {
            DateTime now = Now;
            return $"{now.Year:0000}{now.Month:00}{now.Day:00}t{now.Hour:00}{now.Minute:00}{now.Second:00}";
        }

        /// <summary>
        /// Determines if the given name is a valid name.
        /// </summary>
        /// <param name="name">The name to check.</param>
        /// <param name="fieldName">The name of the field being validated.</param>
        /// <returns>The results of the validation.</returns>

        public static IEnumerable<ValidationResult> ValidateName(string name, string fieldName)
        {
            if (string.IsNullOrEmpty(name))
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValdiationNotEmptyMessage), fieldName);
                yield break;
            }
            if (!Regex.IsMatch(name, @"^[a-z\d]"))
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValidationStartLetterOrNumberMessage), fieldName);
            }
            if (Regex.IsMatch(name, @"[^a-z\d\-]"))
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValidationAllLetterNumberOrDashMessage), fieldName);
            }

            if (name.Length > 100)
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValidationMaxCharactersMessage), fieldName, 100);
            }
        }

        /// <summary>
        /// Converts a possibly invalid name to one that will pass name validation.
        /// </summary>
        /// <param name="name">The name to converto to valid.</param>
        /// <returns>A valid name built from the starting name.</returns>
        public static string ToValidName(string name)
        {
            if (name == null)
            {
                return null;
            }
            string kebobedName = StringUtils.ToKebobCase(name);
            string beginsWithLetterOrNumber = Regex.Replace(kebobedName, @"^[^a-z\d]+", "");
            string invalidCharactersReplaced = Regex.Replace(beginsWithLetterOrNumber, @"[^a-z\d\-]", "-");
            return invalidCharactersReplaced.Length > 100 ?
                invalidCharactersReplaced.Substring(0, 100) :
                invalidCharactersReplaced;
        }

        public static IEnumerable<ValidationResult> ValidatePositiveNonZeroInteger(string value, string fieldName)
        {
            int intValue;
            if (!int.TryParse(value, out intValue))
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValidationPositiveNonZeroMessage), fieldName);
            }
            else if (intValue <= 0)
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValidationPositiveNonZeroMessage), fieldName);
            }
        }
    }
}
