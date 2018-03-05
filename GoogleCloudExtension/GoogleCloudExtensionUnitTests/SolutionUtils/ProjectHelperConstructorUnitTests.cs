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
    [TestClass]
    public class ProjectHelperConstructorUnitTests
    {
        private const string WebApplicationProjectKind = "{349C5851-65DF-11DA-9384-00065B846F21}";
        private static readonly string ProjectFullName = Path.Combine("Projects", "Solution1", "Project1", "project1.csproj");

        private Mock<Project> _projectMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _projectMock = new Mock<Project>();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorProjectKindMisc() => TestConstructorByProjectKind(Constants.vsProjectKindMisc);

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorProjectKindFolder() => TestConstructorByProjectKind(Constants.vsProjectKindSolutionItems);

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorProjectKindUnmolded() => TestConstructorByProjectKind(Constants.vsProjectKindUnmodeled);

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorNoFullName() => TestConstructorByFullNamePropertiesNull(null, Mock.Of<Properties>());

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorNoProperties() => TestConstructorByFullNamePropertiesNull(ProjectFullName, null);

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), AllowDerivedTypes = true)]
        public void TestConstructorNoPropertiesNoFullName() => TestConstructorByFullNamePropertiesNull(null, null);

        private void TestConstructorByProjectKind(string projectKind)
        {
            _projectMock.Setup(p => p.FullName).Returns(ProjectFullName);
            _projectMock.Setup(p => p.Properties).Returns(Mock.Of<Properties>());
            _projectMock.Setup(p => p.Kind).Returns(projectKind);

            new ProjectHelper(_projectMock.Object);
        }

        private void TestConstructorByFullNamePropertiesNull(string fullName, Properties properties)
        {
            _projectMock.Setup(p => p.Kind).Returns(WebApplicationProjectKind);
            _projectMock.Setup(p => p.FullName).Returns(fullName);
            _projectMock.Setup(p => p.Properties).Returns(properties);

            new ProjectHelper(_projectMock.Object);
        }
    }
}
