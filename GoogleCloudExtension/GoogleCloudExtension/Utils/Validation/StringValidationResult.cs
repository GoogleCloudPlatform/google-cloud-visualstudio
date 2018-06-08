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
using System.Globalization;
using System.Windows.Controls;

namespace GoogleCloudExtension.Utils.Validation
{
    /// <summary>
    /// A simple validation result with a string message as the error content.
    /// </summary>
    public class StringValidationResult : ValidationResult
    {
        /// <summary>
        /// The error content of the validation result as a string.
        /// </summary>
        public string Message => ErrorContent.ToString();

        /// <summary>
        /// Factory method for making a string result using the name of a resource.
        /// </summary>
        /// <param name="resourceName">The name of the resource to build the message from</param>
        /// <param name="formatParams">The string format parameters used to build the string.</param>
        /// <returns>The new string validation result.</returns>
        public static StringValidationResult FromResource(string resourceName, params object[] formatParams)
        {
            CultureInfo culture = Resources.Culture;
            string resource = Resources.ResourceManager.GetString(resourceName, culture);
            if (resource == null)
            {
                throw new ArgumentException(
                    string.Format(Resources.Culture, Resources.ExceptionResourceNotFoundMessage, resourceName),
                    nameof(resourceName));
            }
            return new StringValidationResult(string.Format(culture, resource, formatParams));
        }

        /// <summary>
        /// Creates a validation result with IsValid set to false and Message set to errorContent.
        /// </summary>
        /// <param name="errorContent">The Message and error content of the validation result.</param>
        private StringValidationResult(string errorContent) : base(false, errorContent) { }

        /// <summary>
        /// Returns the message.
        /// </summary>
        /// <returns>The Message.</returns>
        public override string ToString() => Message;

        /// <summary>Compares the specified instance and the current instance of <see cref="StringValidationResult" /> for value equality.</summary>
        /// <returns>true if <paramref name="obj" /> and this instance of <see cref="StringValidationResult" />.have the same values.</returns>
        /// <param name="obj">The <see cref="StringValidationResult" /> instance to compare.</param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
            {
                return true;
            }
            else if (obj is StringValidationResult stringResult)
            {
                return Equals(stringResult.ErrorContent, ErrorContent) && Equals(stringResult.IsValid, IsValid);
            }
            else
            {
                return false;
            }
        }

        /// <summary>Returns the hash code for this <see cref="StringValidationResult" />.</summary>
        /// <returns>The hash code for this <see cref="StringValidationResult" />.</returns>
        public override int GetHashCode() => IsValid.GetHashCode() * 31 + ErrorContent.GetHashCode();
    }
}
