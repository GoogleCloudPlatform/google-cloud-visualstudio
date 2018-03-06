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
using System;
using System.IO;

namespace GoogleCloudExtensionUnitTests.SolutionUtils
{
    /// <summary>
    /// Class defining unit tests for the <seealso cref="ProjectHelper"/> class.
    /// </summary>
    [TestClass]
    public class ProjectHelperUnitTest
    {
        private const string WebApplicationProjectKind = "{349C5851-65DF-11DA-9384-00065B846F21}";

        private const string UniqueNameNoSeparator = @"project1.csproj";
        private static readonly string UniqueNameSeparator = Path.DirectorySeparatorChar + @"project1.csproj";
        private const string UniqueNameDifferent = @"project2.csproj";

        private static readonly string ProjectRoot = Path.Combine("Projects", "Solution1", "Project1");
        private static readonly string ProjectFullName = Path.Combine(ProjectRoot, UniqueNameNoSeparator);

        private const string AssemblyVersionProperty = "AssemblyVersion";
        private const string AssemblyNameProperty = "AssemblyName";
        private const string AssemblyVersionValue = "AssemblyVersionValue";
        private const string AssemblyNameValue = "AssemblyNameValue";

        private ProjectHelper _objectUnderTest;
        private Mock<Project> _projectMock;
        private Mock<Properties> _propertiesMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _projectMock = new Mock<Project>();
            _propertiesMock = new Mock<Properties>();
        }

        /// <summary>
        /// Helper method to initialize an empty <seealso cref="Properties"/> mock.
        /// </summary>
        private void InitEmptyProperties()
        {
            _propertiesMock.Setup(ps => ps.Count).Returns(0);
            _propertiesMock.Setup(ps => ps.GetEnumerator()).Returns(new Property[] { }.GetEnumerator());
        }

        /// <summary>
        /// Helper method to initialize a <seealso cref="Properties"/> mock containing the assembly properties.
        /// </summary>
        private void InitAssemblyProperties()
        {
            Mock<Property> assemblyVersionPropMock = new Mock<Property>();
            assemblyVersionPropMock.Setup(v => v.Name).Returns(AssemblyVersionProperty);
            assemblyVersionPropMock.Setup(v => v.Value).Returns(AssemblyVersionValue);

            Mock<Property> assemblyNamePropMock = new Mock<Property>();
            assemblyNamePropMock.Setup(v => v.Name).Returns(AssemblyNameProperty);
            assemblyNamePropMock.Setup(v => v.Value).Returns(AssemblyNameValue);

            _propertiesMock.Setup(ps => ps.Count).Returns(2);
            _propertiesMock.Setup(ps => ps.GetEnumerator()).Returns(new Property[]
            {
                assemblyVersionPropMock.Object,
                assemblyNamePropMock.Object
            }.GetEnumerator());
        }

        /// <summary>
        /// Tests that the <seealso cref="ProjectHelper"/> constructor correctly throws if the project kind <seealso cref="Constants.vsProjectKindMisc"/>.
        /// Some project kinds are not supported.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorValidationProjectKindMisc() => TestConstructorEmptyProperties(Constants.vsProjectKindMisc, ProjectFullName, UniqueNameNoSeparator);

        /// <summary>
        /// Tests that the <seealso cref="ProjectHelper"/> constructor correctly throws if the project kind <seealso cref="Constants.vsProjectKindSolutionItems"/>.
        /// Some project kinds are not supported.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorValidationProjectKindFolder() => TestConstructorEmptyProperties(Constants.vsProjectKindSolutionItems, ProjectFullName, UniqueNameNoSeparator);

        /// <summary>
        /// Tests that the <seealso cref="ProjectHelper"/> constructor correctly throws if the project kind <seealso cref="Constants.vsProjectKindUnmodeled"/>.
        /// Some project kinds are not supported.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorValidationProjectKindUnmolded() => TestConstructorEmptyProperties(Constants.vsProjectKindUnmodeled, ProjectFullName, UniqueNameNoSeparator);

        /// <summary>
        /// Tests that the <seealso cref="ProjectHelper"/> constructor correctly throws if the project has no <seealso cref="Project.FullName"/>.        
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorValidationNoFullName() => TestConstructorEmptyProperties(WebApplicationProjectKind, null, UniqueNameNoSeparator);

        /// <summary>
        /// Tests that the <seealso cref="ProjectHelper"/> constructor correctly throws if the project has no <seealso cref="Project.UniqueName"/>.        
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorValidationNoUniqueName() => TestConstructorEmptyProperties(WebApplicationProjectKind, ProjectFullName, null);

        /// <summary>
        /// Tests that the <seealso cref="ProjectHelper"/> constructor correctly throws if the project has no <seealso cref="Project.Properties"/>.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorValidationNoProperties() => TestConstructorNullProperties(WebApplicationProjectKind, ProjectFullName, UniqueNameNoSeparator);

        /// <summary>
        /// Tests that the <seealso cref="ProjectHelper"/> constructor correctly throws if the project has no properties set at all.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorValidationAllNull() => TestConstructorNullProperties(null, null, null);

        /// <summary>
        /// Tests for the correct initial state of the <seealso cref="ProjectHelper"/> instance.
        /// </summary>
        [TestMethod]
        public void TestInitialStateUniqueNameNoSeparator() => TestConstructorAssemblyProperties(WebApplicationProjectKind, ProjectFullName, UniqueNameNoSeparator);

        /// <summary>
        /// Tests for the correct initial state of the <seealso cref="ProjectHelper"/> instance.
        /// </summary>
        [TestMethod]
        public void TestInitialStateUniqueNameSeparator() => TestConstructorAssemblyProperties(WebApplicationProjectKind, ProjectFullName, UniqueNameSeparator);

        /// <summary>
        /// Tests for the correct initial state of the <seealso cref="ProjectHelper"/> instance.
        /// </summary>
        [TestMethod]
        public void TestInitialStateUniqueNameDifferent() => TestConstructorAssemblyProperties(WebApplicationProjectKind, ProjectFullName, UniqueNameDifferent);

        /// <summary>
        /// Tests for the correct initial state of the <seealso cref="ProjectHelper"/> instance.
        /// </summary>
        [TestMethod]
        public void TestInitialStateEmptyPropertiesUniqueNameNoSeparator() => TestConstructorEmptyProperties(WebApplicationProjectKind, ProjectFullName, UniqueNameNoSeparator);

        /// <summary>
        /// Tests for the correct initial state of the <seealso cref="ProjectHelper"/> instance.
        /// </summary>
        [TestMethod]
        public void TestInitialStateEmptyPropertiesUniqueNameSeparator() => TestConstructorEmptyProperties(WebApplicationProjectKind, ProjectFullName, UniqueNameSeparator);

        /// <summary>
        /// Tests for the correct initial state of the <seealso cref="ProjectHelper"/> instance.
        /// </summary>
        [TestMethod]
        public void TestInitialStateEmptyPropertiesUniqueNameDifferen() => TestConstructorEmptyProperties(WebApplicationProjectKind, ProjectFullName, UniqueNameDifferent);

        private void TestConstructorNullProperties(string projectKind, string fullName, string uniqueName)
        {
            // If properties are null, constructor should throw, it should never get to the point of
            // checking whether properties are initialized or not.
            TestConstructor(projectKind, fullName, uniqueName, null, Assert.Fail);
        }

        private void TestConstructorEmptyProperties(string projectKind, string fullName, string uniqueName)
        {
            InitEmptyProperties();
            TestConstructor(projectKind, fullName, uniqueName, _propertiesMock.Object, AssertAssemblyPropertiesNull);
        }

        private void TestConstructorAssemblyProperties(string projectKind, string fullName, string uniqueName)
        {
            InitAssemblyProperties();
            TestConstructor(projectKind, fullName, uniqueName, _propertiesMock.Object, AssertAssemblyPropertiesInitialized);
        }

        /// <summary>
        /// Helper method for testing <seealso cref="ProjectHelper"/> constructor.
        /// Parameters are not those of the constructor being tested but are used to create a
        /// <seealso cref="Project"/> mock which is the one passed as parameter to the constructor
        /// under test.
        /// </summary>
        /// <param name="projectKind">String representation of a GUID representing the project kind.
        /// Some project kinds are not supported by the extension and we need to test for correct validation
        /// of this value.</param>
        /// <param name="fullName">Project full name, usually its full path.</param>
        /// <param name="uniqueName">Project unique name, usually the name of the project definition file.</param>
        /// <param name="properties">Project properties.</param>
        /// <param name="assertPropertiesInitialized">Action to use for testing the correct initialization of
        /// <seealso cref="ProjectHelper"/> based on the value of <paramref name="properties"/> </param>
        private void TestConstructor(string projectKind, string fullName, string uniqueName, Properties properties, Action assertPropertiesInitialized)
        {
            _projectMock.Setup(p => p.Kind).Returns(projectKind);
            _projectMock.Setup(p => p.FullName).Returns(fullName);
            _projectMock.Setup(p => p.UniqueName).Returns(uniqueName);
            _projectMock.Setup(p => p.Properties).Returns(properties);

            _objectUnderTest = new ProjectHelper(_projectMock.Object);

            Assert.AreEqual(fullName, _objectUnderTest.FullName, true);
            Assert.AreEqual(uniqueName, _objectUnderTest.UniqueName, true);
            Assert.AreEqual(ProjectRoot, _objectUnderTest.ProjectRoot, true);

            assertPropertiesInitialized();
        }

        private void AssertAssemblyPropertiesInitialized()
        {
            Assert.AreEqual(AssemblyVersionValue, _objectUnderTest.Version, true);
            Assert.AreEqual(AssemblyNameValue, _objectUnderTest.AssemblyName, true);
        }

        private void AssertAssemblyPropertiesNull()
        {
            Assert.IsNull(_objectUnderTest.Version);
            Assert.IsNull(_objectUnderTest.AssemblyName);
        }
    }
}
