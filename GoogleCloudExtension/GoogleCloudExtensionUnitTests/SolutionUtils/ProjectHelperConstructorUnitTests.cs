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

namespace GoogleCloudExtensionUnitTests.SolutionUtils
{
    [TestClass]
    public class ProjectHelperConstructorUnitTests
    {
        private const string WebApplicationProjectKind = "{349C5851-65DF-11DA-9384-00065B846F21}";
        private const string ProjectFullName = @"c:\Projects\Solution1\Project1\project1.csproj";

        private ProjectHelper _objectUnderTest;
        private Mock<Project> _projectMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _projectMock = new Mock<Project>();
        }

        private void InitProjectMockForKindValidityTesting()
        {
            _projectMock.Setup(p => p.FullName).Returns(ProjectFullName);
            _projectMock.Setup(p => p.Properties).Returns(Mock.Of<Properties>());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorProjectKindMisc()
        {
            InitProjectMockForKindValidityTesting();
            _projectMock.Setup(p => p.Kind).Returns(Constants.vsProjectKindMisc);

            _objectUnderTest = new ProjectHelper(_projectMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorProjectKindFolder()
        {
            InitProjectMockForKindValidityTesting();
            _projectMock.Setup(p => p.Kind).Returns(Constants.vsProjectKindSolutionItems);

            _objectUnderTest = new ProjectHelper(_projectMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorProjectKindUnmolded()
        {
            InitProjectMockForKindValidityTesting();
            _projectMock.Setup(p => p.Kind).Returns(Constants.vsProjectKindUnmodeled);

            _objectUnderTest = new ProjectHelper(_projectMock.Object);
        }

        private void InitProjectMockForExtraValidityTesting()
        {
            _projectMock.Setup(p => p.Kind).Returns(WebApplicationProjectKind);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorNoFullName()
        {
            InitProjectMockForExtraValidityTesting();
            _projectMock.Setup(p => p.FullName).Returns((string)null);
            _projectMock.Setup(p => p.Properties).Returns(Mock.Of<Properties>());

            _objectUnderTest = new ProjectHelper(_projectMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorNoProperties()
        {
            InitProjectMockForExtraValidityTesting();
            _projectMock.Setup(p => p.FullName).Returns(ProjectFullName);
            _projectMock.Setup(p => p.Properties).Returns((Properties)null);

            _objectUnderTest = new ProjectHelper(_projectMock.Object);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorNoPropertiesNoFullName()
        {
            InitProjectMockForExtraValidityTesting();
            _projectMock.Setup(p => p.FullName).Returns((string)null);
            _projectMock.Setup(p => p.Properties).Returns((Properties)null);

            _objectUnderTest = new ProjectHelper(_projectMock.Object);
        }
    }
}
