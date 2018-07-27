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

using GoogleCloudExtension.Theming;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace GoogleCloudExtensionUnitTests.Theming
{
    [TestClass]
    public class CommonDialogWindowContentTests
    {
        private Mock<ICloseSource> _viewModelMock;
        private CommonWindowContent<ICloseSource> _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _viewModelMock = new Mock<ICloseSource>();
            _objectUnderTest = new TestCommonWindowContent(_viewModelMock.Object, "Default Title");
        }

        [TestMethod]
        public void TestConstructor_SetsTitle()
        {
            const string expectedTitle = "Expected Title";

            _objectUnderTest = new TestCommonWindowContent(_viewModelMock.Object, expectedTitle);

            Assert.AreEqual(expectedTitle, _objectUnderTest.Title);
        }

        [TestMethod]
        public void TestConstructor_SetsViewModel()
        {
            Assert.AreEqual(_viewModelMock.Object, _objectUnderTest.ViewModel);
        }

        [TestMethod]
        public void TestConstructor_RegistersClose()
        {
            var eventHandlerMock = new Mock<Action>();
            _objectUnderTest.Close += eventHandlerMock.Object;

            _viewModelMock.Raise(c => c.Close += null);

            eventHandlerMock.Verify(h => h());
        }

        [TestMethod]
        public void TestOnParentClose_UnregistersClose()
        {
            var eventHandlerMock = new Mock<Action>();
            _objectUnderTest.Close += eventHandlerMock.Object;

            _objectUnderTest.OnParentClosed();
            _viewModelMock.Raise(c => c.Close += null);

            eventHandlerMock.Verify(h => h(), Times.Never);
        }

        private class TestCommonWindowContent : CommonWindowContent<ICloseSource>
        {
            public TestCommonWindowContent(ICloseSource viewModel, string title) : base(viewModel, title) { }
        }
    }
}
