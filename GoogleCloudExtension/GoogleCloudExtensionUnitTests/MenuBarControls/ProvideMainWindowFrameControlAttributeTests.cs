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

using System;
using System.Runtime.InteropServices;
using GoogleCloudExtension.MenuBarControls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.MenuBarControls
{
    [TestClass]
    public class ProvideMainWindowFrameControlAttributeTests
    {
        private const int ViewId = 456;
        private const string TestTypeRegKey = @"MainWindowFrameControls\{" + TestType.Guid + "}";
        private const string TestFactoryTypeRegGuid = "{" + TestFactoryType.Guid + "}";
        private Mock<RegistrationAttribute.RegistrationContext> _contextMock;
        private ProvideMainWindowFrameControlAttribute _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _contextMock = new Mock<RegistrationAttribute.RegistrationContext> { DefaultValueProvider = DefaultValueProvider.Mock };

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

            keyMock.Verify(k => k.SetValue(null, _objectUnderTest.Name));
            keyMock.Verify(k => k.SetValue("Package", packageGuid));
            keyMock.Verify(k => k.SetValue("ViewFactory", TestFactoryTypeRegGuid));
            keyMock.Verify(k => k.SetValue("ViewId", ViewId));
            keyMock.Verify(k => k.SetValue("DisplayName", _objectUnderTest.DisplayNameResourceKey));
            keyMock.Verify(k => k.SetValue("Alignment", _objectUnderTest.Alignment.ToString()));
            keyMock.Verify(k => k.SetValue("FullScreenAlignment", _objectUnderTest.Alignment.ToString()));
            keyMock.Verify(k => k.SetValue("Sort", _objectUnderTest.Sort));
            keyMock.Verify(k => k.SetValue("FullScreenSort", _objectUnderTest.Sort));
        }

        [TestMethod]
        public void TestDisplayNameResourceKey_SetsProperty()
        {
            const string expectedValue = "Expected Value";

            _objectUnderTest.DisplayNameResourceKey = expectedValue;

            Assert.AreEqual(expectedValue, _objectUnderTest.DisplayNameResourceKey);
        }

        [TestMethod]
        public void TestAlignment_SetsProperty()
        {
            const ProvideMainWindowFrameControlAttribute.AlignmentEnum expectedValue =
                (ProvideMainWindowFrameControlAttribute.AlignmentEnum)5;

            _objectUnderTest.Alignment = expectedValue;

            Assert.AreEqual(expectedValue, _objectUnderTest.Alignment);
        }

        [TestMethod]
        public void TestSort_SetsProperty()
        {
            const int expectedValue = 2123;

            _objectUnderTest.Sort = expectedValue;

            Assert.AreEqual(expectedValue, _objectUnderTest.Sort);
        }

        [TestMethod]
        public void TestName_SetsProperty()
        {
            const string expectedValue = "Expected Value";

            _objectUnderTest.Name = expectedValue;

            Assert.AreEqual(expectedValue, _objectUnderTest.Name);
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
