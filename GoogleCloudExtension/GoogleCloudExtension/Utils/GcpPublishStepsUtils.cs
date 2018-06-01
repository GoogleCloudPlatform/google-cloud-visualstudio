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
        private static readonly Regex s_defaultVersionFormatRegex = new Regex("^[0-9]{8}t[0-9]{6}");

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
            if (!int.TryParse(value, out int intValue) || intValue <= 0)
            {
                yield return StringValidationResult.FromResource(
                    nameof(Resources.ValidationPositiveNonZeroMessage), fieldName);
            }
        }

        /// <summary>
        /// Increments or updates a version string.
        /// </summary>
        /// <remarks>
        /// If the input is null or whitespace, returns the default version.
        /// If the version string matches a default version, the new version is a default version with the new time.
        /// If the version string contains exactly one integer, increment the integer and return the new string.
        /// If the version string ends with an integer, increment the integer and return the new string.
        /// Otherwise, append "2" to the string and return.
        /// </remarks>
        /// <param name="version">The version to increment.</param>
        /// <returns>The incremented version.</returns>
        public static string IncrementVersion(string version)
        {
            var singleIntegerRegex = new Regex("^[^0-9]*[0-9]+[^0-9]*$");
            var captureSingleIntRegex = new Regex("[0-9]+");
            var captureTraingIntRegex = new Regex("[0-9]+$");
            if (string.IsNullOrWhiteSpace(version) || IsDefaultVersion(version))
            {
                return GetDefaultVersion();
            }
            else if (singleIntegerRegex.IsMatch(version) &&
                int.TryParse(captureSingleIntRegex.Match(version).Value, out int capturedInt))
            {
                capturedInt++;
                return captureSingleIntRegex.Replace(version, capturedInt.ToString());
            }
            else if (captureTraingIntRegex.IsMatch(version) && int.TryParse(
              captureTraingIntRegex.Match(version).Value, out capturedInt))
            {
                capturedInt++;
                return captureTraingIntRegex.Replace(version, capturedInt.ToString());
            }
            else
            {
                return version + "2";
            }
        }

        /// <summary>
        /// Returns true if the given version string matches the default version regex.
        /// </summary>
        /// <param name="version">The version string to test.</param>
        public static bool IsDefaultVersion(string version) =>
            version != null && s_defaultVersionFormatRegex.IsMatch(version);
    }
}
