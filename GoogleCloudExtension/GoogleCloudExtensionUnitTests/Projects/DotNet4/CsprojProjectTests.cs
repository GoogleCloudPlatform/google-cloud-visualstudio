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

using EnvDTE;
using GoogleCloudExtension.Projects.DotNet4;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.Projects.DotNet4
{
    [TestClass]
    public class CsprojProjectTests
    {
        [TestMethod]
        public void TestConstructor_SetsFrameworkVersion()
        {
            var mockedProject = Mock.Of<Project>(
                p => p.Properties.Item("TargetFrameworkMoniker").Value.ToString() ==
                    ".NETFramework,Version=v4.4.4");
            var objectUnderTest = new CsprojProject(mockedProject);

            Assert.AreEqual("4.4.4", objectUnderTest.FrameworkVersion);
        }
    }
}
