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
using GoogleCloudExtension;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.ComponentModel.Composition.Hosting;

namespace GoogleCloudExtensionUnitTests
{
    public abstract class MockedGlobalServiceProviderTestsBase
    {
        protected Mock<DTE> DteMock { get; private set; }
        protected Mock<IServiceProvider> ServiceProviderMock { get; private set; }
        protected Mock<IComponentModel> ComponentModelMock { get; private set; }
        protected abstract IVsPackage Package { get; }

        protected void RunPackageInitalize()
        {
            // This runs the Initialize() method.
            Package.SetSite(ServiceProviderMock.Object);
        }

        [TestInitialize]
        public void TestInitalize()
        {
            DteMock = new Mock<DTE>();
            ServiceProviderMock = DteMock.As<IServiceProvider>();
            ServiceProviderMock.SetupService<DTE, DTE>(DteMock);
            ComponentModelMock = ServiceProviderMock.SetupService<SComponentModel, IComponentModel>();
            ComponentModelMock.DefaultValueProvider = DefaultValueProvider.Mock;

            // Initalize the export provider to get types exported in GoogleCloudExtension.dll.
            ComponentModelMock.Setup(cm => cm.DefaultExportProvider).Returns(
                new CompositionContainer(new AssemblyCatalog(typeof(GoogleCloudExtensionPackage).Assembly)));
            ServiceProviderMock.SetupDefaultServices();
        }

        [TestCleanup]
        public void AfterEach()
        {
            GoogleCloudExtensionPackage.Instance = null;
            ServiceProviderMock.Dispose();
        }
    }
}