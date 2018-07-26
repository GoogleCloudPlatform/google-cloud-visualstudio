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

using GoogleCloudExtension.MenuBarControls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Runtime.InteropServices;
using RegistrationContext = Microsoft.VisualStudio.Shell.RegistrationAttribute.RegistrationContext;

namespace GoogleCloudExtensionUnitTests.MenuBarControls
{
    [TestClass]
    public class ProvideMainWindowFrameControlAttributeTests
    {
        private const int ViewId = 456;
        private const string TestTypeRegKey = @"MainWindowFrameControls\{" + TestType.Guid + "}";
        private const string TestFactoryTypeRegGuid = "{" + TestFactoryType.Guid + "}";
        private Mock<RegistrationContext> _contextMock;
        private ProvideMainWindowFrameControlAttribute _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _contextMock = new Mock<RegistrationContext> { DefaultValueProvider = DefaultValueProvider.Mock };

            _objectUnderTest = new ProvideMainWindowFrameControlAttribute(
                typeof(TestType),
                ViewId,
                typeof(TestFactoryType));
        }

        [TestMethod]
        public void TestUnregister_RemovesKey()
        {
            _objectUnderTest.Unregister(_contextMock.Object);

            _contextMock.Verify(c => c.RemoveKey(TestTypeRegKey));
        }

        [TestMethod]
        public void TestRegister_CreatesKeyFromControlType()
        {

            _objectUnderTest.Register(_contextMock.Object);

            _contextMock.Verify(c => c.CreateKey(TestTypeRegKey));
        }

        [TestMethod]
        public void TestRegister_SetsValues()
        {
            const string packageGuid = "{1ad74bc1-90a9-4c30-bf40-e6035cd7f4df}";
            _contextMock.Setup(c => c.ComponentType.GUID).Returns(new Guid(packageGuid));
            var keyMock = new Mock<RegistrationAttribute.Key>();
            _contextMock.Setup(c => c.CreateKey(TestTypeRegKey)).Returns(keyMock.Object);
            _objectUnderTest.Register(_contextMock.Object);

            keyMock.Verify(k => k.SetValue(null, "GCP Project Card"));
            keyMock.Verify(k => k.SetValue("Package", packageGuid));
            keyMock.Verify(k => k.SetValue("ViewFactory", TestFactoryTypeRegGuid));
            keyMock.Verify(k => k.SetValue("ViewId", ViewId));
            keyMock.Verify(k => k.SetValue("DisplayName", "#1000"));
            keyMock.Verify(k => k.SetValue("Alignment", "MenuBarRight"));
            keyMock.Verify(k => k.SetValue("FullScreenAlignment", "MenuBarRight"));
            keyMock.Verify(k => k.SetValue("Sort", 550));
            keyMock.Verify(k => k.SetValue("FullScreenSort", 550));
        }

        [Guid(Guid)]
        public class TestType
        {
            public const string Guid = "34acdaca-54a0-4753-894c-6acc58cb8688";
        }

        [Guid(Guid)]
        public class TestFactoryType
        {
            public const string Guid = "00d0016a-adab-405c-8aca-c207b9ad8a69";
        }
    }
}
