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
using GoogleCloudExtension.Projects;
using GoogleCloudExtension.Services.VsProject;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.Projects
{
    [TestClass]
    public class ParsedDteProjectExtensionsTests : ExtensionTestBase
    {
        private const string PropertyName = "PropertyName";
        private Project _mockedProject;
        private IParsedDteProject _mockedParsedProject;
        private Mock<IVsProjectPropertyService> _propertyServiceMock;
        private const string PropertyValue = "PropertyValue";

        protected override void BeforeEach()
        {
            _propertyServiceMock = new Mock<IVsProjectPropertyService>();
            PackageMock.Setup(p => p.GetMefService<IVsProjectPropertyService>()).Returns(_propertyServiceMock.Object);

            _mockedProject = Mock.Of<Project>();
            _mockedParsedProject = Mock.Of<IParsedDteProject>(p => p.Project == _mockedProject);
        }

        [TestMethod]
        public void TestGetUserProperty_DelegatesToPropertyService()
        {
            _mockedParsedProject.GetUserProperty(PropertyName);

            _propertyServiceMock.Verify(s => s.GetUserProperty(_mockedProject, PropertyName));
        }

        [TestMethod]
        public void TestSaveUserProperty_DelegatesToPropertyService()
        {
            _mockedParsedProject.SaveUserProperty(PropertyName, PropertyValue);

            _propertyServiceMock.Verify(s => s.SaveUserProperty(_mockedProject, PropertyName, PropertyValue));
        }

        [TestMethod]
        public void TestDeleteUserProperty_DelegatesToPropertyService()
        {
            _mockedParsedProject.DeleteUserProperty(PropertyName);

            _propertyServiceMock.Verify(s => s.DeleteUserProperty(_mockedProject, PropertyName));
        }
    }
}
