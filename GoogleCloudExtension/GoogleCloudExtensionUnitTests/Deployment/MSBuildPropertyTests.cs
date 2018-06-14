// Copyright 2018 Google Inc. All Rights Reserved.
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

using GoogleCloudExtension.Deployment;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.Deployment
{
    [TestClass]
    public class MSBuildPropertyTests
    {
        private const string DefaultPropertyValue = "DefaultPropertyValue";
        private const string ExpectedPropertyName = "ExpectedPropertyName";
        private const string DefaultPropertyName = "DefaultPropertyName";
        private const string ExpectedPropertyValue = "Expected Property Value";

        [TestMethod]
        public void TestConstructor_SetsPropertyName()
        {
            var objectUnderTest = new MSBuildProperty(ExpectedPropertyName, DefaultPropertyValue);

            Assert.AreEqual(ExpectedPropertyName, objectUnderTest.PropertyName);
        }

        [TestMethod]
        public void TestConstructor_SetsPropertyValue()
        {
            var objectUnderTest = new MSBuildProperty(DefaultPropertyName, ExpectedPropertyValue);

            Assert.AreEqual(ExpectedPropertyValue, objectUnderTest.PropertyValue);
        }

        [TestMethod]
        public void TestToString_ReturnsMSBuildCliArg()
        {
            var objectUnderTest = new MSBuildProperty(ExpectedPropertyName, ExpectedPropertyValue);

            Assert.AreEqual("/p:ExpectedPropertyName=\"Expected Property Value\"", objectUnderTest.ToString());
        }
    }
}
