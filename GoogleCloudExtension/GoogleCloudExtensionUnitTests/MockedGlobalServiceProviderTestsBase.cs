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

using EnvDTE;
using EnvDTE80;
using GoogleCloudExtension;
using GoogleCloudExtensionUnitTests.FakeServices;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace GoogleCloudExtensionUnitTests
{
    public abstract class MockedGlobalServiceProviderTestsBase
    {
        protected Mock<DTE2> DteMock { get; private set; }
        protected Mock<IServiceProvider> ServiceProviderMock { get; private set; }
        protected Mock<IComponentModel> ComponentModelMock { get; private set; }

        [TestInitialize]
        public void TestInitialize()
        {
            var taskSchedulerMock = new Mock<IVsTaskSchedulerService>();
            Mock<IVsTaskSchedulerService2> taskScheduler2Mock = taskSchedulerMock.As<IVsTaskSchedulerService2>();
            taskSchedulerMock.Setup(ts => ts.CreateTaskCompletionSource())
                .Returns(() => new FakeIVsTaskCompletionSource());
            taskScheduler2Mock.Setup(ts => ts.GetAsyncTaskContext())
                .Returns(AssemblyInitialize.JoinableApplicationContext);
            taskScheduler2Mock.Setup(ts => ts.GetTaskScheduler(It.IsAny<uint>()))
                .Returns((uint context) => FakeIVsTask.GetSchedulerFromContext((__VSTASKRUNCONTEXT)context));

            DteMock = new Mock<DTE>().As<DTE2>();
            ServiceProviderMock = DteMock.As<IServiceProvider>();
            ServiceProviderMock.SetupService<SDTE, DTE2>(DteMock);
            ServiceProviderMock.SetupService<DTE, DTE2>(DteMock);
            ComponentModelMock =
                ServiceProviderMock.SetupService<SComponentModel, IComponentModel>(DefaultValueProvider.Mock);
            ServiceProviderMock.SetupService<SVsTaskSchedulerService, IVsTaskSchedulerService2>(taskScheduler2Mock);
            ServiceProviderMock.SetupDefaultServices();

            ServiceProvider oldProvider = ServiceProvider.GlobalProvider;
            ServiceProvider.CreateFromSetSite(ServiceProviderMock.Object);
            Assert.AreNotEqual(oldProvider, ServiceProvider.GlobalProvider);
        }

        [TestCleanup]
        public void AfterEach()
        {
            GoogleCloudExtensionPackage.Instance = null;
            ServiceProviderMock.Dispose();
        }
    }
}