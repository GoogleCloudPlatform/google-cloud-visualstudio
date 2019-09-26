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
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.ManageAccounts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.ManageAccounts
{
    [TestClass]
    public class ManageAccountsViewModelTests : ExtensionTestBase
    {
        private ManageAccountsViewModel _objectUnderTest;
        private Mock<Action> _closeHandlerMock;

        [TestInitialize]
        public void BeforeEach()
        {
            _objectUnderTest = new ManageAccountsViewModel();

            _closeHandlerMock = new Mock<Action>();
            _objectUnderTest.Close += _closeHandlerMock.Object;
        }

        [TestMethod]
        public void TestDoubleClickedItem_InvokesClose()
        {
            CredentialStoreMock.Setup(cs => cs.CurrentAccount.AccountName).Returns("CurrentAccountName");

            _objectUnderTest.DoubleClickedItem(
                new UserAccountViewModel(Mock.Of<IUserAccount>(ua => ua.AccountName == "SelectedAccountName")));

            _closeHandlerMock.Verify(a => a());
        }

        [TestMethod]
        public void TestDoubleClickedItem_UpdatesCurrentAccount()
        {
            CredentialStoreMock.Setup(cs => cs.CurrentAccount.AccountName).Returns("CurrentAccountName");
            var userAccount = Mock.Of<IUserAccount>(ua => ua.AccountName == "SelectedAccountName");

            _objectUnderTest.DoubleClickedItem(new UserAccountViewModel(userAccount));

            CredentialStoreMock.Verify(cs => cs.UpdateCurrentAccount(userAccount));
        }

        [TestMethod]
        public void TestDoubleClickedItem_ShortCircutsCurrentAccount()
        {
            CredentialStoreMock.Setup(cs => cs.CurrentAccount.AccountName).Returns("CurrentAccountName");
            var userAccount = Mock.Of<IUserAccount>(ua => ua.AccountName == "CurrentAccountName");

            _objectUnderTest.DoubleClickedItem(new UserAccountViewModel(userAccount));

            CredentialStoreMock.Verify(cs => cs.UpdateCurrentAccount(userAccount), Times.Never);
            _closeHandlerMock.Verify(a => a(), Times.Never);
        }

        [TestMethod]
        public void TestSetAsCurrentAccount_InvokesClose()
        {
            _objectUnderTest.CurrentUserAccount = new UserAccountViewModel(Mock.Of<IUserAccount>());

            _objectUnderTest.SetAsCurrentAccountCommand.Execute(null);

            _closeHandlerMock.Verify(a => a());
        }

        [TestMethod]
        public void TestSetAsCurrentAccount_UpdatesCurrentAccount()
        {
            var userAccount = Mock.Of<IUserAccount>(ua => ua.AccountName == "SelectedAccountName");
            _objectUnderTest.CurrentUserAccount = new UserAccountViewModel(userAccount);

            _objectUnderTest.SetAsCurrentAccountCommand.Execute(null);

            CredentialStoreMock.Verify(cs => cs.UpdateCurrentAccount(userAccount));
        }
    }
}
