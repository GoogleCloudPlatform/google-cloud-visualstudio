// Copyright 2016 Google Inc. All Rights Reserved.
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
using GoogleCloudExtension.SolutionUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.IO;

namespace GoogleCloudExtensionUnitTests.SolutionUtils
{
    [TestClass]
    public class ProjectHelperUnitTest
    {
        private const string WebApplicationProjectKind = "{349C5851-65DF-11DA-9384-00065B846F21}";

        private static readonly string ProjectFullName = Path.Combine("Projects", "Solution1", "Project1", "project1.csproj");
        private static readonly string ProjectRoot = Path.Combine("Projects", "Solution1", "Project1");

        private const string UniqueNameNoSeparator = @"project1.csproj";
        private static readonly string UniqueNameSeparator = Path.DirectorySeparatorChar + @"project1.csproj";
        private const string UniqueNameDifferent = @"project2.csproj";


        private const string AssemblyVersionProperty = "AssemblyVersion";
        private const string AssemblyNameProperty = "AssemblyName";
        private const string AssemblyVersionValue = "AssemblyVersionValue";
        private const string AssemblyNameValue = "AssemblyNameValue";

        private ProjectHelper _objectUnderTest;
        private Mock<Project> _projectMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _projectMock = new Mock<Project>();
            _projectMock.Setup(p => p.Kind).Returns(WebApplicationProjectKind);
            _projectMock.Setup(p => p.FullName).Returns(ProjectFullName);
        }

        private void InitAssemblyProperties()
        {
            Mock<Property> assemblyVersionPropMock = new Mock<Property>();
            assemblyVersionPropMock.Setup(v => v.Name).Returns(AssemblyVersionProperty);
            assemblyVersionPropMock.Setup(v => v.Value).Returns(AssemblyVersionValue);

            Mock<Property> assemblyNamePropMock = new Mock<Property>();
            assemblyNamePropMock.Setup(v => v.Name).Returns(AssemblyNameProperty);
            assemblyNamePropMock.Setup(v => v.Value).Returns(AssemblyNameValue);

            Mock<Properties> properties = new Mock<Properties>();
            properties.Setup(ps => ps.Count).Returns(2);
            properties.Setup(ps => ps.GetEnumerator()).Returns(new Property[]
            {
                assemblyVersionPropMock.Object,
                assemblyNamePropMock.Object
            }.GetEnumerator());

            _projectMock.Setup(p => p.Properties).Returns(properties.Object);
        }

        private void InitEmptyProperties()
        {
            Mock<Properties> properties = new Mock<Properties>();
            properties.Setup(ps => ps.Count).Returns(0);
            properties.Setup(ps => ps.GetEnumerator()).Returns(new Property[] { }.GetEnumerator());

            _projectMock.Setup(p => p.Properties).Returns(properties.Object);
        }

        [TestMethod]
        public void TestInitialStateUniqueNameNoSeparator()
        {
            InitAssemblyProperties();
            _projectMock.Setup(p => p.UniqueName).Returns(UniqueNameNoSeparator);
            _objectUnderTest = new ProjectHelper(_projectMock.Object);

            TestStandardInitialState();
            TestAssemblyPropertiesInitialized();
            Assert.AreEqual(UniqueNameNoSeparator, _objectUnderTest.UniqueName, true);
        }

        [TestMethod]
        public void TestInitialStateUniqueNameSeparator()
        {
            InitAssemblyProperties();
            _projectMock.Setup(p => p.UniqueName).Returns(UniqueNameSeparator);
            _objectUnderTest = new ProjectHelper(_projectMock.Object);

            TestStandardInitialState();
            TestAssemblyPropertiesInitialized();
            Assert.AreEqual(UniqueNameSeparator, _objectUnderTest.UniqueName, true);
        }

        [TestMethod]
        public void TestInitialStateUniqueNameDifferent()
        {
            InitAssemblyProperties();
            _projectMock.Setup(p => p.UniqueName).Returns(UniqueNameDifferent);
            _objectUnderTest = new ProjectHelper(_projectMock.Object);

            TestStandardInitialState();
            TestAssemblyPropertiesInitialized();
            Assert.AreEqual(UniqueNameDifferent, _objectUnderTest.UniqueName, true);
        }

        public void TestInitialStateEmptyProperties()
        {
            InitEmptyProperties();
            _projectMock.Setup(p => p.UniqueName).Returns(UniqueNameNoSeparator);
            _objectUnderTest = new ProjectHelper(_projectMock.Object);

            TestStandardInitialState();
            TestAssemblyPropertiesNull();
            Assert.AreEqual(UniqueNameDifferent, _objectUnderTest.UniqueName, true);
        }

        private void TestStandardInitialState()
        {
            Assert.AreEqual(ProjectFullName, _objectUnderTest.FullName, true);
            Assert.AreEqual(ProjectRoot, _objectUnderTest.ProjectRoot, true);
        }

        private void TestAssemblyPropertiesInitialized()
        {
            Assert.AreEqual(AssemblyVersionValue, _objectUnderTest.Version, true);
            Assert.AreEqual(AssemblyNameValue, _objectUnderTest.AssemblyName, true);
        }

        private void TestAssemblyPropertiesNull()
        {
            Assert.IsNull(_objectUnderTest.Version);
            Assert.IsNull(_objectUnderTest.AssemblyName);
        }
    }
}
