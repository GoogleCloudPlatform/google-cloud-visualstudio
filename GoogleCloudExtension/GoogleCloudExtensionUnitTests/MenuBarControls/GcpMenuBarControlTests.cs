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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace GoogleCloudExtensionUnitTests.MenuBarControls
{
    [TestClass]
    public class GcpMenuBarControlTests
    {
        private GcpMenuBarControl _objectUnderTest;
        private Mock<IGcpUserProjectViewModel> _viewModelMock;

        [TestInitialize]
        public void BeforeEach()
        {
            Application.Current.Resources.MergedDictionaries.Add(GcpMenuBarPopupControlTests.ResourceDictionary);
            _viewModelMock = new Mock<IGcpUserProjectViewModel>();
            _objectUnderTest = new GcpMenuBarControl(_viewModelMock.Object);
        }

        [TestCleanup]
        public void AfterEach() =>
            Application.Current.Resources.MergedDictionaries.Remove(GcpMenuBarPopupControlTests.ResourceDictionary);

        [TestMethod]
        public void TestConstructor_SetsDataContext()
        {
            Assert.AreEqual(_viewModelMock.Object, _objectUnderTest.DataContext);
        }

        [TestMethod]
        public void TestGetDataSource()
        {
            var dataSource = Mock.Of<IVsUISimpleDataSource>();
            _objectUnderTest.put_DataSource(dataSource);

            int hrResult = _objectUnderTest.get_DataSource(out IVsUISimpleDataSource output);

            Assert.AreEqual(VSConstants.S_OK, hrResult);
            Assert.AreEqual(dataSource, output);
        }

        [TestMethod]
        public void TestPutDataSource()
        {
            var dataSource = Mock.Of<IVsUISimpleDataSource>();

            int hrResult = _objectUnderTest.put_DataSource(dataSource);
            _objectUnderTest.get_DataSource(out IVsUISimpleDataSource output);

            Assert.AreEqual(VSConstants.S_OK, hrResult);
            Assert.AreEqual(dataSource, output);
        }

        [TestMethod]
        public void TestTranslateAccelerator() => Assert.AreEqual(
            VSConstants.S_OK,
            _objectUnderTest.TranslateAccelerator(Mock.Of<IVsUIAccelerator>()));

        [TestMethod]
        public void TestGetUIObject_OutputsSelf()
        {
            int hrResult = _objectUnderTest.GetUIObject(out object uiObject);
            Assert.AreEqual(VSConstants.S_OK, hrResult);
            Assert.AreEqual(_objectUnderTest, uiObject);
        }

        [TestMethod]
        public void TestCreateFrameworkElement_OutputsSelf()
        {
            int hrResult = _objectUnderTest.CreateFrameworkElement(out object uiObject);
            Assert.AreEqual(VSConstants.S_FALSE, hrResult);
            Assert.AreEqual(_objectUnderTest, uiObject);
        }

        [TestMethod]
        public void TestGetFrameworkElement_OutputsSelf()
        {
            int hrResult = _objectUnderTest.GetFrameworkElement(out object uiObject);
            Assert.AreEqual(VSConstants.S_OK, hrResult);
            Assert.AreEqual(_objectUnderTest, uiObject);
        }

        [TestMethod]
        public void TestHitTest()
        {
            int result = ((INonClientArea)_objectUnderTest).HitTest(default);

            Assert.AreEqual(GcpMenuBarControl.HTCLIENT, result);
        }
    }
}
