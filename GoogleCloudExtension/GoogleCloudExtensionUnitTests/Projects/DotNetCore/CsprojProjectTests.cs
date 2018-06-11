﻿// Copyright 2018 Google Inc. All Rights Reserved.
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

using EnvDTE;
using GoogleCloudExtension.Deployment;
using GoogleCloudExtension.Projects.DotNetCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.Projects.DotNetCore
{
    [TestClass]
    public class CsprojProjectTests
    {
        [TestMethod]
        public void TestConstructor_SetsFrameworkVersion()
        {
            var objectUnderTest = new CsprojProject(Mock.Of<Project>(), "netcoreapp1.7");

            Assert.AreEqual("1.7", objectUnderTest.FrameworkVersion);
        }

        [TestMethod]
        public void TestProjectType_IsNetCore()
        {
            var objectUnderTest = new CsprojProject(Mock.Of<Project>(), "netcoreapp1.7");

            Assert.AreEqual(KnownProjectTypes.NetCoreWebApplication, objectUnderTest.ProjectType);
        }
    }
}
