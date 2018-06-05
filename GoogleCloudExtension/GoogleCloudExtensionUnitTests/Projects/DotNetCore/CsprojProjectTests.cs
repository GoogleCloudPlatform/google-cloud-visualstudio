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
        public void TestConstructor_SetsProject()
        {
            var mockedProject = Mock.Of<Project>();
            var objectUnderTest = new CsprojProject(mockedProject, "");

            Assert.AreEqual(mockedProject, objectUnderTest.Project);
        }

        [TestMethod]
        [DataRow(CsprojProject.NetCoreApp1_0, KnownProjectTypes.NetCoreWebApplication1_0)]
        [DataRow(CsprojProject.NetCoreApp1_1, KnownProjectTypes.NetCoreWebApplication1_1)]
        [DataRow(CsprojProject.NetCoreApp2_0, KnownProjectTypes.NetCoreWebApplication2_0)]
        [DataRow("", KnownProjectTypes.None)]
        public void TestProjectType_IsExpected(string targetFramework, KnownProjectTypes expectedProjectType)
        {
            var objectUnderTest = new CsprojProject(Mock.Of<Project>(), targetFramework);

            Assert.AreEqual(expectedProjectType, objectUnderTest.ProjectType);
        }

        [TestMethod]
        public void TestName_ComesFromProject()
        {
            const string testProjectName = "TestProjectName";
            var mockedProject = Mock.Of<Project>(p => p.Name == testProjectName);
            var objectUnderTest = new CsprojProject(mockedProject, "");

            Assert.AreEqual(testProjectName, objectUnderTest.Name);
        }

        [TestMethod]
        public void TestFullPath_ComesFromProject()
        {
            const string testProjectName = @"c:\Full\Project\Name";
            var mockedProject = Mock.Of<Project>(p => p.FullName == testProjectName);
            var objectUnderTest = new CsprojProject(mockedProject, "");

            Assert.AreEqual(testProjectName, objectUnderTest.FullPath);
        }

        [TestMethod]
        public void TestDirectoryPath_ComesFromProject()
        {
            var mockedProject = Mock.Of<Project>(p => p.FullName == @"c:\Full\Project\Path");
            var objectUnderTest = new CsprojProject(mockedProject, "");

            Assert.AreEqual(@"c:\Full\Project", objectUnderTest.DirectoryPath);
        }
    }
}
