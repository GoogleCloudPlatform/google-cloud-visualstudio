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

using Google.Apis.Plus.v1.Data;
using GoogleCloudExtension;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.CloudExplorer
{
    [TestClass]
    public class CloudExplorerViewModelTests : ExtensionTestBase
    {
        private CloudExplorerViewModel _objectUnderTest;
        private ISelectionUtils _mockedSelectionUtils;
        private Mock<IGPlusDataSource> _gPlusDataSourceMock;
        private SynchronizedCollection<string> _propertiesChanged;

        protected override void BeforeEach()
        {
            _gPlusDataSourceMock = new Mock<IGPlusDataSource>();

            PackageMock.Setup(p => p.DataSourceFactory.GPlusDataSource).Returns(_gPlusDataSourceMock.Object);

            _mockedSelectionUtils = Mock.Of<ISelectionUtils>();
            _objectUnderTest = new CloudExplorerViewModel(_mockedSelectionUtils);
            _propertiesChanged = new SynchronizedCollection<string>();
            _objectUnderTest.PropertyChanged += (sender, args) => _propertiesChanged.Add(args.PropertyName);
        }

        [TestMethod]
        public void TestConstructor_InitalizesCommands()
        {
            Assert.IsTrue(_objectUnderTest.RefreshCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.ManageAccountsCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.DoubleClickCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.SelectProjectCommand.CanExecuteCommand);
        }

        [TestMethod]
        public void TestConstructor_InitalizesButtons()
        {
            ButtonDefinition button = _objectUnderTest.Buttons.Single();
            Assert.AreEqual(CloudExplorerViewModel.s_refreshIcon.Value, button.Icon);
            Assert.AreEqual(Resources.CloudExplorerRefreshButtonToolTip, button.ToolTip);
            Assert.AreEqual(_objectUnderTest.RefreshCommand, button.Command);
        }

        [TestMethod]
        public async Task TestRefreshCommand_ResetsCredentialsAsync()
        {
            const string profileName = "NewProfileName";
            var getProfileResult = new Person
            {
                Emails = new[] { new Person.EmailsData { Value = profileName } },
                Image = new Person.ImageData()
            };
            _gPlusDataSourceMock.Setup(ds => ds.GetProfileAsync()).ReturnsResult(getProfileResult);
            _propertiesChanged.Clear();

            _objectUnderTest.RefreshCommand.Execute(null);
            await _objectUnderTest.RefreshCommand.LatestExecution.SafeTask;
            await _objectUnderTest.ProfileNameAsync.SafeTask;

            CollectionAssert.Contains(_propertiesChanged, nameof(_objectUnderTest.ProfileNameAsync));
            Assert.AreEqual(profileName, _objectUnderTest.ProfileNameAsync.Value);
        }
    }
}
