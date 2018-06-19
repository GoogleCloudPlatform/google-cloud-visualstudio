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

using GoogleCloudExtension.CloudExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.CloudExplorer
{
    [TestClass]
    public class CloudExplorerSourceBaseTests
    {
        private ICloudSourceContext _mockedCloudSourceContext;
        private CloudExplorerSourceBase<ISourceRootViewModelBase> _objectUnderTest;

        [TestInitialize]
        public void BeforeEach()
        {
            _mockedCloudSourceContext = Mock.Of<ICloudSourceContext>();
            var objectUnderTestMock =
                new Mock<CloudExplorerSourceBase<ISourceRootViewModelBase>>(_mockedCloudSourceContext)
                {
                    CallBase = true,
                    DefaultValue = DefaultValue.Mock
                };

            _objectUnderTest = objectUnderTestMock.Object;
        }

        [TestMethod]
        public void TestConstructor_InitializesContext()
        {
            Assert.AreEqual(_mockedCloudSourceContext, _objectUnderTest.Context);
        }

        [TestMethod]
        public void TestRefresh_CallsRootRefresh()
        {
            _objectUnderTest.Refresh();

            Mock.Get(_objectUnderTest).Verify(o => o.Root.Refresh(), Times.Once);
        }

        [TestMethod]
        public void TestInvalidateProjectOrAccount_CallsRootInvalidateProjectOrAccount()
        {
            _objectUnderTest.InvalidateProjectOrAccount();

            Mock.Get(_objectUnderTest).Verify(o => o.Root.InvalidateProjectOrAccount(), Times.Once);
        }
    }
}
