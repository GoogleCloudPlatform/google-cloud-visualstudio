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
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.Theming
{
    [TestClass]
    public class CommonDialogWindowTests : WpfTestBase<CommonDialogWindow<ICloseSource>>
    {
        private CommonDialogWindow<ICloseSource> _objectUnderTest;
        private Mock<ICommonWindowContent<ICloseSource>> _contentMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _contentMock =
                new Mock<ICommonWindowContent<ICloseSource>> { DefaultValueProvider = DefaultValueProvider.Mock };
            _contentMock.Setup(c => c.Title).Returns("Default Title");
            _objectUnderTest = new CommonDialogWindow<ICloseSource>(_contentMock.Object);
        }

        [TestMethod]
        public void TestConstructor_SetsContent()
        {
            Assert.AreEqual(_contentMock.Object, _objectUnderTest.Content);
        }

        [TestMethod]
        public void TestConstructor_SetsTitle()
        {
            const string expectedTitle = "Expected Title";
            _contentMock.Setup(c => c.Title).Returns(expectedTitle);

            _objectUnderTest = new CommonDialogWindow<ICloseSource>(_contentMock.Object);

            Assert.AreEqual(expectedTitle, _objectUnderTest.Title);
        }

        [TestMethod]
        public async Task TestConstructor_RegistersClose()
        {
            bool? showModalResult = await GetResult(
                w => _contentMock.Raise(c => c.ViewModel.Close += null),
                () => _objectUnderTest.ShowModal());

            Assert.IsTrue(showModalResult.HasValue);
            Assert.IsFalse(showModalResult.Value);
        }

        protected override void RegisterActivatedEvent(EventHandler handler) => _objectUnderTest.Activated += handler;

        protected override void UnregisterActivatedEvent(EventHandler handler) => _objectUnderTest.Activated -= handler;
    }
}
