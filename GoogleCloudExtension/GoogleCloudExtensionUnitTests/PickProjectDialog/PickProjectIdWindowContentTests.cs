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

using Google.Apis.CloudResourceManager.v1.Data;
using GoogleCloudExtension.PickProjectDialog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;

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
            // Initalize data bindings. See https://stackoverflow.com/questions/5396805/force-binding-in-wpf
            _objectUnderTest.Dispatcher.Invoke(() => { }, DispatcherPriority.SystemIdle);
        }

        [TestMethod]
        public void TestInitalConditions()
        {
            Assert.AreEqual(_objectUnderTest.DataContext, _viewModelMock.Object);
            Assert.IsTrue(_objectUnderTest._filter.IsFocused);
        }

        [TestMethod]
        public void TestFilterUpdated()
        {
            var visibleProject = new Project { Name = "Visible", ProjectId = "2" };
            _viewModelMock.Setup(vm => vm.FilterItem(It.IsAny<Project>())).Returns(false);
            _viewModelMock.Setup(vm => vm.FilterItem(visibleProject)).Returns(true);

            _viewModelMock.SetupGet(vm => vm.Projects).Returns(
                new[]
                {
                    new Project {Name = "Filtered Out", ProjectId = "1"},
                    visibleProject
                });
            _viewModelMock.Raise(
                vm => vm.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IPickProjectIdViewModel.Projects)));

            var cvs = (CollectionViewSource)_objectUnderTest.Resources[PickProjectIdWindowContent.CvsKey];
            CollectionAssert.AreEqual(new[] { visibleProject }, cvs.View.Cast<Project>().ToList());
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
