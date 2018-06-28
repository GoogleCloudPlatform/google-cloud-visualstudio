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

using GoogleCloudExtension.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GoogleCloudExtensionUnitTests.Services
{
    [TestClass]
    public class EnvironmentServiceTests
    {
        private EnvironmentService _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new EnvironmentService();
        }

        [TestMethod]
        public void TestExpandEnvironmentVariables()
        {
            const string expectedEnvVarValue = "ExpectedEnvVarValue";
            const string targetEnvVarName = "TargetEnvVarName";
            Environment.SetEnvironmentVariable(targetEnvVarName, expectedEnvVarValue);

            string result = _objectUnderTest.ExpandEnvironmentVariables($"%{targetEnvVarName}%");

            Assert.AreEqual(expectedEnvVarValue, result);
        }

        [TestMethod]
        public void TestGetFolderPath()
        {
            string expectedResult = Environment.GetEnvironmentVariable("ProgramFiles");

            string result = _objectUnderTest.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            Assert.AreEqual(expectedResult, result);
        }
    }
}
