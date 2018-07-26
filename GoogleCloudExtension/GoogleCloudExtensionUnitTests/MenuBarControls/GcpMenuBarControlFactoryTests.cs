﻿// Copyright 2018 Google Inc. All Rights Reserved.
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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.MenuBarControls
{
    [TestClass]
    public class GcpMenuBarControlFactoryTests
    {
        private GcpMenuBarControlFactory _objectUnderTest;
        private Mock<IGcpMenuBarControl> _controlMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _controlMock = new Mock<IGcpMenuBarControl>();
            _objectUnderTest = new GcpMenuBarControlFactory(_controlMock.ToLazy());
        }

        [TestMethod]
        public void TestCreateUIElement_ReturnsControl()
        {
            Guid empty = Guid.Empty;

            int hrResult = _objectUnderTest.CreateUIElement(ref empty, 0, out IVsUIElement output);

            Assert.AreEqual(VSConstants.S_OK, hrResult);
            Assert.AreEqual(_controlMock.Object, output);
        }
    }
}
