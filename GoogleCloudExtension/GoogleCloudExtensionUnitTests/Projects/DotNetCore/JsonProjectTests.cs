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
    public class JsonProjectTests
    {
        [TestMethod]
        public void TestConstructor_SetsProject()
        {
            var mockedProject = Mock.Of<Project>();
            var objectUnderTest = new JsonProject(mockedProject);

            Assert.AreEqual(mockedProject, objectUnderTest.Project);
        }

        [TestMethod]
        public void TestProjectType_IsConstant()
        {
            var objectUnderTest = new JsonProject(Mock.Of<Project>());

            Assert.AreEqual(KnownProjectTypes.NetCoreWebApplication1_0, objectUnderTest.ProjectType);
        }

        [TestMethod]
        public void TestName_ComesFromProject()
        {
            const string testProjectName = @"c:\Full\TestProject\Name";
            var mockedProject = Mock.Of<Project>(p => p.FullName == testProjectName);
            var objectUnderTest = new JsonProject(mockedProject);

            Assert.AreEqual("TestProject", objectUnderTest.Name);
        }

        [TestMethod]
        public void TestFullPath_ComesFromProject()
        {
            const string testProjectName = @"c:\Full\Project\Name";
            var mockedProject = Mock.Of<Project>(p => p.FullName == testProjectName);
            var objectUnderTest = new JsonProject(mockedProject);

            Assert.AreEqual(testProjectName, objectUnderTest.FullPath);
        }

        [TestMethod]
        public void TestDirectoryPath_ComesFromProject()
        {
            var mockedProject = Mock.Of<Project>(p => p.FullName == @"c:\Full\Project\Path");
            var objectUnderTest = new JsonProject(mockedProject);

            Assert.AreEqual(@"c:\Full\Project", objectUnderTest.DirectoryPath);
        }
    }
}
