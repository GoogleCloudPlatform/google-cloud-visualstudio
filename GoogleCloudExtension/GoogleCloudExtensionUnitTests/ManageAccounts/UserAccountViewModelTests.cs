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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Plus.v1.Data;
using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ManageAccounts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;

namespace GoogleCloudExtensionUnitTests.ManageAccounts
{
    [TestClass]
    public class UserAccountViewModelTests : ExtensionTestBase
    {
        private TaskCompletionSource<Person> _getProfileTaskSource;

        private static readonly UserAccount s_defaultUserAccount = new UserAccount
        {
            AccountName = "DefaultName",
            ClientId = "default-client-id",
            ClientSecret = "DefaultSecret",
            RefreshToken = "DefautRefreshToken"
        };

        protected override void BeforeEach()
        {
            _getProfileTaskSource = new TaskCompletionSource<Person>();
            DataSourceFactoryMock.Setup(dsf => dsf.CreatePlusDataSource(It.IsAny<GoogleCredential>()).GetProfileAsync())
                .Returns(_getProfileTaskSource.Task);
        }


        [TestMethod]
        public void TestConstructor_SetsUserAccount()
        {
            var objectUnderTest = new UserAccountViewModel(s_defaultUserAccount);

            Assert.AreEqual(s_defaultUserAccount, objectUnderTest.UserAccount);
        }

        [TestMethod]
        public void TestConstructor_SetsAccountName()
        {
            const string testAccountName = "TestAccountName";
            var userAccount = new UserAccount { AccountName = testAccountName };

            var objectUnderTest = new UserAccountViewModel(userAccount);

            Assert.AreEqual(testAccountName, objectUnderTest.AccountName);
        }

        [TestMethod]
        public void TestConstructor_GetsDataSourceFromGivenAccountCredential()
        {
            GoogleCredential expectedCredential = s_defaultUserAccount.GetGoogleCredential();
            var mockedUserAccount = Mock.Of<IUserAccount>(a => a.GetGoogleCredential() == expectedCredential);

            _ = new UserAccountViewModel(mockedUserAccount);

            DataSourceFactoryMock.Verify(dsf => dsf.CreatePlusDataSource(expectedCredential));
        }

        [TestMethod]
        public async Task TestConstructor_ReturnsNullProfilePictureForExceptionCreatingDataSource()
        {
            DataSourceFactoryMock.Setup(dsf => dsf.CreatePlusDataSource(It.IsAny<GoogleCredential>()))
                .Throws<Exception>();

            var objectUnderTest = new UserAccountViewModel(s_defaultUserAccount);
            await objectUnderTest.ProfilePictureAsync.SafeTask;

            Assert.IsTrue(objectUnderTest.ProfilePictureAsync.IsCompleted);
            Assert.IsNull(objectUnderTest.ProfilePictureAsync.Value);
        }

        [TestMethod]
        public async Task TestConstructor_ReturnsNullNameForExceptionCreatingDataSource()
        {
            DataSourceFactoryMock.Setup(dsf => dsf.CreatePlusDataSource(It.IsAny<GoogleCredential>()))
                .Throws<Exception>();

            var objectUnderTest = new UserAccountViewModel(s_defaultUserAccount);
            await objectUnderTest.NameAsync.SafeTask;

            Assert.IsTrue(objectUnderTest.NameAsync.IsCompleted);
            Assert.IsNull(objectUnderTest.NameAsync.Value);
        }

        [TestMethod]
        public void TestConstructor_StartsLoadingProfilePicture()
        {
            var objectUnderTest = new UserAccountViewModel(s_defaultUserAccount);

            Assert.IsTrue(objectUnderTest.ProfilePictureAsync.IsPending);
        }

        [TestMethod]
        public void TestConstructor_StartsLoadingName()
        {
            var objectUnderTest = new UserAccountViewModel(s_defaultUserAccount);

            Assert.IsTrue(objectUnderTest.NameAsync.IsPending);
        }

        [TestMethod]
        public void TestConstructor_ShowsDefaultNameWhileLoading()
        {
            var objectUnderTest = new UserAccountViewModel(s_defaultUserAccount);

            Assert.AreEqual(Resources.CloudExplorerLoadingMessage, objectUnderTest.NameAsync.Value);
        }

        [TestMethod]
        public async Task TestConstructor_LoadsProfilePictureUrl()
        {
            const string profilePictureUrl = "profile-picture-url";
            var objectUnderTest = new UserAccountViewModel(s_defaultUserAccount);

            _getProfileTaskSource.SetResult(new Person { Image = new Person.ImageData { Url = profilePictureUrl } });
            await objectUnderTest.ProfilePictureAsync.SafeTask;

            Assert.AreEqual(profilePictureUrl, objectUnderTest.ProfilePictureAsync.Value);
        }

        [TestMethod]
        public async Task TestConstructor_LoadsName()
        {
            const string profileDisplayName = "Profile Display Name";
            var objectUnderTest = new UserAccountViewModel(s_defaultUserAccount);

            _getProfileTaskSource.SetResult(new Person { DisplayName = profileDisplayName });
            await objectUnderTest.NameAsync.SafeTask;

            Assert.AreEqual(profileDisplayName, objectUnderTest.NameAsync.Value);
        }
    }
}
