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
using GoogleCloudExtension.Services.FileSystem;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TestingHelpers;

namespace GoogleCloudExtensionUnitTests.Accounts
{
    [TestClass]
    public class CredentialsStoreTests
    {
        private const string DefaultProjectId = "New Project Id";
        private const string DefaultAccountName = "Default Account Name";
        private CredentialsStore _objectUnderTest;
        private Mock<IFileSystem> _fileSystemMock;
        private Mock<Action<object, EventArgs>> _projectIdChangedHandlerMock;
        private Mock<Action<object, EventArgs>> _accountChangedHandlerMock;
        private IUserAccount _defaultUserAccount;
        private Project _defaultProject;
        private Mock<IDataSourceFactory> _dataSourceFactoryMock;
        private TaskCompletionSource<IList<Project>> _getProjectsSource;

        [TestInitialize]
        public void BeforeEach()
        {
            _getProjectsSource = new TaskCompletionSource<IList<Project>>();
            _defaultProject = new Project { ProjectId = DefaultProjectId };
            _defaultUserAccount = Mock.Of<IUserAccount>(ua => ua.AccountName == DefaultAccountName);

            _fileSystemMock = new Mock<IFileSystem> { DefaultValueProvider = DefaultValueProvider.Mock };
            _dataSourceFactoryMock = new Mock<IDataSourceFactory>();
            _dataSourceFactoryMock.Setup(dsf => dsf.CreateResourceManagerDataSource().GetProjectsListAsync())
                .Returns(() => _getProjectsSource.Task);

            _objectUnderTest = new CredentialsStore(_fileSystemMock.ToLazy(), _dataSourceFactoryMock.ToLazy());

            _projectIdChangedHandlerMock = new Mock<Action<object, EventArgs>>();
            _accountChangedHandlerMock = new Mock<Action<object, EventArgs>>();
            _objectUnderTest.CurrentProjectIdChanged += new EventHandler(_projectIdChangedHandlerMock.Object);
            _objectUnderTest.CurrentAccountChanged += new EventHandler(_accountChangedHandlerMock.Object);
        }

        [TestMethod]
        public void TestConstructor_LoadsAccounts()
        {
            const string account1FilePath = "c:\\account1.json";
            var account1 = new UserAccount { AccountName = "Account1" };
            _fileSystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
            _fileSystemMock.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>()))
                .Returns(new[] { account1FilePath });
            _fileSystemMock.Setup(fs => fs.File.ReadAllText(account1FilePath))
                .Returns(JsonConvert.SerializeObject(account1));

            _objectUnderTest = new CredentialsStore(_fileSystemMock.ToLazy(), _dataSourceFactoryMock.ToLazy());

            Assert.AreEqual(account1.AccountName, _objectUnderTest.GetAccount(account1.AccountName).AccountName);
        }

        [TestMethod]
        public void TestConstructor_SkipsNonJsonFiles()
        {
            _fileSystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
            const string notAccountFilePath = "c:\\notAnAccount.txt";
            _fileSystemMock.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>()))
                .Returns(new[] { notAccountFilePath });

            _objectUnderTest = new CredentialsStore(_fileSystemMock.ToLazy(), _dataSourceFactoryMock.ToLazy());

            _fileSystemMock.Verify(fs => fs.File.ReadAllText(notAccountFilePath), Times.Never);
        }

        [TestMethod]
        public void TestConstructor_ThrowsCredentialsStoreExceptionForIOError()
        {
            const string account1FilePath = "c:\\account1.json";
            _fileSystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
            _fileSystemMock.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>()))
                .Returns(new[] { account1FilePath });
            _fileSystemMock.Setup(fs => fs.File.ReadAllText(account1FilePath)).Throws<IOException>();

            Assert.ThrowsException<CredentialsStoreException>(
                () => new CredentialsStore(_fileSystemMock.ToLazy(), _dataSourceFactoryMock.ToLazy()));
        }

        [TestMethod]
        public void TestConstructor_ThrowsCredentialsStoreExceptionForInvalidJson()
        {
            const string account1FilePath = "c:\\account1.json";
            _fileSystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
            _fileSystemMock.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>()))
                .Returns(new[] { account1FilePath });
            _fileSystemMock.Setup(fs => fs.File.ReadAllText(account1FilePath)).Returns("This is not Json!");

            Assert.ThrowsException<CredentialsStoreException>(
                () => new CredentialsStore(_fileSystemMock.ToLazy(), _dataSourceFactoryMock.ToLazy()));
        }

        [TestMethod]
        public void TestConstructor_ResetsCredentialsFromDefault()
        {
            const string account1Name = "Account1";
            const string account1FilePath = "c:\\account1.json";
            const string expectedProjectId = "Expected Project Id";
            var account1 = new UserAccount { AccountName = account1Name };
            var defaultCredentials =
                new DefaultCredentials { AccountName = account1Name, ProjectId = expectedProjectId };
            _fileSystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
            _fileSystemMock.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>()))
                .Returns(new[] { account1FilePath });
            _fileSystemMock.Setup(fs => fs.File.ReadAllText(account1FilePath))
                .Returns(JsonConvert.SerializeObject(account1));
            _fileSystemMock.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
            _fileSystemMock
                .Setup(
                    fs => fs.File.ReadAllText(
                        It.Is<string>(
                            s => s.EndsWith(CredentialsStore.DefaultCredentialsFileName, StringComparison.Ordinal))))
                .Returns(
                    JsonConvert.SerializeObject(
                        defaultCredentials));

            _objectUnderTest = new CredentialsStore(_fileSystemMock.ToLazy(), _dataSourceFactoryMock.ToLazy());

            Assert.AreEqual(account1.AccountName, _objectUnderTest.CurrentAccount.AccountName);
            Assert.AreEqual(expectedProjectId, _objectUnderTest.CurrentProjectId);
        }

        [TestMethod]
        public void TestUpdateCurrentProject_SetsCurrentProjectId()
        {
            const string expectedProjectId = "Expected Project Id";

            _objectUnderTest.UpdateCurrentProject(new Project { ProjectId = expectedProjectId });

            Assert.AreEqual(expectedProjectId, _objectUnderTest.CurrentProjectId);
        }

        [TestMethod]
        public void TestUpdateCurrentProject_SetsCurrentProjectNumber()
        {
            const int expectedProjectNumber = 10512;
            _objectUnderTest.UpdateCurrentProject(
                new Project { ProjectId = DefaultProjectId, ProjectNumber = expectedProjectNumber });

            Assert.AreEqual(expectedProjectNumber.ToString(), _objectUnderTest.CurrentProjectNumericId);
        }

        [TestMethod]
        public void TestUpdateCurrentProject_InvokesCurrentProjetIdChanged()
        {

            _objectUnderTest.UpdateCurrentProject(_defaultProject);

            _projectIdChangedHandlerMock.Verify(h => h(_objectUnderTest, EventArgs.Empty));
        }

        [TestMethod]
        public void TestUpdateCurrentProject_WithNoUserAccountDeletesCredentials()
        {
            _objectUnderTest.UpdateCurrentAccount(null);

            _objectUnderTest.UpdateCurrentProject(_defaultProject);

            _fileSystemMock.Verify(
                fs => fs.File.Delete(
                    It.Is<string>(
                        s => s.EndsWith(CredentialsStore.DefaultCredentialsFileName, StringComparison.Ordinal))));
        }

        [TestMethod]
        public void TestUpdateCurrentProject_WithUnNamedUserAccountDeletesCredentials()
        {
            _objectUnderTest.UpdateCurrentAccount(Mock.Of<IUserAccount>());

            _objectUnderTest.UpdateCurrentProject(_defaultProject);

            _fileSystemMock.Verify(
                fs => fs.File.Delete(
                    It.Is<string>(
                        s => s.EndsWith(CredentialsStore.DefaultCredentialsFileName, StringComparison.Ordinal))));
        }

        [TestMethod]
        public void TestUpdateCurrentProject_NamedUserAccountWritesCredentials()
        {

            _objectUnderTest.UpdateCurrentAccount(_defaultUserAccount);

            _objectUnderTest.UpdateCurrentProject(_defaultProject);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(
                    It.Is<string>(
                        s => s.EndsWith(CredentialsStore.DefaultCredentialsFileName, StringComparison.Ordinal)),
                    It.Is<string>(s => IsExpectedCredentialsJson(s, DefaultAccountName, DefaultProjectId))));
        }

        [TestMethod]
        public void TestUpdateCurrentProject_DoesNothingForSameProject()
        {
            _objectUnderTest.UpdateCurrentProject(_defaultProject);
            Mock.Get(_fileSystemMock.Object.File).ResetCalls();
            _projectIdChangedHandlerMock.ResetCalls();
            _accountChangedHandlerMock.ResetCalls();

            _objectUnderTest.UpdateCurrentProject(_defaultProject);

            _fileSystemMock.Verify(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _fileSystemMock.Verify(fs => fs.File.Delete(It.IsAny<string>()), Times.Never);
            _projectIdChangedHandlerMock.Verify(f => f(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Never);
            _accountChangedHandlerMock.Verify(f => f(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Never);
        }

        [TestMethod]
        public void TestUpdateCurrentAccount_UpdatesCurrentAccount()
        {
            _objectUnderTest.UpdateCurrentAccount(_defaultUserAccount);

            Assert.AreEqual(_defaultUserAccount, _objectUnderTest.CurrentAccount);
        }

        [TestMethod]
        public void TestUpdateCurrentAccount_ClearsCurrentProjectId()
        {
            _objectUnderTest.UpdateCurrentProject(_defaultProject);
            _objectUnderTest.UpdateCurrentAccount(_defaultUserAccount);

            Assert.IsNull(_objectUnderTest.CurrentProjectId);
        }

        [TestMethod]
        public void TestUpdateCurrentAccount_ClearsNumericProjectId()
        {
            _objectUnderTest.UpdateCurrentProject(_defaultProject);
            _objectUnderTest.UpdateCurrentAccount(_defaultUserAccount);

            Assert.IsNull(_objectUnderTest.CurrentProjectNumericId);
        }

        [TestMethod]
        public void TestUpdateCurrentAccount_RaisesCurrentAccountChanged()
        {

            _objectUnderTest.UpdateCurrentAccount(_defaultUserAccount);

            _accountChangedHandlerMock.Verify(f => f(_objectUnderTest, EventArgs.Empty));
        }

        [TestMethod]
        public void TestUpdateCurrentAccount_RaisesCurrentProjectIdChanged()
        {
            _objectUnderTest.UpdateCurrentAccount(_defaultUserAccount);

            _projectIdChangedHandlerMock.Verify(f => f(_objectUnderTest, EventArgs.Empty));
        }

        [TestMethod]
        public void TestUpdateCurrentAccount_StartsCurrentAccountProjectsLoad()
        {
            Task<IEnumerable<Project>> originalTask = _objectUnderTest.CurrentAccountProjects;

            _objectUnderTest.UpdateCurrentAccount(_defaultUserAccount);

            Assert.AreNotEqual(originalTask, _objectUnderTest.CurrentAccountProjects);
        }

        [TestMethod]
        public void TestUpdateCurrentAccount_UpdatesDefaultCredentials()
        {
            _objectUnderTest.UpdateCurrentAccount(_defaultUserAccount);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(
                    It.Is<string>(
                        s => s.EndsWith(CredentialsStore.DefaultCredentialsFileName, StringComparison.Ordinal)),
                    It.Is<string>(s => IsExpectedCredentialsJson(s, DefaultAccountName, null))));
        }

        [TestMethod]
        public void TestUpdateCurrentProject_DoesNothingForSameAccount()
        {
            _objectUnderTest.UpdateCurrentAccount(_defaultUserAccount);
            Mock.Get(_fileSystemMock.Object.File).ResetCalls();
            _projectIdChangedHandlerMock.ResetCalls();

            _objectUnderTest.UpdateCurrentAccount(_defaultUserAccount);

            _fileSystemMock.Verify(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _fileSystemMock.Verify(fs => fs.File.Delete(It.IsAny<string>()), Times.Never);
            _projectIdChangedHandlerMock.Verify(f => f(It.IsAny<object>(), It.IsAny<EventArgs>()), Times.Never);
        }

        [TestMethod]
        public void TestResetCredentials_StartsCurrentAccountProjectsLoad()
        {
            Task<IEnumerable<Project>> originalTask = _objectUnderTest.CurrentAccountProjects;

            _objectUnderTest.ResetCredentials(null, null);

            Assert.AreNotEqual(originalTask, _objectUnderTest.CurrentAccountProjects);
        }

        [TestMethod]
        public void TestResetCredentials_RaisesCurrentAccountChanged()
        {

            _objectUnderTest.ResetCredentials(null, null);

            _accountChangedHandlerMock.Verify(f => f(_objectUnderTest, EventArgs.Empty));
        }

        [TestMethod]
        public void TestResetCredentials_RaisesCurrentProjectIdChanged()
        {

            _objectUnderTest.ResetCredentials(null, null);

            _projectIdChangedHandlerMock.Verify(f => f(_objectUnderTest, EventArgs.Empty));
        }

        [TestMethod]
        public void TestResetCredentials_ClearsPropertiesForMissingAccount()
        {
            _objectUnderTest.ResetCredentials(DefaultAccountName, DefaultProjectId);

            Assert.IsNull(_objectUnderTest.CurrentAccount);
            Assert.IsNull(_objectUnderTest.CurrentProjectId);
            Assert.IsNull(_objectUnderTest.CurrentProjectNumericId);
        }

        [TestMethod]
        public void TestResetCredentials_SetsFieldsForExistingAccount()
        {
            _objectUnderTest.AddAccount(_defaultUserAccount);

            _objectUnderTest.ResetCredentials(DefaultAccountName, DefaultProjectId);

            Assert.AreEqual(_defaultUserAccount, _objectUnderTest.CurrentAccount);
            Assert.AreEqual(DefaultProjectId, _objectUnderTest.CurrentProjectId);
            Assert.IsNull(_objectUnderTest.CurrentProjectNumericId);
        }

        [TestMethod]
        public void TestRefreshProjects_StartsCurrentAccountProjectsLoad()
        {
            Task<IEnumerable<Project>> originalTask = _objectUnderTest.CurrentAccountProjects;

            _objectUnderTest.RefreshProjects();

            Assert.AreNotEqual(originalTask, _objectUnderTest.CurrentAccountProjects);
        }

        [TestMethod]
        public async Task TestCurrentAccountProjects_LoadsCurrentAccountProjects()
        {
            Project[] expectedResult = { new Project(), _defaultProject };

            _objectUnderTest.RefreshProjects();
            _getProjectsSource.SetResult(expectedResult);
            IEnumerable<Project> results = await _objectUnderTest.CurrentAccountProjects;

            CollectionAssert.AreEqual(expectedResult, results.ToList());
        }

        [TestMethod]
        public async Task TestCurrentAccountProjects_GetsEmptyOnTaskException()
        {
            _objectUnderTest.RefreshProjects();
            _getProjectsSource.SetException(new Exception());
            IEnumerable<Project> results = await _objectUnderTest.CurrentAccountProjects;

            CollectionAssert.That.IsEmpty(results);
        }

        [TestMethod]
        public void TestDeleteAccount_ThrowExceptionForNonExistantAccount()
        {
            Assert.ThrowsException<InvalidOperationException>(
                () => _objectUnderTest.DeleteAccount(_defaultUserAccount));
        }

        [TestMethod]
        public void TestDeleteAccount_DeletesAccountFile()
        {
            _objectUnderTest.AddAccount(_defaultUserAccount);

            _objectUnderTest.DeleteAccount(_defaultUserAccount);

            _fileSystemMock.Verify(fs => fs.File.Delete(It.IsAny<string>()));
        }

        [TestMethod]
        public void TestDeleteAccount_ReloadsAccountCache()
        {
            _fileSystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
            _fileSystemMock.Setup(fs => fs.Directory.EnumerateFiles(It.IsAny<string>()))
                .Returns(Enumerable.Empty<string>());
            _objectUnderTest.AddAccount(_defaultUserAccount);

            _objectUnderTest.DeleteAccount(_defaultUserAccount);

            _fileSystemMock.Verify(fs => fs.Directory.EnumerateFiles(It.IsAny<string>()));
        }

        [TestMethod]
        public void TestDeleteAccount_ResetsCredentialsWhenCurrent()
        {
            _objectUnderTest.AddAccount(_defaultUserAccount);
            _objectUnderTest.UpdateCurrentAccount(_defaultUserAccount);

            _objectUnderTest.DeleteAccount(_defaultUserAccount);

            Assert.IsNull(_objectUnderTest.CurrentAccount);
        }

        [TestMethod]
        public void TestAddAccount_CreatesCredentailsRootWhenMissing()
        {
            _fileSystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(false);

            _objectUnderTest.AddAccount(_defaultUserAccount);

            _fileSystemMock.Verify(fs => fs.Directory.CreateDirectory(It.IsAny<string>()));
        }

        [TestMethod]
        public void TestAddAccount_SkipsCreatesCredentailsRootWhenExistant()
        {
            _fileSystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);

            _objectUnderTest.AddAccount(_defaultUserAccount);

            _fileSystemMock.Verify(fs => fs.Directory.CreateDirectory(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void TestAddAccount_SavesUserFile()
        {
            _objectUnderTest.AddAccount(_defaultUserAccount);

            _fileSystemMock.Verify(
                fs => fs.File.WriteAllText(It.IsAny<string>(), JsonConvert.SerializeObject(_defaultUserAccount)));
        }

        [TestMethod]
        public void TestAddAccount_ThrowsCredentialsStoreExceptionForFileIOException()
        {
            _fileSystemMock.Setup(
                fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Throws<IOException>();

            Assert.ThrowsException<CredentialsStoreException>(() => _objectUnderTest.AddAccount(_defaultUserAccount));
        }

        [TestMethod]
        public void TestAddAccount_AddsAccount()
        {
            _objectUnderTest.AddAccount(_defaultUserAccount);

            Assert.AreEqual(_defaultUserAccount, _objectUnderTest.GetAccount(_defaultUserAccount.AccountName));
        }

        [TestMethod]
        public void TestGetAccount_ReturnsNullForNull()
        {
            IUserAccount result = _objectUnderTest.GetAccount(null);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetAccount_GetsNullForMissingAccount()
        {
            IUserAccount result = _objectUnderTest.GetAccount(DefaultAccountName);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGetAccount_GetsAccount()
        {
            _objectUnderTest.AddAccount(_defaultUserAccount);

            IUserAccount result = _objectUnderTest.GetAccount(_defaultUserAccount.AccountName);

            Assert.AreEqual(_defaultUserAccount, result);
        }

        private static bool IsExpectedCredentialsJson(string s, string accountName, string projectId)
        {
            var credentials = JsonConvert.DeserializeObject<DefaultCredentials>(s);
            return credentials.AccountName == accountName && credentials.ProjectId == projectId;
        }
    }
}
