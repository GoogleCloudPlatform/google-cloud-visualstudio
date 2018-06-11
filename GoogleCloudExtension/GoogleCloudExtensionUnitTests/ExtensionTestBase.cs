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

using GoogleCloudExtension;
using GoogleCloudExtension.Accounts;
using GoogleCloudExtension.Analytics;
using GoogleCloudExtension.UserPrompt;
using GoogleCloudExtension.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace GoogleCloudExtensionUnitTests
{
    public abstract class ExtensionTestBase
    {
        protected Mock<IGoogleCloudExtensionPackage> PackageMock { get; private set; }

        protected Mock<Func<UserPromptWindow.Options, bool>> PromptUserMock { get; private set; }
        protected Mock<IDataSourceFactory> DataSourceFactoryMock { get; private set; }

        protected static Mock<ICredentialsStore> CredentialStoreMock { get; private set; }

        [TestInitialize]
        public void IntializeGlobalsForTest()
        {
            PackageMock = new Mock<IGoogleCloudExtensionPackage> { DefaultValue = DefaultValue.Mock };
            GoogleCloudExtensionPackage.Instance = PackageMock.Object;

            CredentialStoreMock = Mock.Get(CredentialsStore.Default);
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns("DefaultProjectId");
            CredentialStoreMock.SetupGet(cs => cs.CurrentAccount)
                .Returns(new UserAccount { AccountName = "DefaultAccountName" });

            PromptUserMock = new Mock<Func<UserPromptWindow.Options, bool>>();
            UserPromptUtils.PromptUserOverride = PromptUserMock.Object;

            DataSourceFactoryMock = Mock.Get(GoogleCloudExtensionPackage.Instance.GetMefService<IDataSourceFactory>());
            EventsReporterWrapper.DisableReporting();
            BeforeEach();
        }

        protected virtual void BeforeEach() { }

        [TestCleanup]
        public void CleanupGlobalsForTest()
        {
            AfterEach();
            UserPromptUtils.PromptUserOverride = null;
            GoogleCloudExtensionPackage.Instance = null;
        }

        protected virtual void AfterEach() { }
    }
}
