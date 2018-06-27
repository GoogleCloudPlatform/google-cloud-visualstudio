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
using Google.Apis.Auth.OAuth2.Flows;
using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class DataSourceFactoryTests : ExtensionTestBase
    {
        private IDataSourceFactory _objectUnderTest;

        protected override void BeforeEach()
        {
            _objectUnderTest = new DataSourceFactory(CredentialStoreMock.Object);
        }

        [TestMethod]
        public void TestCreateResourceManagerDataSource_ReturnsNullForNoCredentials()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential).Returns(() => null);

            ResourceManagerDataSource result = _objectUnderTest.CreateResourceManagerDataSource();

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Test0ArgCreatePlusDataSource_ReturnsNullForNoCredentials()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential).Returns(() => null);

            IGPlusDataSource result = _objectUnderTest.CreatePlusDataSource();

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestCreateResourceManagerDataSource_Returns()
        {
            var userAccount = new UserAccount
            {
                AccountName = "TestAccountName",
                ClientId = "TestClientId",
                ClientSecret = "TestClientSecret",
                RefreshToken = "TestRefreshToken"
            };
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential)
                .Returns(userAccount.GetGoogleCredential());

            ResourceManagerDataSource result = _objectUnderTest.CreateResourceManagerDataSource();

            var googleCredential = (GoogleCredential)result.Service.HttpClientInitializer;
            var userCredential = (UserCredential)googleCredential.UnderlyingCredential;
            var flow = (GoogleAuthorizationCodeFlow)userCredential.Flow;
            Assert.AreEqual(userAccount.ClientSecret, flow.ClientSecrets.ClientSecret);
            Assert.AreEqual(userAccount.ClientId, flow.ClientSecrets.ClientId);
            Assert.AreEqual(userAccount.RefreshToken, userCredential.Token.RefreshToken);
            Assert.AreEqual(GoogleCloudExtensionPackage.Instance.VersionedApplicationName, result.Service.ApplicationName);
        }

        [TestMethod]
        public void Test0ArgCreatePlusDataSource_Returns()
        {
            var userAccount = new UserAccount
            {
                AccountName = "TestAccountName",
                ClientId = "TestClientId",
                ClientSecret = "TestClientSecret",
                RefreshToken = "TestRefreshToken"
            };
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential).Returns(userAccount.GetGoogleCredential());

            IGPlusDataSource result = _objectUnderTest.CreatePlusDataSource();

            var dataSource = (GPlusDataSource)result;
            var googleCredential = (GoogleCredential)dataSource.Service.HttpClientInitializer;
            var userCredential = (UserCredential)googleCredential.UnderlyingCredential;
            var flow = (GoogleAuthorizationCodeFlow)userCredential.Flow;
            Assert.AreEqual(userAccount.ClientSecret, flow.ClientSecrets.ClientSecret);
            Assert.AreEqual(userAccount.ClientId, flow.ClientSecrets.ClientId);
            Assert.AreEqual(userAccount.RefreshToken, userCredential.Token.RefreshToken);
            Assert.AreEqual(GoogleCloudExtensionPackage.Instance.VersionedApplicationName, dataSource.Service.ApplicationName);
        }

        [TestMethod]
        public void Test1ArgCreatePlusDataSource_Returns()
        {
            var userAccount = new UserAccount
            {
                AccountName = "TestAccountName",
                ClientId = "TestClientId",
                ClientSecret = "TestClientSecret",
                RefreshToken = "TestRefreshToken"
            };

            IGPlusDataSource result = _objectUnderTest.CreatePlusDataSource(userAccount.GetGoogleCredential());

            var dataSource = (GPlusDataSource)result;
            var googleCredential = (GoogleCredential)dataSource.Service.HttpClientInitializer;
            var userCredential = (UserCredential)googleCredential.UnderlyingCredential;
            var flow = (GoogleAuthorizationCodeFlow)userCredential.Flow;
            Assert.AreEqual(userAccount.ClientSecret, flow.ClientSecrets.ClientSecret);
            Assert.AreEqual(userAccount.ClientId, flow.ClientSecrets.ClientId);
            Assert.AreEqual(userAccount.RefreshToken, userCredential.Token.RefreshToken);
            Assert.AreEqual(GoogleCloudExtensionPackage.Instance.VersionedApplicationName, dataSource.Service.ApplicationName);
        }

        [TestMethod]
        public void Test1ArgCreatePlusDataSource_ReturnsNullForNoCredentials()
        {
            IGPlusDataSource result = _objectUnderTest.CreatePlusDataSource(null);

            Assert.IsNull(result);
        }
    }
}
