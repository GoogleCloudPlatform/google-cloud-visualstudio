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
using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.CloudExplorer;
using GoogleCloudExtension.MenuBarControls;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.CloudExplorer
{
    [TestClass]
    public class CloudExplorerViewModelTests : ExtensionTestBase
    {
        private CloudExplorerViewModel _objectUnderTest;
        private ISelectionUtils _mockedSelectionUtils;
        private Mock<IGcpUserProjectViewModel> _userProjectViewModelMock;
        private TaskCompletionSource<Project> _currentProjectSource;

        protected override void BeforeEach()
        {
            _currentProjectSource = new TaskCompletionSource<Project>();

            _userProjectViewModelMock =
                new Mock<IGcpUserProjectViewModel> { DefaultValueProvider = DefaultValueProvider.Mock };
            _userProjectViewModelMock.Setup(up => up.CurrentProjectAsync)
                .Returns(new AsyncProperty<Project>(_currentProjectSource.Task));

            PackageMock.Setup(p => p.GetMefService<IGcpUserProjectViewModel>())
                .Returns(_userProjectViewModelMock.Object);

            _mockedSelectionUtils = Mock.Of<ISelectionUtils>();
            _objectUnderTest = new CloudExplorerViewModel(_mockedSelectionUtils);
        }

        [TestMethod]
        public void TestConstructor_InitalizesCommands()
        {
            Assert.IsTrue(_objectUnderTest.RefreshCommand.CanExecuteCommand);
            Assert.IsTrue(_objectUnderTest.DoubleClickCommand.CanExecuteCommand);
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
        public void TestConstructor_GetUserProjectFromMefService()
        {
            Assert.AreEqual(_userProjectViewModelMock.Object, _objectUnderTest.UserProject);
        }

        [TestMethod]
        public async Task TestRefreshCommand_ResetsCredentialsAsync()
        {
            _currentProjectSource.SetResult(new Project());

            _objectUnderTest.RefreshCommand.Execute(null);
            await _objectUnderTest.RefreshCommand.LatestExecution;

            _userProjectViewModelMock.Verify(up => up.UpdateUserProfile());
            _userProjectViewModelMock.Verify(up => up.LoadCurrentProject());
        }

        [TestMethod]
        public async Task TestRefreshCommand_AwaitsCurrentProject()
        {
            _objectUnderTest.RefreshCommand.Execute(null);
            AsyncProperty asyncProperty = _objectUnderTest.RefreshCommand.LatestExecution;

            Assert.IsFalse(asyncProperty.IsCompleted);
            _currentProjectSource.SetResult(new Project());
            await asyncProperty;
            Assert.IsTrue(asyncProperty.IsCompleted);
        }

        [TestMethod]
        public async Task TestRefreshCommand_EmptyStateCommandSetToManageAccountsWhenNoAccount()
        {
            CredentialStoreMock.Setup(cs => cs.CurrentAccount).Returns(() => null);
            var expectedCommand = Mock.Of<IProtectedCommand>();
            _userProjectViewModelMock.Setup(up => up.ManageAccountsCommand).Returns(expectedCommand);

            _objectUnderTest.RefreshCommand.Execute(null);
            _currentProjectSource.SetResult(new Project());
            await _objectUnderTest.RefreshCommand.LatestExecution;

            Assert.AreEqual(expectedCommand, _objectUnderTest.EmptyStateCommand);
        }

        [TestMethod]
        public async Task TestRefreshCommand_EmptyStateMessageSetWhenNoProject()
        {
            CredentialStoreMock.Setup(cs => cs.CurrentAccount).Returns(new UserAccount());

            _objectUnderTest.RefreshCommand.Execute(null);
            _currentProjectSource.SetResult(null);
            await _objectUnderTest.RefreshCommand.LatestExecution;

            Assert.AreEqual(Resources.CloudExploreNoProjectMessage, _objectUnderTest.EmptyStateMessage);
        }

        [TestMethod]
        public void TestSelectedProjectCommand_RedirectesToUserProject()
        {
            Assert.AreEqual(_userProjectViewModelMock.Object.SelectProjectCommand, _objectUnderTest.SelectProjectCommand);
        }

        [TestMethod]
        public void TestCurrentProject_RedirectesToUserProject()
        {
            var expectedCurrentProject = new Project();
            _userProjectViewModelMock.Setup(up => up.CurrentProjectAsync)
                .Returns(new AsyncProperty<Project>(expectedCurrentProject));

            Assert.AreEqual(expectedCurrentProject, ((ICloudSourceContext)_objectUnderTest).CurrentProject);
        }

    }
}
