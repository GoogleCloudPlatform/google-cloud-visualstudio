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
using Google.Apis.Plus.v1.Data;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ManageAccounts;
using GoogleCloudExtension.MenuBarControls;
using GoogleCloudExtension.PickProjectDialog;
using GoogleCloudExtension.Services;
using GoogleCloudExtension.Utils;
using GoogleCloudExtension.Utils.Async;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.MenuBarControls
{
    [TestClass]
    public class GcpUserProjectViewModelTests : ExtensionTestBase
    {
        private GcpUserProjectViewModel _objectUnderTest;
        private Mock<IDataSourceFactory> _dataSourceFactoryMock;
        private Mock<ICredentialsStore> _credentialsStoreMock;
        private Mock<IUserPromptService> _userPromptServiceMock;

        [TestInitialize]
        public new void BeforeEach()
        {
            _dataSourceFactoryMock = new Mock<IDataSourceFactory>();
            _credentialsStoreMock = new Mock<ICredentialsStore>();
            _userPromptServiceMock = new Mock<IUserPromptService>();
            _objectUnderTest = new GcpUserProjectViewModel(
                _dataSourceFactoryMock.Object,
                _credentialsStoreMock.Object,
                _userPromptServiceMock.ToLazy());
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
            AsyncProperty<string> origianlPictureProperty = _objectUnderTest.ProfilePictureUrlAsync;

            _dataSourceFactoryMock.Raise(ds => ds.DataSourcesUpdated += null, EventArgs.Empty);

            Assert.AreNotEqual(origianlPictureProperty, _objectUnderTest.ProfilePictureUrlAsync);
        }

        [TestMethod]
        public void TestOpenPopup_OpensPopup()
        {
            _objectUnderTest.OpenPopup.Execute(null);

            Assert.IsTrue(_objectUnderTest.IsPopupOpen);
        }

        [TestMethod]
        public void TestManageAccountsCommand_PromptsUser()
        {
            _objectUnderTest.ManageAccountsCommand.Execute(null);

            _userPromptServiceMock.Verify(p => p.PromptUser(It.IsAny<ManageAccountsWindowContent>()));
        }

        [TestMethod]
        public void TestSelectProjectCommand_PromptsUser()
        {
            _objectUnderTest.SelectProjectCommand.Execute(null);

            _userPromptServiceMock.Verify(p => p.PromptUser(It.IsAny<PickProjectIdWindowContent>()));
        }

        [TestMethod]
        public void TestSelectProjectCommand_UpdatesCurrentProjectAsync()
        {
            var expectedProject = new Project();
            _userPromptServiceMock.Setup(p => p.PromptUser(It.IsAny<PickProjectIdWindowContent>()))
                .Returns(expectedProject);

            _objectUnderTest.SelectProjectCommand.Execute(null);

            Assert.AreEqual(expectedProject, _objectUnderTest.CurrentProjectAsync.Value);
        }

        [TestMethod]
        public void TestSelectProjectCommand_UpdatesCredentialsStore()
        {
            var expectedProject = new Project();
            _userPromptServiceMock.Setup(p => p.PromptUser(It.IsAny<PickProjectIdWindowContent>()))
                .Returns(expectedProject);

            _objectUnderTest.SelectProjectCommand.Execute(null);

            _credentialsStoreMock.Verify(cs => cs.UpdateCurrentProject(expectedProject));
        }

        [TestMethod]
        public void TestSelectProjectCommand_SkipsUpdatesWhenCanceled()
        {
            AsyncProperty<Project> originalCurrentProjectProperty = _objectUnderTest.CurrentProjectAsync;
            _userPromptServiceMock.Setup(p => p.PromptUser(It.IsAny<PickProjectIdWindowContent>()))
                .Returns(() => null);

            _objectUnderTest.SelectProjectCommand.Execute(null);

            _credentialsStoreMock.Verify(cs => cs.UpdateCurrentProject(It.IsAny<Project>()), Times.Never);
            Assert.AreEqual(originalCurrentProjectProperty, _objectUnderTest.CurrentProjectAsync);
        }

        [TestMethod]
        public void TestUpdateUserProfile_SetsNullResultsForNoDataSource()
        {
            _dataSourceFactoryMock.Setup(ds => ds.GPlusDataSource).Returns(() => null);

            _objectUnderTest.UpdateUserProfile();

            Assert.IsNull(_objectUnderTest.ProfilePictureUrlAsync.Value);
            Assert.IsNull(_objectUnderTest.ProfileNameAsync.Value);
            Assert.IsNull(_objectUnderTest.ProfileEmailAsyc.Value);
        }

        [TestMethod]
        public async Task TestUpdateUserProfile_GetsDataFromDataSource()
        {
            const string expectedImageUrl = "Expected Url";
            const string expectedDisplayName = "Expected Display Name";
            const string expectedEmailAdderss = "Expected Email Adderss";
            _dataSourceFactoryMock.Setup(ds => ds.GPlusDataSource.GetProfileAsync())
                .ReturnsResult(
                    new Person
                    {
                        Image = new Person.ImageData { Url = expectedImageUrl },
                        DisplayName = expectedDisplayName,
                        Emails = new List<Person.EmailsData> { new Person.EmailsData { Value = expectedEmailAdderss } }
                    });

            _objectUnderTest.UpdateUserProfile();

            await _objectUnderTest.ProfilePictureUrlAsync;
            await _objectUnderTest.ProfileNameAsync;
            await _objectUnderTest.ProfileEmailAsyc;

            Assert.AreEqual(expectedImageUrl, _objectUnderTest.ProfilePictureUrlAsync.Value);
            Assert.AreEqual(expectedDisplayName, _objectUnderTest.ProfileNameAsync.Value);
            Assert.AreEqual(expectedEmailAdderss, _objectUnderTest.ProfileEmailAsyc.Value);
        }

        [TestMethod]
        public void TestLoadCurrentProject_SetsDefaultValueWhenReloading()
        {
            const string defaultProjectId = "ExpectedProjectId";
            var expectedDefaultProject = new Project { ProjectId = defaultProjectId };
            _userPromptServiceMock.Setup(s => s.PromptUser(It.IsAny<PickProjectIdWindowContent>()))
                .Returns(expectedDefaultProject);
            _credentialsStoreMock.Setup(cs => cs.CurrentProjectId).Returns(defaultProjectId);
            _dataSourceFactoryMock.Setup(dsf => dsf.ResourceManagerDataSource.GetProjectAsync(It.IsAny<string>()))
                .Returns(new TaskCompletionSource<Project>().Task);

            _objectUnderTest.SelectProjectCommand.Execute(null);
            _objectUnderTest.LoadCurrentProject();

            Assert.AreEqual(expectedDefaultProject, _objectUnderTest.CurrentProjectAsync.Value);
            Assert.IsTrue(_objectUnderTest.CurrentProjectAsync.IsPending);
        }

        [TestMethod]
        public void TestLoadCurrentProject_SetsNullDefaultValueWhenDifferent()
        {
            var currentViewModelProject = new Project { ProjectId = "ViewModelProjectId" };
            _userPromptServiceMock.Setup(s => s.PromptUser(It.IsAny<PickProjectIdWindowContent>()))
                .Returns(currentViewModelProject);
            _credentialsStoreMock.Setup(cs => cs.CurrentProjectId).Returns("CredentialsStoreProjectId");
            _dataSourceFactoryMock.Setup(dsf => dsf.ResourceManagerDataSource.GetProjectAsync(It.IsAny<string>()))
                .Returns(new TaskCompletionSource<Project>().Task);

            _objectUnderTest.SelectProjectCommand.Execute(null);
            _objectUnderTest.LoadCurrentProject();

            Assert.IsNull(_objectUnderTest.CurrentProjectAsync.Value);
            Assert.IsTrue(_objectUnderTest.CurrentProjectAsync.IsPending);
        }

        [TestMethod]
        public async Task TestLoadCurrentProject_SetsNullWhenNullProjectId()
        {
            _credentialsStoreMock.Setup(cs => cs.CurrentProjectId).Returns(() => null);

            _objectUnderTest.LoadCurrentProject();
            await _objectUnderTest.CurrentProjectAsync;

            Assert.IsNull(_objectUnderTest.CurrentProjectAsync.Value);
        }

        [TestMethod]
        public async Task TestLoadCurrentProject_GetsProjectFromDataSource()
        {
            var expectedProject = new Project();
            const string currentProjectId = "CurrentProjectId";
            _credentialsStoreMock.Setup(cs => cs.CurrentProjectId).Returns(currentProjectId);
            _dataSourceFactoryMock.Setup(dsf => dsf.ResourceManagerDataSource.GetProjectAsync(currentProjectId))
                .ReturnsResult(expectedProject);

            _objectUnderTest.LoadCurrentProject();
            await _objectUnderTest.CurrentProjectAsync;

            Assert.AreEqual(expectedProject, _objectUnderTest.CurrentProjectAsync.Value);
        }
    }
}
