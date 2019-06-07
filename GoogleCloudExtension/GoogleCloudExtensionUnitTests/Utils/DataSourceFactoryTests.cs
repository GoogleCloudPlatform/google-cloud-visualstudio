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

using System;
using System.Diagnostics.CodeAnalysis;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;
using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.DataSources;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.Utils
{
    [TestClass]
    public class DataSourceFactoryTests : ExtensionTestBase
    {
        private DataSourceFactory _objectUnderTest;

        private UserAccount _userAccount;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new DataSourceFactory(CredentialStoreMock.Object);
            _userAccount = new UserAccount
            {
                AccountName = "TestAccountName",
                ClientId = "TestClientId",
                ClientSecret = "TestClientSecret",
                RefreshToken = "TestRefreshToken"
            };
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential).Returns(() => _userAccount.GetGoogleCredential());
        }

        [TestMethod]
        public void TestDefault_DefersToPackage()
        {
            var expectedFactory = Mock.Of<IDataSourceFactory>();
            PackageMock.Setup(p => p.DataSourceFactory).Returns(expectedFactory);

            Assert.AreEqual(expectedFactory, DataSourceFactory.Default);
        }

        [TestMethod]
        public void TestCurrentAccountChanged_UpdatesResourceManagerDataSource()
        {
            IResourceManagerDataSource origianlResourceDataSource = _objectUnderTest.ResourceManagerDataSource;

            CredentialStoreMock.Raise(cs => cs.CurrentAccountChanged += null, EventArgs.Empty);

            Assert.AreNotEqual(origianlResourceDataSource, _objectUnderTest.ResourceManagerDataSource);
        }

        [TestMethod]
        public void TestCurrentAccountChanged_UpdatesGPlusDataSource()
        {
            IGPlusDataSource origianlGPlusDataSource = _objectUnderTest.GPlusDataSource;

            CredentialStoreMock.Raise(cs => cs.CurrentAccountChanged += null, EventArgs.Empty);

            Assert.AreNotEqual(origianlGPlusDataSource, _objectUnderTest.GPlusDataSource);
        }

        [TestMethod]
        public void TestCurrentAccountChanged_InvokesDataSourcesUpdated()
        {
            var eventHandlerMock = new Mock<Action<object, EventArgs>>();
            _objectUnderTest.DataSourcesUpdated += new EventHandler(eventHandlerMock.Object);

            CredentialStoreMock.Raise(cs => cs.CurrentAccountChanged += null, EventArgs.Empty);

            eventHandlerMock.Verify(h => h(_objectUnderTest, EventArgs.Empty));
        }

        [TestMethod]
        public void TestCreateResourceManagerDataSource_ReturnsNullForNoCredentials()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential).Returns(() => null);

            IResourceManagerDataSource result = _objectUnderTest.CreateResourceManagerDataSource();

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestCreateResourceManagerDataSource_Returns()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential)
                .Returns(_userAccount.GetGoogleCredential());

            IResourceManagerDataSource result = _objectUnderTest.CreateResourceManagerDataSource();

            Assert.That.DataSource(result).IsBuiltFrom(_userAccount);
        }

        [TestMethod]
        public void TestResourceManagerDataSource_ReturnsNullForNoCredentials()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential).Returns(() => null);

            IResourceManagerDataSource result = _objectUnderTest.ResourceManagerDataSource;

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestResourceManagerDataSource_Returns()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential)
                .Returns(_userAccount.GetGoogleCredential());

            IResourceManagerDataSource result = _objectUnderTest.ResourceManagerDataSource;

            Assert.That.DataSource(result).IsBuiltFrom(_userAccount);
        }

        [TestMethod]
        public void TestResourceManagerDataSource_ResetByAccountChange()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential)
                .Returns(() => null);

            IResourceManagerDataSource emptyCredentailsSource = _objectUnderTest.ResourceManagerDataSource;
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential)
                .Returns(_userAccount.GetGoogleCredential());
            CredentialStoreMock.Raise(cs => cs.CurrentAccountChanged += null, EventArgs.Empty);
            IResourceManagerDataSource updatedCredentialsSource = _objectUnderTest.ResourceManagerDataSource;

            Assert.IsNull(emptyCredentailsSource);
            Assert.That.DataSource(updatedCredentialsSource).IsBuiltFrom(_userAccount);
        }

        [TestMethod]
        public void Test0ArgCreatePlusDataSource_ReturnsNullForNoCredentials()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential).Returns(() => null);

            IGPlusDataSource result = _objectUnderTest.CreatePlusDataSource();

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Test0ArgCreatePlusDataSource_Returns()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential).Returns(_userAccount.GetGoogleCredential());

            IGPlusDataSource result = _objectUnderTest.CreatePlusDataSource();

            Assert.That.DataSource(result).IsBuiltFrom(_userAccount);
        }

        [TestMethod]
        public void TestGPlusDataSource_ReturnsNullForNoCredentials()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential).Returns(() => null);

            IGPlusDataSource result = _objectUnderTest.GPlusDataSource;

            Assert.IsNull(result);
        }

        [TestMethod]
        public void TestGPlusDataSource_Returns()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential).Returns(_userAccount.GetGoogleCredential());

            IGPlusDataSource result = _objectUnderTest.GPlusDataSource;

            Assert.That.DataSource(result).IsBuiltFrom(_userAccount);
        }

        [TestMethod]
        public void TestGPlusDataSource_ResetByAccountChange()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential)
                .Returns(() => null);

            IGPlusDataSource emptyCredentailsSource = _objectUnderTest.GPlusDataSource;
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential)
                .Returns(_userAccount.GetGoogleCredential());
            CredentialStoreMock.Raise(cs => cs.CurrentAccountChanged += null, EventArgs.Empty);
            IGPlusDataSource updatedCredentialsSource = _objectUnderTest.GPlusDataSource;

            Assert.IsNull(emptyCredentailsSource);
            Assert.That.DataSource(updatedCredentialsSource).IsBuiltFrom(_userAccount);
        }

        [TestMethod]
        public void Test1ArgCreatePlusDataSource_ReturnsNullForNoCredentials()
        {
            IGPlusDataSource result = _objectUnderTest.CreatePlusDataSource(null);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Test1ArgCreatePlusDataSource_Returns()
        {
            IGPlusDataSource result = _objectUnderTest.CreatePlusDataSource(_userAccount.GetGoogleCredential());

            Assert.That.DataSource(result).IsBuiltFrom(_userAccount);
        }

        [TestMethod]
        public void TestCreateGkeDataSource_BuildsFromCurrentAccount()
        {
            CredentialStoreMock.SetupGet(cs => cs.CurrentGoogleCredential).Returns(_userAccount.GetGoogleCredential());

            IGkeDataSource result = _objectUnderTest.CreateGkeDataSource();

            Assert.That.DataSource(result).IsBuiltFrom(_userAccount);
        }
    }

    internal static class AssertExtension
    {
        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        public static DataSourceAssert DataSource(this Assert that, IDataSourceBase<BaseClientService> dataSource) =>
            new DataSourceAssert(dataSource);

        internal class DataSourceAssert
        {
            private readonly BaseClientService _service;

            public DataSourceAssert(IDataSourceBase<BaseClientService> dataSource)
            {
                _service = dataSource.Service;
            }

            public void IsBuiltFrom(IUserAccount userAccount)
            {
                var googleCredential = (GoogleCredential)_service.HttpClientInitializer;
                var userCredential = (UserCredential)googleCredential.UnderlyingCredential;
                var flow = (GoogleAuthorizationCodeFlow)userCredential.Flow;
                Assert.AreEqual(userAccount.ClientSecret, flow.ClientSecrets.ClientSecret);
                Assert.AreEqual(userAccount.ClientId, flow.ClientSecrets.ClientId);
                Assert.AreEqual(userAccount.RefreshToken, userCredential.Token.RefreshToken);
                Assert.AreEqual(GoogleCloudExtensionPackage.Instance.VersionedApplicationName, _service.ApplicationName);
            }
        }
    }
}
