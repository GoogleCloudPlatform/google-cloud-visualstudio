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

using GoogleCloudExtension;
using GoogleCloudExtension.Utils.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace GoogleCloudExtensionUnitTests.Utils.Validation
{
    [TestClass]
    public class StringValidationResultTests
    {
        [TestMethod]
        public void ValidateExceptionOnMissingResource()
        {
            const string resourceName = "not exists";
            try
            {
                StringValidationResult.FromResource(resourceName);
                Assert.Fail();
            }
            catch (ArgumentException e)
            {
                Assert.IsInstanceOfType(e, typeof(ArgumentException));

                string expectedMessage =
                    String.Format(Resources.Culture, Resources.ExceptionResourceNotFoundMessage, resourceName);
                Assert.IsTrue(e.Message.StartsWith(expectedMessage));

                MethodInfo targetMethod =
                    typeof(StringValidationResult).GetMethod(
                        nameof(StringValidationResult.FromResource), new[] { typeof(string), typeof(object[]) });
                string expectedParameterName =
                    targetMethod.GetParameters().Single(pi => pi.ParameterType == typeof(string)).Name;
                Assert.AreEqual(expectedParameterName, e.ParamName);
            }
        }

        [TestMethod]
        public void Success()
        {
            const string fieldName = "Field name";
            string expectedMessage = String.Format(
                Resources.Culture, Resources.ValidationMaxCharactersMessage, fieldName, 10);
            var result = StringValidationResult.FromResource(
                nameof(Resources.ValidationMaxCharactersMessage), fieldName, 10);

            Assert.AreEqual(expectedMessage, result.Message);
            Assert.AreEqual(expectedMessage, result.ErrorContent);
            Assert.AreEqual(expectedMessage, result.ToString());
            Assert.IsFalse(result.IsValid);
        }
    }
}
