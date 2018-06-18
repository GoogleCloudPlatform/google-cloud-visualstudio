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
using System;

namespace GoogleCloudExtensionUnitTests.Deployment
{
    [TestClass]
    public class MSBuildTargetTests
    {
        [TestMethod]
        public void TestConstructor_SetsTarget()
        {
            const string expectedTarget = "ExpectedTarget";
            var objectUnderTest = new MSBuildTarget(expectedTarget);

            Assert.AreEqual(expectedTarget, objectUnderTest.Target);
        }

        [TestMethod]
        public void TestToString_ReturnsMSBuildCliArgument()
        {
            var objectUnderTest = new MSBuildTarget("ExpectedTarget");

            Assert.AreEqual("/t:ExpectedTarget", objectUnderTest.ToString());
        }

        [TestMethod]
        public void TestConstructor_ThrowsForNullTarget()
        {
            var e = Assert.ThrowsException<ArgumentNullException>(() => new MSBuildTarget(null));

            Assert.AreEqual("target", e.ParamName);
        }
    }
}
