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

using GoogleCloudExtension.Projects;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GoogleCloudExtensionUnitTests.Projects
{
    [TestClass]
    public class ParsedDteProjectExtensionsTests : ExtensionTestBase
    {
        private Mock<IVsSolution> _vsSolutionMock;
        private Mock<IVsBuildPropertyStorage> _buildStoreMock;
        private IParsedDteProject _mockedParsedProject;
        private IVsHierarchy _mockedVsProject;

        [TestInitialize]
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public new void BeforeEach()
        {
            _mockedParsedProject = Mock.Of<IParsedDteProject>(p => p.Project.UniqueName == "DefaultProjectName");

            var vsProjectMock = new Mock<IVsHierarchy>();
            _buildStoreMock = vsProjectMock.As<IVsBuildPropertyStorage>();
            _mockedVsProject = vsProjectMock.Object;
            _vsSolutionMock = new Mock<IVsSolution>();
            _vsSolutionMock.Setup(s => s.GetProjectOfUniqueName(It.IsAny<string>(), out _mockedVsProject))
                .Returns(VSConstants.S_OK);
            PackageMock.Setup(p => p.GetService<IVsSolution>()).Returns(_vsSolutionMock.Object);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public void TestGetUserProperty_ThrowsOnSolutionFailure()
        {
            const string propertyName = "PropertyName";
            IVsHierarchy vsProject = _mockedVsProject;
            _vsSolutionMock.Setup(s => s.GetProjectOfUniqueName(It.IsAny<string>(), out vsProject))
                .Returns(VSConstants.E_FAIL);

            Assert.ThrowsException<COMException>(() => _mockedParsedProject.GetUserProperty(propertyName));
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public void TestGetUserProperty_ThrowsOnBuildPropertyStorageFail()
        {
            var value = "PropertyValue";
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.GetPropertyValue(
                        propertyName, null, ParsedDteProjectExtensions.UserFileFlag, out value))
                .Returns(VSConstants.E_FAIL);

            Assert.ThrowsException<COMException>(() => _mockedParsedProject.GetUserProperty(propertyName));
        }

        [TestMethod]
        public void TestGetUserProperty_ReturnsProperty()
        {
            var expectedValue = "PropertyValue";
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.GetPropertyValue(
                        propertyName, null, ParsedDteProjectExtensions.UserFileFlag, out expectedValue))
                .Returns(VSConstants.S_OK);

            string result = _mockedParsedProject.GetUserProperty(propertyName);

            Assert.AreEqual(expectedValue, result);
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public void TestSaveUserProperty_ThrowsOnSolutionFailure()
        {
            const string value = "PropertyValue";
            const string propertyName = "PropertyName";
            IVsHierarchy vsProject = _mockedVsProject;
            _vsSolutionMock.Setup(s => s.GetProjectOfUniqueName(It.IsAny<string>(), out vsProject))
                .Returns(VSConstants.E_FAIL);

            Assert.ThrowsException<COMException>(() => _mockedParsedProject.SaveUserProperty(propertyName, value));
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public void TestSaveUserProperty_ThrowsOnBuildPropertyStorageFail()
        {
            var value = "PropertyValue";
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.SetPropertyValue(propertyName, null, ParsedDteProjectExtensions.UserFileFlag, value))
                .Returns(VSConstants.E_FAIL);

            Assert.ThrowsException<COMException>(() => _mockedParsedProject.SaveUserProperty(propertyName, value));
        }

        [TestMethod]
        public void TestSaveUserProperty_SavesProperty()
        {
            var value = "PropertyValue";
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.SetPropertyValue(propertyName, null, ParsedDteProjectExtensions.UserFileFlag, value))
                .Returns(VSConstants.S_OK).Verifiable();

            _mockedParsedProject.SaveUserProperty(propertyName, value);

            _buildStoreMock.Verify();
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public void TestDeleteUserProperty_ThrowsOnSolutionFailure()
        {
            const string propertyName = "PropertyName";
            IVsHierarchy vsProject = _mockedVsProject;
            _vsSolutionMock.Setup(s => s.GetProjectOfUniqueName(It.IsAny<string>(), out vsProject))
                .Returns(VSConstants.E_FAIL);

            Assert.ThrowsException<COMException>(() => _mockedParsedProject.DeleteUserProperty(propertyName));
        }

        [TestMethod]
        [SuppressMessage("ReSharper", "RedundantAssignment")]
        public void TestDeleteUserProperty_ThrowsOnBuildPropertyStorageFail()
        {
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.RemoveProperty(propertyName, null, ParsedDteProjectExtensions.UserFileFlag))
                .Returns(VSConstants.E_FAIL);

            Assert.ThrowsException<COMException>(() => _mockedParsedProject.DeleteUserProperty(propertyName));
        }

        [TestMethod]
        public void TestDeleteUserProperty_DeletesProperty()
        {
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.RemoveProperty(propertyName, null, ParsedDteProjectExtensions.UserFileFlag))
                .Returns(VSConstants.S_OK).Verifiable();

            _mockedParsedProject.DeleteUserProperty(propertyName);

            _buildStoreMock.Verify();
        }
    }
}
