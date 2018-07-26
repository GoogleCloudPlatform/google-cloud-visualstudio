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
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.MenuBarControls;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Windows.Media.Imaging;

namespace GoogleCloudExtensionUnitTests.MenuBarControls
{
    [TestClass]
    public class GcpUserProjectViewModelTests
    {
        private GcpUserProjectViewModel _objectUnderTest;
        private Mock<IDataSourceFactory> _dataSourceFactoryMock;
        private Mock<ICredentialsStore> _credentialsStoreMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _dataSourceFactoryMock = new Mock<IDataSourceFactory>();
            _credentialsStoreMock = new Mock<ICredentialsStore>();
            _objectUnderTest = new GcpUserProjectViewModel(_dataSourceFactoryMock.Object, _credentialsStoreMock.Object);
        }

        [TestMethod]
        public void TestConstructor_InitalizesCurrentProjectAsync()
        {
            Assert.IsNotNull(_objectUnderTest.CurrentProjectAsync);
        }

        [TestMethod]
        public void TestConstructor_RegistersCurrentProjectIdChanged()
        {
            AsyncProperty<Project> origianlProjectProperty = _objectUnderTest.CurrentProjectAsync;

            _credentialsStoreMock.Raise(cs => cs.CurrentProjectIdChanged += null, EventArgs.Empty);

            Assert.AreNotEqual(origianlProjectProperty, _objectUnderTest.CurrentProjectAsync);
        }

        [TestMethod]
        public void TestConstructor_RegistersDataSourceUpdated()
        {
            AsyncProperty<BitmapImage> origianlPictureProperty = _objectUnderTest.ProfilePictureAsync;

            _dataSourceFactoryMock.Raise(ds => ds.DataSourcesUpdated += null, EventArgs.Empty);

            Assert.AreNotEqual(origianlPictureProperty, _objectUnderTest.ProfilePictureAsync);
        }

        [TestMethod]
        public void TestOpenPopup_OpensPopup()
        {
            _objectUnderTest.OpenPopup.Execute(null);

            Assert.IsTrue(_objectUnderTest.IsPopupOpen);
        }
    }
}
