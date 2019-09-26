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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using GoogleCloudExtension.CloudExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GoogleCloudExtensionUnitTests.CloudExplorer
{
    public class DynamicCloudExplorerSourceBaseTests
    {
        /// <summary>
        /// A testable <see cref="SourceRootViewModelBase"/> with overriden functions incrementing call counts.
        /// </summary>
        [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
        private class TestRootViewModel : SourceRootViewModelBase
        {
            #region NotImplemented

            /// <summary>
            /// Returns the caption to use for the root node for this data source.
            /// </summary>
            public override string RootCaption => throw new NotImplementedException();

            /// <summary>
            /// Returns the tree node to use when there's an error loading data.
            /// </summary>
            public override TreeLeaf ErrorPlaceholder => throw new NotImplementedException();

            /// <summary>
            /// Returns the tree node to use when there's no data returned by this data source.
            /// </summary>
            public override TreeLeaf NoItemsPlaceholder => throw new NotImplementedException();

            /// <summary>
            /// Returns the tree node to use while loading data.
            /// </summary>
            public override TreeLeaf LoadingPlaceholder => throw new NotImplementedException();

            /// <summary>
            /// Returns the tree node to use when we detect that the necessary APIs are not enabled.
            /// </summary>
            public override TreeLeaf ApiNotEnabledPlaceholder => throw new NotImplementedException();

            /// <summary>
            /// Returns the names of the required APIs for the source.
            /// </summary>
            public override IList<string> RequiredApis => throw new NotImplementedException();

            /// <summary>
            /// Override this function to load and display the data in the control.
            /// </summary>
            protected override Task LoadDataOverrideAsync() => throw new NotImplementedException();

            #endregion NotImplemented

            public override void Initialize(ICloudSourceContext context) => InitializeMock.Object(context);

            public Mock<Action<ICloudSourceContext>> InitializeMock { get; } = new Mock<Action<ICloudSourceContext>>();
        }

        /// <summary>
        /// The concrete subclass of <see cref="DynamicCloudExplorerSourceBase{TRootViewModel}"/> used for testing.
        /// </summary>
        private class TestDynamicCloudExplorerSourceBase : DynamicCloudExplorerSourceBase<TestRootViewModel>
        {
            public TestDynamicCloudExplorerSourceBase(ICloudSourceContext context) : base(context) { }
        }

        [TestMethod]
        public void TestConstructor_InitializesRoot()
        {
            var mockedCloudSourceContext = Mock.Of<ICloudSourceContext>();
            var objectUnderTest = new TestDynamicCloudExplorerSourceBase(mockedCloudSourceContext);

            Assert.IsNotNull(objectUnderTest.Root);
            objectUnderTest.Root.InitializeMock.Verify(f => f(mockedCloudSourceContext), Times.Once);
        }
    }
}
