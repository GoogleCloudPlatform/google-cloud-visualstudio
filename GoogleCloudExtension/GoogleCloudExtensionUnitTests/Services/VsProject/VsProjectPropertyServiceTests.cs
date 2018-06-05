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
using GoogleCloudExtension.Services.VsProject;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Runtime.InteropServices;

namespace GoogleCloudExtensionUnitTests.Services.VsProject
{
    [TestClass]
    public class VsProjectPropertyServiceTests
    {

        private Mock<IVsSolution> _vsSolutionMock;
        private Mock<IVsBuildPropertyStorage> _buildStoreMock;
        private Project _mockedProject;
        private IVsHierarchy _mockedVsProject;
        private VsProjectPropertyService _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _mockedProject = Mock.Of<Project>(p => p.UniqueName == "DefaultProjectName");

            var vsProjectMock = new Mock<IVsHierarchy>();
            _buildStoreMock = vsProjectMock.As<IVsBuildPropertyStorage>();
            _mockedVsProject = vsProjectMock.Object;
            _vsSolutionMock = new Mock<SVsSolution>().As<IVsSolution>();
            _vsSolutionMock.Setup(s => s.GetProjectOfUniqueName(It.IsAny<string>(), out _mockedVsProject))
                .Returns(VSConstants.S_OK);
            var mockedServiceProvider =
                Mock.Of<SVsServiceProvider>(sp => sp.GetService(typeof(SVsSolution)) == _vsSolutionMock.Object);
            _objectUnderTest = new VsProjectPropertyService(new Lazy<SVsServiceProvider>(() => mockedServiceProvider));
        }

        [TestMethod]
        public void TestGetUserProperty_ThrowsOnSolutionFailure()
        {
            const string propertyName = "PropertyName";
            _vsSolutionMock.Setup(s => s.GetProjectOfUniqueName(It.IsAny<string>(), out _mockedVsProject))
                .Returns(VSConstants.E_FAIL);

            Assert.ThrowsException<COMException>(() => _objectUnderTest.GetUserProperty(_mockedProject, propertyName));
        }

        [TestMethod]
        public void TestGetUserProperty_ThrowsOnBuildPropertyStorageFail()
        {
            string value;
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.GetPropertyValue(
                        propertyName, null, VsProjectPropertyService.UserFileFlag, out value))
                .Returns(VSConstants.E_FAIL);

            Assert.ThrowsException<COMException>(() => _objectUnderTest.GetUserProperty(_mockedProject, propertyName));
        }

        [TestMethod]
        public void TestGetUserProperty_ReturnsProperty()
        {
            var expectedValue = "PropertyValue";
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.GetPropertyValue(
                        propertyName, null, VsProjectPropertyService.UserFileFlag, out expectedValue))
                .Returns(VSConstants.S_OK);

            string result = _objectUnderTest.GetUserProperty(_mockedProject, propertyName);

            Assert.AreEqual(expectedValue, result);
        }

        [TestMethod]
        public void TestGetUserProperty_ReturnsNullForMissingPropertyHr()
        {
            string expectedValue = null;
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.GetPropertyValue(
                        propertyName, null, VsProjectPropertyService.UserFileFlag, out expectedValue))
                .Returns(VsProjectPropertyService.HrPropertyNotFound);

            string result = _objectUnderTest.GetUserProperty(_mockedProject, propertyName);

            Assert.AreEqual(expectedValue, result);
        }

        [TestMethod]
        public void TestSaveUserProperty_ThrowsOnSolutionFailure()
        {
            const string value = "PropertyValue";
            const string propertyName = "PropertyName";
            _vsSolutionMock.Setup(s => s.GetProjectOfUniqueName(It.IsAny<string>(), out _mockedVsProject))
                .Returns(VSConstants.E_FAIL);

            Assert.ThrowsException<COMException>(() => _objectUnderTest.SaveUserProperty(_mockedProject, propertyName, value));
        }

        [TestMethod]
        public void TestSaveUserProperty_ThrowsOnBuildPropertyStorageFail()
        {
            var value = "PropertyValue";
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.SetPropertyValue(propertyName, null, VsProjectPropertyService.UserFileFlag, value))
                .Returns(VSConstants.E_FAIL);

            Assert.ThrowsException<COMException>(() => _objectUnderTest.SaveUserProperty(_mockedProject, propertyName, value));
        }

        [TestMethod]
        public void TestSaveUserProperty_SavesProperty()
        {
            const string value = "PropertyValue";
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.SetPropertyValue(propertyName, null, VsProjectPropertyService.UserFileFlag, value))
                .Returns(VSConstants.S_OK).Verifiable();

            _objectUnderTest.SaveUserProperty(_mockedProject, propertyName, value);

            _buildStoreMock.Verify();
        }

        [TestMethod]
        public void TestSaveUserProperty_GivenNullDeletesProperty()
        {
            const string value = null;
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.RemoveProperty(propertyName, null, VsProjectPropertyService.UserFileFlag))
                .Returns(VSConstants.S_OK).Verifiable();

            _objectUnderTest.SaveUserProperty(_mockedProject, propertyName, value);

            _buildStoreMock.Verify();
        }

        [TestMethod]
        public void TestDeleteUserProperty_ThrowsOnSolutionFailure()
        {
            const string propertyName = "PropertyName";
            // ReSharper disable once RedundantAssignment
            IVsHierarchy vsProject = _mockedVsProject;
            _vsSolutionMock.Setup(s => s.GetProjectOfUniqueName(It.IsAny<string>(), out vsProject))
                .Returns(VSConstants.E_FAIL);

            Assert.ThrowsException<COMException>(() => _objectUnderTest.DeleteUserProperty(_mockedProject, propertyName));
        }

        [TestMethod]
        public void TestDeleteUserProperty_ThrowsOnBuildPropertyStorageFail()
        {
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.RemoveProperty(propertyName, null, VsProjectPropertyService.UserFileFlag))
                .Returns(VSConstants.E_FAIL);

            Assert.ThrowsException<COMException>(() => _objectUnderTest.DeleteUserProperty(_mockedProject, propertyName));
        }

        [TestMethod]
        public void TestDeleteUserProperty_DeletesProperty()
        {
            const string propertyName = "PropertyName";
            _buildStoreMock
                .Setup(
                    s => s.RemoveProperty(propertyName, null, VsProjectPropertyService.UserFileFlag))
                .Returns(VSConstants.S_OK).Verifiable();

            _objectUnderTest.DeleteUserProperty(_mockedProject, propertyName);

            _buildStoreMock.Verify();
        }
    }
}
