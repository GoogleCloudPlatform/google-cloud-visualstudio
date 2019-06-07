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

using System.ComponentModel;
using GoogleCloudExtension.PickProjectDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.PickProjectDialog
{
    [TestClass]
    public class PickProjectIdWindowContentTests : ExtensionTestBase
    {
        private PickProjectIdWindowContent _objectUnderTest;
        private Mock<IPickProjectIdViewModel> _viewModelMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _viewModelMock = new Mock<IPickProjectIdViewModel>();
            _objectUnderTest = new PickProjectIdWindowContent(_viewModelMock.Object);
        }

        [TestMethod]
        public void TestInitialConditions()
        {
            Assert.AreEqual(_objectUnderTest.DataContext, _viewModelMock.Object);
            Assert.IsTrue(_objectUnderTest._filter.IsFocused);
        }

        [TestMethod]
        public void TestFilterUpdateWithNullViewDoesNotThrow()
        {
            _viewModelMock.SetupGet(vm => vm.Projects).Returns(() => null);
            _viewModelMock.Raise(
                vm => vm.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IPickProjectIdViewModel.Projects)));

            _viewModelMock.SetupGet(vm => vm.Filter).Returns("Visible");
            _viewModelMock.Raise(
                vm => vm.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IPickProjectIdViewModel.Filter)));
        }
    }
}
