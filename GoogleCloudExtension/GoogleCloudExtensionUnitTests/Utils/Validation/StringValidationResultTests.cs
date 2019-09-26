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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using GoogleCloudExtension;
using GoogleCloudExtension.Utils.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.Utils.Validation
{
    [TestClass]
    public class StringValidationResultTests
    {
        private const string FieldName = "Field name";
        private const int MaxChars = 10;

        [TestMethod]
        public void ValidateExceptionOnMissingResource()
        {
            const string resourceName = "not exists";
            string expectedMessage =
                string.Format(Resources.Culture, Resources.ExceptionResourceNotFoundMessage, resourceName);

            var e = Assert.ThrowsException<ArgumentException>(
                () => StringValidationResult.FromResource(resourceName), expectedMessage);

            MethodInfo targetMethod =
                typeof(StringValidationResult).GetMethod(
                    nameof(StringValidationResult.FromResource), new[] { typeof(string), typeof(object[]) });
            Debug.Assert(targetMethod != null);
            string expectedParameterName =
                targetMethod.GetParameters().Single(pi => pi.ParameterType == typeof(string)).Name;
            Assert.AreEqual(expectedParameterName, e.ParamName);
        }

        [TestMethod]
        public void Success()
        {
            string expectedMessage = string.Format(
                Resources.Culture, Resources.ValidationMaxCharactersMessage, FieldName, MaxChars);
            StringValidationResult result = StringValidationResult.FromResource(
                nameof(Resources.ValidationMaxCharactersMessage), FieldName, MaxChars);

            Assert.AreEqual(expectedMessage, result.Message);
            Assert.AreEqual(expectedMessage, result.ErrorContent);
            Assert.AreEqual(expectedMessage, result.ToString());
            Assert.IsFalse(result.IsValid);
        }

        public static IEnumerable<object> NotEqualObjects { get; } = new[]
        {
            new object[] {null},
            new[] {new object()},
            new object[] {ValidationResult.ValidResult},
            new object[]
            {
                StringValidationResult.FromResource(nameof(Resources.ValidationStartLetterOrNumberMessage), FieldName)
            }
        };

        [TestMethod]
        [DynamicData(nameof(NotEqualObjects))]
        public void TestEquals_False(object notEqualObject)
        {
            StringValidationResult result = StringValidationResult.FromResource(
                nameof(Resources.ValidationMaxCharactersMessage), FieldName, MaxChars);

            Assert.IsFalse(result.Equals(notEqualObject));
        }

        [TestMethod]
        public void TestEqual_True()
        {
            StringValidationResult result1 = StringValidationResult.FromResource(
                nameof(Resources.ValidationMaxCharactersMessage), FieldName, MaxChars);
            StringValidationResult result2 = StringValidationResult.FromResource(
                nameof(Resources.ValidationMaxCharactersMessage), FieldName, MaxChars);

            // ReSharper disable once EqualExpressionComparison
            Assert.IsTrue(result1.Equals(result1));
            Assert.IsTrue(result1.Equals(result2));
        }

        [TestMethod]
        public void TestHashCode()
        {
            StringValidationResult result = StringValidationResult.FromResource(
                nameof(Resources.ValidationMaxCharactersMessage), FieldName, MaxChars);
            StringValidationResult equalResult = StringValidationResult.FromResource(
                nameof(Resources.ValidationMaxCharactersMessage), FieldName, MaxChars);

            StringValidationResult notEqualResult = StringValidationResult.FromResource(
                nameof(Resources.ValidationStartLetterOrNumberMessage), FieldName);

            Assert.AreNotEqual(0, result.GetHashCode());
            Assert.AreNotEqual(ValidationResult.ValidResult.GetHashCode(), result.GetHashCode());
            Assert.AreNotEqual(notEqualResult.GetHashCode(), result.GetHashCode());

            Assert.AreEqual(result.GetHashCode(), result.GetHashCode());
            Assert.AreEqual(equalResult.GetHashCode(), result.GetHashCode());
        }
    }
}
