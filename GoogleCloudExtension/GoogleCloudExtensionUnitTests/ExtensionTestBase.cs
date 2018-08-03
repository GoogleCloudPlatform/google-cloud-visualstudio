﻿// Copyright 2018 Google Inc. All Rights Reserved.
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
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests
{
    public abstract class ExtensionTestBase
    {
        private Mock<IServiceProvider> _serviceProviderMock;
        protected Mock<IGoogleCloudExtensionPackage> PackageMock { get; private set; }

        protected Mock<ICredentialsStore> CredentialStoreMock { get; private set; }

        [TestInitialize]
        public void IntializeGlobalsForTest()
        {

            PackageMock = new Mock<IGoogleCloudExtensionPackage> { DefaultValue = DefaultValue.Mock };
            PackageMock.Setup(p => p.JoinableTaskFactory).Returns(AssemblyInitialize.JoinableApplicationContext.Factory);

            _serviceProviderMock = new Mock<IServiceProvider>();
            _serviceProviderMock.SetupService<SVsActivityLog, IVsActivityLog>();
            var taskSchedularMock = new Mock<IVsTaskSchedulerService>();
            Mock<IVsTaskSchedulerService2> taskSchedular2Mock = taskSchedularMock.As<IVsTaskSchedulerService2>();
            taskSchedular2Mock.Setup(ts => ts.GetAsyncTaskContext()).Returns(AssemblyInitialize.JoinableApplicationContext);
            _serviceProviderMock.SetupService<SVsTaskSchedulerService, IVsTaskSchedulerService>(taskSchedularMock);
            _serviceProviderMock.SetupDefaultServices();

            ServiceProvider oldProvider = ServiceProvider.GlobalProvider;
            ServiceProvider.CreateFromSetSite(_serviceProviderMock.Object);
            Assert.AreNotEqual(oldProvider, ServiceProvider.GlobalProvider);

            GoogleCloudExtensionPackage.Instance = PackageMock.Object;

            CredentialStoreMock = Mock.Get(CredentialsStore.Default);
            CredentialStoreMock.SetupGet(cs => cs.CurrentProjectId).Returns("DefaultProjectId");
            CredentialStoreMock.SetupGet(cs => cs.CurrentAccount)
                .Returns(new UserAccount { AccountName = "DefaultAccountName" });

            EventsReporterWrapper.DisableReporting();

            BeforeEach();
        }

        protected virtual void BeforeEach() { }

        [TestCleanup]
        public void CleanupGlobalsForTest()
        {
            AfterEach();
            _serviceProviderMock.Dispose();
            GoogleCloudExtensionPackage.Instance = null;
        }

        protected virtual void AfterEach() { }
    }
}
